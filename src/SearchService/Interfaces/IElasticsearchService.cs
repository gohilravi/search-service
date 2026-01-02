using SearchService.Models;

namespace SearchService.Interfaces;

public interface IElasticsearchService
{
    Task<bool> IndexOfferDocumentAsync(ElasticSearchOfferDocument document, CancellationToken cancellationToken = default);
    Task<bool> UpdateOfferDocumentAsync(string id, ElasticSearchOfferDocument document, CancellationToken cancellationToken = default);
    Task<bool> DeleteOfferDocumentAsync(string id, CancellationToken cancellationToken = default);
    Task<SearchResult> SearchAsync(SearchService.Models.SearchRequest request, UserContext userContext, object? accessFilter = null, CancellationToken cancellationToken = default);
    Task<AutocompleteResult> AutocompleteAsync(AutocompleteRequest request, CancellationToken cancellationToken = default);
    Task<bool> OfferIndexExistsAsync(CancellationToken cancellationToken = default);
    Task<bool> CreateOfferIndexAsync(CancellationToken cancellationToken = default);
    Task<List<ElasticSearchOfferDocument>> FindOfferDocumentsByEntityIdAsync(string entityType, string entityId, CancellationToken cancellationToken = default);
    Task<ElasticSearchOfferDocument?> GetOfferDocumentByIdAsync(string id, CancellationToken cancellationToken = default);
}