using SearchService.Interfaces;
using SearchService.Models;
using SearchService.Services.Security;
using Microsoft.Extensions.Logging;

namespace SearchService.Services.Search;

public class SearchService : ISearchService
{
    private readonly IElasticsearchService _elasticsearchService;
    private readonly IQueryFilterBuilder _filterBuilder;
    private readonly ISynonymService _synonymService;
    private readonly IEntityDetectionService _entityDetectionService;
    private readonly ILogger<SearchService> _logger;

    public SearchService(
        IElasticsearchService elasticsearchService,
        IQueryFilterBuilder filterBuilder,
        ISynonymService synonymService,
        IEntityDetectionService entityDetectionService,
        ILogger<SearchService> logger)
    {
        _elasticsearchService = elasticsearchService;
        _filterBuilder = filterBuilder;
        _synonymService = synonymService;
        _entityDetectionService = entityDetectionService;
        _logger = logger;
    }

    public async Task<SearchResult> SearchAsync(SearchRequest request, UserContext userContext, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing search request: {Query} for user {UserId}", request.Query, userContext.UserId);

            // Apply synonym expansion
            if (!string.IsNullOrWhiteSpace(request.Query))
            {
                request.Query = _synonymService.ExpandSynonyms(request.Query);
                _logger.LogDebug("Query after synonym expansion: {Query}", request.Query);
            }

            // Entity detection could be used for specialized queries
            var detectedEntities = _entityDetectionService.DetectEntities(request.Query ?? string.Empty);
            if (detectedEntities.ContainsKey("vin"))
            {
                // Could enhance query for VIN search
                _logger.LogInformation("Detected VIN in query: {Vin}", detectedEntities["vin"]);
            }

            // Build access filter based on user context
            var accessFilter = _filterBuilder.BuildAccessFilter(userContext);

            // Execute search using the unified Elasticsearch service
            var result = await _elasticsearchService.SearchAsync(request, userContext, accessFilter, cancellationToken);

            _logger.LogInformation("Search completed: {TotalResults} results found in {ExecutionTime}ms", 
                result.TotalResults, result.ExecutionTimeMs);

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Search operation was cancelled for query: {Query}", request.Query);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing search for query: {Query}", request.Query);
            return new SearchResult
            {
                IsSuccessful = false,
                ErrorMessage = "An error occurred while executing the search",
                TotalResults = 0,
                Results = new List<object>(),
                ExecutionTimeMs = 0
            };
        }
    }
}