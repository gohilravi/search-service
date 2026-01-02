using Nest;
using Microsoft.Extensions.Logging;
using SearchService.Interfaces;
using SearchService.Models;

namespace SearchService.Services.Elasticsearch;

public class ElasticsearchService : IElasticsearchService
{
    private readonly IElasticClient _client;
    private readonly IIndexManager _indexManager;
    private readonly IDocumentMapper _documentMapper;
    private readonly ILogger<ElasticsearchService> _logger;
    private const string OfferIndexName = "offers";

    public ElasticsearchService(
        IElasticClient client,
        IIndexManager indexManager,
        IDocumentMapper documentMapper,
        ILogger<ElasticsearchService> logger)
    {
        _client = client;
        _indexManager = indexManager;
        _documentMapper = documentMapper;
        _logger = logger;
    }

    public async Task<bool> IndexOfferDocumentAsync(ElasticSearchOfferDocument document, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureOfferIndexExistsAsync(cancellationToken);
            
            var response = await _client.IndexAsync(document, idx => idx
                .Index(OfferIndexName)
                .Id(document.Id), cancellationToken);
            
            if (!response.IsValid)
            {
                _logger.LogError("Failed to index offer document {DocumentId}: {Error}", document.Id, response.DebugInformation);
                return false;
            }

            _logger.LogInformation("Successfully indexed offer document {DocumentId}", document.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing offer document {DocumentId}", document.Id);
            return false;
        }
    }

    public async Task<bool> UpdateOfferDocumentAsync(string id, ElasticSearchOfferDocument document, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureOfferIndexExistsAsync(cancellationToken);

            var response = await _client.UpdateAsync<ElasticSearchOfferDocument>(id, u => u
                .Index(OfferIndexName)
                .Doc(document)
                .DocAsUpsert(true), cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError("Failed to update offer document {DocumentId}: {Error}", id, response.DebugInformation);
                return false;
            }

            _logger.LogInformation("Successfully updated offer document {DocumentId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating offer document {DocumentId}", id);
            return false;
        }
    }

    public async Task<bool> DeleteOfferDocumentAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.DeleteAsync<ElasticSearchOfferDocument>(id, d => d
                .Index(OfferIndexName), cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError("Failed to delete offer document {DocumentId}: {Error}", id, response.DebugInformation);
                return false;
            }

            _logger.LogInformation("Successfully deleted offer document {DocumentId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting offer document {DocumentId}", id);
            return false;
        }
    }

    public async Task<SearchResult> SearchAsync(SearchService.Models.SearchRequest request, UserContext userContext, object? accessFilter = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureOfferIndexExistsAsync(cancellationToken);

            var searchDescriptor = new SearchDescriptor<ElasticSearchOfferDocument>()
                .Index(OfferIndexName)
                .Size(request.PageSize)
                .From(request.PageNumber * request.PageSize)
                .Query(q => BuildSearchQuery(q, request, userContext, accessFilter))
                .Sort(s => BuildSortQuery(s, request));

            // Add aggregations if requested
            if (request.IncludeAggregations)
            {
                searchDescriptor = searchDescriptor.Aggregations(a => BuildAggregations(a, request));
            }

            var response = await _client.SearchAsync<ElasticSearchOfferDocument>(searchDescriptor, cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError("Search failed: {Error}", response.DebugInformation);
                return new SearchResult { IsSuccessful = false, ErrorMessage = "Search failed" };
            }

            return new SearchResult
            {
                IsSuccessful = true,
                TotalResults = response.Total,
                Results = response.Documents.Cast<object>().ToList(),
                Aggregations = ExtractAggregations(response.Aggregations),
                ExecutionTimeMs = response.Took
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing search");
            return new SearchResult { IsSuccessful = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<AutocompleteResult> AutocompleteAsync(AutocompleteRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureOfferIndexExistsAsync(cancellationToken);

            var searchDescriptor = new SearchDescriptor<ElasticSearchOfferDocument>()
                .Index(OfferIndexName)
                .Size(request.MaxResults)
                .Query(q => q
                    .MultiMatch(mm => mm
                        .Fields(f => f
                            .Field(p => p.VehicleMake)
                            .Field(p => p.VehicleModel)
                            .Field(p => p.Vin)
                            .Field(p => p.Seller.Name)
                            .Field(p => p.SearchableText))
                        .Query(request.Term)
                        .Type(TextQueryType.BoolPrefix)
                        .Fuzziness(Fuzziness.Auto)))
                .Source(s => s.Includes(i => i
                    .Field(p => p.Id)
                    .Field(p => p.OfferId)
                    .Field(p => p.VehicleMake)
                    .Field(p => p.VehicleModel)
                    .Field(p => p.Vin)
                    .Field(p => p.Seller.Name)));

            var response = await _client.SearchAsync<ElasticSearchOfferDocument>(searchDescriptor, cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError("Autocomplete search failed: {Error}", response.DebugInformation);
                return new AutocompleteResult { IsSuccessful = false };
            }

            return new AutocompleteResult
            {
                IsSuccessful = true,
                Suggestions = response.Documents.Select(d => new AutocompleteSuggestion
                {
                    Value = $"{d.VehicleMake} {d.VehicleModel} - {d.Vin}",
                    Label = $"{d.VehicleYear} {d.VehicleMake} {d.VehicleModel}",
                    Id = d.Id,
                    Category = "Offer"
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing autocomplete");
            return new AutocompleteResult { IsSuccessful = false };
        }
    }

    public async Task<bool> OfferIndexExistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.Indices.ExistsAsync(OfferIndexName, ct: cancellationToken);
            return response.Exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if offer index exists");
            return false;
        }
    }

    public async Task<bool> CreateOfferIndexAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.Indices.CreateAsync(OfferIndexName, c => c
                .Map<ElasticSearchOfferDocument>(m => m.AutoMap())
                .Settings(s => s
                    .NumberOfShards(3)
                    .NumberOfReplicas(1)
                    .Analysis(a => a
                        .Analyzers(an => an
                            .Custom("autocomplete_analyzer", ca => ca
                                .Tokenizer("standard")
                                .Filters("lowercase", "edge_ngram")))
                        .TokenFilters(tf => tf
                            .EdgeNGram("edge_ngram", ng => ng
                                .MinGram(1)
                                .MaxGram(20))))), cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError("Failed to create offer index: {Error}", response.DebugInformation);
                return false;
            }

            _logger.LogInformation("Successfully created offer index");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating offer index");
            return false;
        }
    }

    public async Task<List<ElasticSearchOfferDocument>> FindOfferDocumentsByEntityIdAsync(string entityType, string entityId, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureOfferIndexExistsAsync(cancellationToken);

            var queryDescriptor = BuildEntitySearchQuery(entityType, entityId);
            
            var searchDescriptor = new SearchDescriptor<ElasticSearchOfferDocument>()
                .Index(OfferIndexName)
                .Size(1000) // Reasonable limit for batch updates
                .Query(q => queryDescriptor);

            var response = await _client.SearchAsync<ElasticSearchOfferDocument>(searchDescriptor, cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError("Failed to find offer documents by entity {EntityType}:{EntityId}: {Error}", entityType, entityId, response.DebugInformation);
                return new List<ElasticSearchOfferDocument>();
            }

            return response.Documents.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding offer documents by entity {EntityType}:{EntityId}", entityType, entityId);
            return new List<ElasticSearchOfferDocument>();
        }
    }

    public async Task<ElasticSearchOfferDocument?> GetOfferDocumentByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureOfferIndexExistsAsync(cancellationToken);
            
            var response = await _client.GetAsync<ElasticSearchOfferDocument>(id, g => g
                .Index(OfferIndexName), cancellationToken);

            if (!response.IsValid || !response.Found)
            {
                return null;
            }

            return response.Source;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting offer document by id {DocumentId}", id);
            return null;
        }
    }

    private async Task EnsureOfferIndexExistsAsync(CancellationToken cancellationToken)
    {
        if (!await OfferIndexExistsAsync(cancellationToken))
        {
            await CreateOfferIndexAsync(cancellationToken);
        }
    }

    private QueryContainer BuildSearchQuery(QueryContainerDescriptor<ElasticSearchOfferDocument> q, SearchService.Models.SearchRequest request, UserContext userContext, object? accessFilter)
    {
        var queries = new List<QueryContainer>();

        // Main search query
        if (!string.IsNullOrEmpty(request.Query))
        {
            queries.Add(q.MultiMatch(mm => mm
                .Fields(f => f
                    .Field(p => p.VehicleMake, 2.0)
                    .Field(p => p.VehicleModel, 2.0)
                    .Field(p => p.Vin, 3.0)
                    .Field(p => p.Seller.Name, 1.5)
                    .Field(p => p.SearchableText))
                .Query(request.Query)
                .Type(TextQueryType.BestFields)
                .Fuzziness(Fuzziness.Auto)));
        }

        // Filters
        if (!string.IsNullOrEmpty(request.Status))
        {
            queries.Add(q.Term(t => t.Field(p => p.Status).Value(request.Status)));
        }

        // Access control
        if (accessFilter != null)
        {
            // Apply access filter based on user context
            // Implementation depends on your access control requirements
        }

        return queries.Count > 0 ? q.Bool(b => b.Must(queries.ToArray())) : q.MatchAll();
    }

    private SortDescriptor<ElasticSearchOfferDocument> BuildSortQuery(SortDescriptor<ElasticSearchOfferDocument> s, SearchService.Models.SearchRequest request)
    {
        if (string.IsNullOrEmpty(request.SortField))
        {
            return s.Field(p => p.CreatedAt, SortOrder.Descending);
        }

        var sortOrder = request.SortOrder?.ToLower() == "asc" ? SortOrder.Ascending : SortOrder.Descending;

        return request.SortField.ToLower() switch
        {
            "createdat" => s.Field(p => p.CreatedAt, sortOrder),
            "lastmodifiedat" => s.Field(p => p.LastModifiedAt, sortOrder),
            "mileage" => s.Field(p => p.Mileage, sortOrder),
            "vehicleyear" => s.Field(p => p.VehicleYear, sortOrder),
            _ => s.Field(p => p.CreatedAt, SortOrder.Descending)
        };
    }

    private AggregationContainerDescriptor<ElasticSearchOfferDocument> BuildAggregations(AggregationContainerDescriptor<ElasticSearchOfferDocument> a, SearchService.Models.SearchRequest request)
    {
        return a
            .Terms("makes", t => t.Field(p => p.VehicleMake.Suffix("keyword")).Size(20))
            .Terms("models", t => t.Field(p => p.VehicleModel.Suffix("keyword")).Size(20))
            .Terms("years", t => t.Field(p => p.VehicleYear.Suffix("keyword")).Size(20))
            .Terms("status", t => t.Field(p => p.Status.Suffix("keyword")).Size(10))
            .Range("mileage_ranges", r => r
                .Field(p => p.Mileage)
                .Ranges(
                    rng => rng.To(50000).Key("0-50k"),
                    rng => rng.From(50000).To(100000).Key("50k-100k"),
                    rng => rng.From(100000).To(150000).Key("100k-150k"),
                    rng => rng.From(150000).Key("150k+")));
    }

    private Dictionary<string, object> ExtractAggregations(IReadOnlyDictionary<string, IAggregate> aggregations)
    {
        var result = new Dictionary<string, object>();

        foreach (var agg in aggregations)
        {
            if (agg.Value is BucketAggregate bucketAgg)
            {
                result[agg.Key] = bucketAgg.Items.OfType<KeyedBucket<object>>()
                    .Select(b => new { Key = b.Key, Count = b.DocCount })
                    .ToList();
            }
        }

        return result;
    }

    private QueryContainer BuildEntitySearchQuery(string entityType, string entityId)
    {
        return entityType.ToLower() switch
        {
            "offer" => new TermQuery { Field = "offerId", Value = entityId },
            "seller" => new TermQuery { Field = "sellerId", Value = entityId },
            "purchase" => new NestedQuery
            {
                Path = "purchases",
                Query = new TermQuery { Field = "purchases.id", Value = entityId }
            },
            "buyer" => new NestedQuery
            {
                Path = "purchases",
                Query = new NestedQuery
                {
                    Path = "purchases.buyer",
                    Query = new TermQuery { Field = "purchases.buyer.id", Value = entityId }
                }
            },
            "transport" => new NestedQuery
            {
                Path = "transports",
                Query = new TermQuery { Field = "transports.id", Value = entityId }
            },
            "carrier" => new NestedQuery
            {
                Path = "transports",
                Query = new NestedQuery
                {
                    Path = "transports.carrier",
                    Query = new TermQuery { Field = "transports.carrier.id", Value = entityId }
                }
            },
            _ => throw new ArgumentException($"Unknown entity type: {entityType}")
        };
    }

    public async Task<bool> BulkIndexDocumentsAsync<T>(IEnumerable<T> documents, string indexName) where T : class
    {
        try
        {
            await _indexManager.EnsureIndexExistsAsync(indexName);
            var bulkResponse = await _client.BulkAsync(b => b
                .Index(indexName)
                .IndexMany(documents));

            if (!bulkResponse.IsValid)
            {
                _logger.LogError("Bulk indexing failed: {Error}", bulkResponse.DebugInformation);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk indexing");
            return false;
        }
    }

    public async Task<bool> DeleteDocumentAsync(string id, string indexName)
    {
        try
        {
            var response = await _client.DeleteAsync<object>(id, d => d.Index(indexName));
            return response.IsValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {Id}", id);
            return false;
        }
    }

    public async Task<bool> IndexExistsAsync(string indexName)
    {
        var response = await _client.Indices.ExistsAsync(indexName);
        return response.Exists;
    }

    public async Task<bool> CreateIndexAsync(string indexName)
    {
        return await _indexManager.CreateIndexAsync(indexName);
    }
}