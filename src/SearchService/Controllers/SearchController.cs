using Microsoft.AspNetCore.Mvc;
using SearchService.Interfaces;
using SearchService.Models;

namespace SearchService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly IAutocompleteService _autocompleteService;
    private readonly IAccessControlService _accessControlService;
    private readonly IElasticsearchService _elasticsearchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        ISearchService searchService,
        IAutocompleteService autocompleteService,
        IAccessControlService accessControlService,
        IElasticsearchService elasticsearchService,
        ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _autocompleteService = autocompleteService;
        _accessControlService = accessControlService;
        _elasticsearchService = elasticsearchService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<SearchResult>> Search([FromBody] SearchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var userContext = _accessControlService.CreateUserContext(
                request.UserType,
                request.AccountId,
                request.UserId);

            var result = await _searchService.SearchAsync(request, userContext, cancellationToken);
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Search operation was cancelled");
            return BadRequest(new { error = "Search operation was cancelled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing search");
            return StatusCode(500, new { error = "An error occurred while processing your search request" });
        }
    }

    [HttpPost("offers")]
    public async Task<ActionResult<SearchResult>> SearchOffers([FromBody] SearchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var userContext = _accessControlService.CreateUserContext(
                request.UserType,
                request.AccountId,
                request.UserId);

            // Add filter for offers
            var accessFilter = _accessControlService.GetAccessFilter(userContext);
            var result = await _elasticsearchService.SearchAsync(request, userContext, accessFilter, cancellationToken);
            
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Offer search operation was cancelled");
            return BadRequest(new { error = "Search operation was cancelled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing offer search");
            return StatusCode(500, new { error = "An error occurred while processing your offer search request" });
        }
    }

    [HttpGet("offers/{id}")]
    public async Task<ActionResult<ElasticSearchOfferDocument>> GetOffer(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var offer = await _elasticsearchService.GetOfferDocumentByIdAsync(id, cancellationToken);
            if (offer == null)
            {
                return NotFound(new { error = "Offer not found" });
            }

            return Ok(offer);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Get offer operation was cancelled for ID: {OfferId}", id);
            return BadRequest(new { error = "Operation was cancelled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving offer {OfferId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the offer" });
        }
    }

    [HttpGet("autocomplete")]
    public async Task<ActionResult<AutocompleteResult>> Autocomplete(
        [FromQuery] string term,
        [FromQuery] string userType = "",
        [FromQuery] string accountId = "",
        [FromQuery] int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new AutocompleteRequest
            {
                Term = term ?? string.Empty,
                UserType = userType,
                AccountId = accountId,
                MaxResults = maxResults
            };

            var result = await _elasticsearchService.AutocompleteAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Autocomplete operation was cancelled");
            return BadRequest(new { error = "Autocomplete operation was cancelled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting autocomplete suggestions");
            return StatusCode(500, new { error = "An error occurred while processing autocomplete request" });
        }
    }

    [HttpPost("entities/{entityType}/{entityId}")]
    public async Task<ActionResult<List<ElasticSearchOfferDocument>>> FindOffersByEntity(
        string entityType, 
        string entityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var validEntityTypes = new[] { "offer", "purchase", "transport", "seller", "buyer", "carrier" };
            if (!validEntityTypes.Contains(entityType.ToLower()))
            {
                return BadRequest(new { error = $"Invalid entity type. Valid types are: {string.Join(", ", validEntityTypes)}" });
            }

            var offers = await _elasticsearchService.FindOfferDocumentsByEntityIdAsync(entityType, entityId, cancellationToken);
            return Ok(offers);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Find offers by entity operation was cancelled for {EntityType}:{EntityId}", entityType, entityId);
            return BadRequest(new { error = "Operation was cancelled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding offers by entity {EntityType}:{EntityId}", entityType, entityId);
            return StatusCode(500, new { error = "An error occurred while searching for offers" });
        }
    }

    [HttpGet("health")]
    public async Task<ActionResult> Health(CancellationToken cancellationToken = default)
    {
        try
        {
            var indexExists = await _elasticsearchService.OfferIndexExistsAsync(cancellationToken);
            return Ok(new { 
                status = "healthy", 
                timestamp = DateTime.UtcNow,
                offerIndexExists = indexExists
            });
        }
        catch (OperationCanceledException)
        {
            return Ok(new { 
                status = "healthy", 
                timestamp = DateTime.UtcNow,
                note = "Health check was cancelled but service is running"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(500, new { 
                status = "unhealthy", 
                timestamp = DateTime.UtcNow,
                error = ex.Message
            });
        }
    }

    [HttpPost("reindex")]
    public async Task<ActionResult> ReindexOffers(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting manual reindex operation");
            
            var indexExists = await _elasticsearchService.OfferIndexExistsAsync(cancellationToken);
            if (!indexExists)
            {
                var created = await _elasticsearchService.CreateOfferIndexAsync(cancellationToken);
                if (!created)
                {
                    return StatusCode(500, new { error = "Failed to create offer index" });
                }
            }

            return Ok(new { 
                message = "Reindex operation initiated", 
                timestamp = DateTime.UtcNow 
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Reindex operation was cancelled");
            return BadRequest(new { error = "Reindex operation was cancelled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during reindex operation");
            return StatusCode(500, new { error = "An error occurred during reindex operation" });
        }
    }
}