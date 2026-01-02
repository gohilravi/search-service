using Nest;
using SearchService.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace SearchService.Infrastructure.Elasticsearch;

public class IndexManager : IIndexManager
{
    private readonly IElasticClient _client;
    private readonly ILogger<IndexManager> _logger;
    private const string IndexName = "search_index_v1";

    public IndexManager(IElasticClient client, ILogger<IndexManager> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<bool> EnsureIndexExistsAsync(string indexName)
    {
        var exists = await IndexExistsAsync(indexName);
        if (!exists)
        {
            return await CreateIndexAsync(indexName);
        }
        return true;
    }

    public async Task<bool> CreateIndexAsync(string indexName)
    {
        try
        {
            var createIndexResponse = await _client.Indices.CreateAsync(indexName, c => c
                .Settings(s => s
                    .NumberOfShards(1)
                    .NumberOfReplicas(1)
                    .Analysis(a => a
                        .Analyzers(an => an
                            .Custom("vin_analyzer", ca => ca
                                .Tokenizer("keyword")
                                .Filters("lowercase")))))
                .Map<Dictionary<string, object>>(m => m
                    .Dynamic()
                    .Properties(p => p
                        .Keyword(k => k.Name("entityType"))
                        .Keyword(k => k.Name("id"))
                        .Keyword(k => k.Name("vin").Fields(f => f.Text(t => t.Name("text"))))
                        .Text(t => t.Name("make").Fields(f => f.Keyword(k => k.Name("keyword"))))
                        .Text(t => t.Name("model"))
                        .Number(n => n.Name("year").Type(NumberType.Integer))
                        .Text(t => t.Name("location"))
                        .Keyword(k => k.Name("status"))
                        .Keyword(k => k.Name("sellerId"))
                        .Keyword(k => k.Name("buyerId"))
                        .Keyword(k => k.Name("carrierId"))
                        .Date(d => d.Name("createdAt"))
                        .Date(d => d.Name("updatedAt"))
                        .Completion(c => c.Name("suggest")))));

            if (!createIndexResponse.IsValid)
            {
                _logger.LogError("Failed to create index: {Error}", createIndexResponse.DebugInformation);
                return false;
            }

            _logger.LogInformation("Index {IndexName} created successfully", indexName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating index {IndexName}", indexName);
            return false;
        }
    }

    public async Task<bool> DeleteIndexAsync(string indexName)
    {
        try
        {
            var response = await _client.Indices.DeleteAsync(indexName);
            return response.IsValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting index {IndexName}", indexName);
            return false;
        }
    }

    public async Task<bool> IndexExistsAsync(string indexName)
    {
        var response = await _client.Indices.ExistsAsync(indexName);
        return response.Exists;
    }
}

