using Microsoft.AspNetCore.Mvc;
using SearchService.Application.Search;
using SearchService.Application.Security;
using SearchService.Core.Models;

namespace SearchService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly IAutocompleteService _autocompleteService;
    private readonly IAccessControlService _accessControlService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        ISearchService searchService,
        IAutocompleteService autocompleteService,
        IAccessControlService accessControlService,
        ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _autocompleteService = autocompleteService;
        _accessControlService = accessControlService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<SearchResult>> Search([FromBody] SearchRequest request)
    {
        try
        {
            var userContext = _accessControlService.CreateUserContext(
                request.UserType,
                request.AccountId,
                request.UserId);

            var result = await _searchService.SearchAsync(request, userContext);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing search");
            return StatusCode(500, new { error = "An error occurred while processing your search request" });
        }
    }

    [HttpGet("autocomplete")]
    public async Task<ActionResult<AutocompleteResult>> Autocomplete(
        [FromQuery] string query,
        [FromQuery] string userType,
        [FromQuery] string accountId,
        [FromQuery] int limit = 10)
    {
        try
        {
            var request = new AutocompleteRequest
            {
                Query = query ?? string.Empty,
                UserType = userType ?? string.Empty,
                AccountId = accountId ?? string.Empty,
                Limit = limit
            };

            var userContext = _accessControlService.CreateUserContext(
                request.UserType,
                request.AccountId,
                string.Empty);

            var result = await _autocompleteService.GetSuggestionsAsync(request, userContext);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting autocomplete suggestions");
            return StatusCode(500, new { error = "An error occurred while processing autocomplete request" });
        }
    }

    [HttpGet("health")]
    public ActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}

