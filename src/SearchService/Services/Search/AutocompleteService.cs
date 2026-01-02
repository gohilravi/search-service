using SearchService.Interfaces;
using SearchService.Models;
using Microsoft.Extensions.Logging;
using SearchService.Services.Security;

namespace SearchService.Services.Search;

public class AutocompleteService : IAutocompleteService
{
    private readonly IElasticsearchService _elasticsearchService;
    private readonly ILogger<AutocompleteService> _logger;

    public AutocompleteService(
        IElasticsearchService elasticsearchService,
        ILogger<AutocompleteService> logger)
    {
        _elasticsearchService = elasticsearchService;
        _logger = logger;
    }

    public async Task<AutocompleteResult> GetSuggestionsAsync(AutocompleteRequest request, UserContext userContext, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing autocomplete request: {Term} for user {UserId}", request.Term, userContext.UserId);

            // Use the unified Elasticsearch service for autocomplete
            var result = await _elasticsearchService.AutocompleteAsync(request, cancellationToken);

            if (!result.IsSuccessful)
            {
                _logger.LogError("Autocomplete search failed: {ErrorMessage}", result.ErrorMessage);
                return new AutocompleteResult
                {
                    IsSuccessful = false,
                    ErrorMessage = result.ErrorMessage,
                    Suggestions = new List<AutocompleteSuggestion>()
                };
            }

            _logger.LogInformation("Autocomplete completed: {SuggestionCount} suggestions found", result.Suggestions.Count);

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Autocomplete operation was cancelled for term: {Term}", request.Term);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting autocomplete suggestions for term: {Term}", request.Term);
            return new AutocompleteResult
            {
                IsSuccessful = false,
                ErrorMessage = "An error occurred while getting autocomplete suggestions",
                Suggestions = new List<AutocompleteSuggestion>()
            };
        }
    }
}