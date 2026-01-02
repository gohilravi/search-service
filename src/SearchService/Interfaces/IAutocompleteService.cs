using SearchService.Models;

namespace SearchService.Interfaces;

public interface IAutocompleteService
{
    Task<AutocompleteResult> GetSuggestionsAsync(AutocompleteRequest request, UserContext userContext, CancellationToken cancellationToken = default);
}