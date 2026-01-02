using Nest;
using Microsoft.Extensions.Logging;
using SearchService.Core.Interfaces;
using SearchService.Core.Models;
using CoreModels = SearchService.Core.Models;

namespace SearchService.Infrastructure.Elasticsearch;

public class ElasticsearchService : IElasticsearchService
{
    private readonly IElasticClient _client;
    private readonly IIndexManager _indexManager;
    private readonly IDocumentMapper _documentMapper;
    private readonly ILogger<ElasticsearchService> _logger;
    private const string IndexName = "search_index_v1";

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

    public async Task<bool> IndexDocumentAsync<T>(T document, string indexName) where T : class
    {
        try
        {
            await _indexManager.EnsureIndexExistsAsync(indexName);
            var response = await _client.IndexAsync(document, idx => idx.Index(indexName));
            
            if (!response.IsValid)
            {
                _logger.LogError("Failed to index document: {Error}", response.DebugInformation);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing document");
            return false;
        }
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

    public async Task<SearchResult> SearchAsync(CoreModels.SearchRequest request, UserContext userContext, object? accessFilter = null)
    {
        try
        {
            await _indexManager.EnsureIndexExistsAsync(IndexName);

            var searchDescriptor = new SearchDescriptor<Dictionary<string, object>>()
                .Index(IndexName)
                .From((request.Page - 1) * request.PageSize)
                .Size(request.PageSize)
                .Query(q => BuildQuery(q, request, userContext, accessFilter));

            var response = await _client.SearchAsync<Dictionary<string, object>>(searchDescriptor);

            if (!response.IsValid)
            {
                _logger.LogError("Search failed: {Error}", response.DebugInformation);
                return new SearchResult { Page = request.Page, PageSize = request.PageSize };
            }

            var items = response.Hits.Select(hit => new SearchResultItem
            {
                Id = hit.Id,
                EntityType = hit.Source?.GetValueOrDefault("entityType")?.ToString() ?? string.Empty,
                Vin = hit.Source?.GetValueOrDefault("vin")?.ToString() ?? string.Empty,
                Make = hit.Source?.GetValueOrDefault("make")?.ToString() ?? string.Empty,
                Model = hit.Source?.GetValueOrDefault("model")?.ToString() ?? string.Empty,
                Year = hit.Source?.GetValueOrDefault("year") as int?,
                Location = hit.Source?.GetValueOrDefault("location")?.ToString() ?? string.Empty,
                Status = hit.Source?.GetValueOrDefault("status")?.ToString() ?? string.Empty,
                Score = hit.Score ?? 0,
                AdditionalFields = hit.Source?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object>()
            }).ToList();

            return new SearchResult
            {
                Items = items,
                TotalCount = response.Total,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing search");
            return new SearchResult { Page = request.Page, PageSize = request.PageSize };
        }
    }

    private QueryContainer BuildQuery(QueryContainerDescriptor<Dictionary<string, object>> q, CoreModels.SearchRequest request, UserContext userContext, object? accessFilter = null)
    {
        var mustQueries = new List<QueryContainer>();
        var filterQueries = new List<QueryContainer>();

        // Entity type filter
        if (request.EntityTypes != null && request.EntityTypes.Any())
        {
            filterQueries.Add(q.Terms(t => t.Field("entityType").Terms(request.EntityTypes)));
        }

        // RBAC access filter
        if (accessFilter is Func<QueryContainerDescriptor<Dictionary<string, object>>, QueryContainer> filterFunc)
        {
            filterQueries.Add(filterFunc(q));
        }

        // Text search query
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            mustQueries.Add(q.MultiMatch(m => m
                .Query(request.Query)
                .Fields(f => f
                    .Field("vin")
                    .Field("make")
                    .Field("model")
                    .Field("location")
                    .Field("id"))
                .Fuzziness(Fuzziness.Auto)
                .Type(TextQueryType.BestFields)));
        }
        else
        {
            mustQueries.Add(q.MatchAll());
        }

        return q.Bool(b => 
        {
            b = b.Must(mustQueries.ToArray());
            if (filterQueries.Any())
            {
                b = b.Filter(filterQueries.ToArray());
            }
            return b;
        });
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

