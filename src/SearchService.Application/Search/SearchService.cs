using SearchService.Core.Interfaces;
using SearchService.Core.Models;
using SearchService.Application.Security;
using Microsoft.Extensions.Logging;

namespace SearchService.Application.Search;

public interface ISearchService
{
    Task<SearchResult> SearchAsync(SearchRequest request, UserContext userContext);
}

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

    public async Task<SearchResult> SearchAsync(SearchRequest request, UserContext userContext)
    {
        // Apply synonym expansion
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            request.Query = _synonymService.ExpandSynonyms(request.Query);
        }

        // Entity detection could be used for specialized queries
        var detectedEntities = _entityDetectionService.DetectEntities(request.Query ?? string.Empty);
        if (detectedEntities.ContainsKey("vin"))
        {
            // Could enhance query for VIN search
            _logger.LogInformation("Detected VIN in query: {Vin}", detectedEntities["vin"]);
        }

        // Build RBAC access filter
        Func<Nest.QueryContainerDescriptor<Dictionary<string, object>>, Nest.QueryContainer> accessFilter = 
            q => _filterBuilder.BuildAccessFilter(userContext, q);

        return await _elasticsearchService.SearchAsync(request, userContext, (object)accessFilter);
    }
}

