namespace SearchService.Core.Interfaces;

public interface IIndexManager
{
    Task<bool> EnsureIndexExistsAsync(string indexName);
    Task<bool> CreateIndexAsync(string indexName);
    Task<bool> DeleteIndexAsync(string indexName);
    Task<bool> IndexExistsAsync(string indexName);
}

