using Nest;
using SearchService.Core.Interfaces;
using SearchService.Core.Models;
using Microsoft.Extensions.Logging;

namespace SearchService.Application.Search;

public interface IAutocompleteService
{
    Task<AutocompleteResult> GetSuggestionsAsync(AutocompleteRequest request, UserContext userContext);
}

public class AutocompleteService : IAutocompleteService
{
    private readonly IElasticClient _client;
    private readonly IQueryFilterBuilder _filterBuilder;
    private readonly ILogger<AutocompleteService> _logger;
    private const string IndexName = "search_index_v1";

    public AutocompleteService(
        IElasticClient client,
        IQueryFilterBuilder filterBuilder,
        ILogger<AutocompleteService> logger)
    {
        _client = client;
        _filterBuilder = filterBuilder;
        _logger = logger;
    }

    public async Task<AutocompleteResult> GetSuggestionsAsync(AutocompleteRequest request, UserContext userContext)
    {
        try
        {
            var response = await _client.SearchAsync<Dictionary<string, object>>(s => s
                .Index(IndexName)
                .Size(request.Limit)
                .Query(q => q
                    .Bool(b => b
                        .Must(m => m
                            .MultiMatch(mm => mm
                                .Query(request.Query)
                                .Fields(f => f.Field("make").Field("model").Field("vin").Field("location"))
                                .Fuzziness(Fuzziness.Auto)))
                        .Filter(f => _filterBuilder.BuildAccessFilter(userContext, f)))));

            if (!response.IsValid)
            {
                _logger.LogError("Autocomplete search failed: {Error}", response.DebugInformation);
                return new AutocompleteResult();
            }

            var suggestions = response.Hits
                .SelectMany(hit => new[]
                {
                    hit.Source?.GetValueOrDefault("make")?.ToString(),
                    hit.Source?.GetValueOrDefault("model")?.ToString(),
                    hit.Source?.GetValueOrDefault("vin")?.ToString(),
                    hit.Source?.GetValueOrDefault("location")?.ToString()
                })
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .Take(request.Limit)
                .ToList();

            return new AutocompleteResult { Suggestions = suggestions };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting autocomplete suggestions");
            return new AutocompleteResult();
        }
    }
}

