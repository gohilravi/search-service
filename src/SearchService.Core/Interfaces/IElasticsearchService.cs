using SearchService.Core.Models;

namespace SearchService.Core.Interfaces;

public interface IElasticsearchService
{
    Task<bool> IndexDocumentAsync<T>(T document, string indexName) where T : class;
    Task<bool> BulkIndexDocumentsAsync<T>(IEnumerable<T> documents, string indexName) where T : class;
    Task<bool> DeleteDocumentAsync(string id, string indexName);
    Task<SearchResult> SearchAsync(SearchRequest request, UserContext userContext, object? accessFilter = null);
    Task<bool> IndexExistsAsync(string indexName);
    Task<bool> CreateIndexAsync(string indexName);
}

