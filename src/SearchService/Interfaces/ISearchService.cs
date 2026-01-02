using SearchService.Models;

namespace SearchService.Interfaces;

public interface ISearchService
{
    Task<SearchResult> SearchAsync(SearchRequest request, UserContext userContext, CancellationToken cancellationToken = default);
}