using System.Text.Json;
using SearchService.Core.Interfaces;
using SearchService.Core.Models;
using Microsoft.Extensions.Logging;

namespace SearchService.Application.EventHandlers;

public class PurchaseEventHandler : IEventHandler
{
    private readonly IElasticsearchService _elasticsearchService;
    private readonly IDocumentMapper _documentMapper;
    private readonly ILogger<PurchaseEventHandler> _logger;
    private const string IndexName = "search_index_v1";

    public PurchaseEventHandler(
        IElasticsearchService elasticsearchService,
        IDocumentMapper documentMapper,
        ILogger<PurchaseEventHandler> logger)
    {
        _elasticsearchService = elasticsearchService;
        _documentMapper = documentMapper;
        _logger = logger;
    }

    public bool CanHandle(string entityType)
    {
        return entityType.Equals("Purchase", StringComparison.OrdinalIgnoreCase);
    }

    public async Task HandleAsync(EventMessage eventMessage)
    {
        try
        {
            var purchase = JsonSerializer.Deserialize<Purchase>(
                JsonSerializer.Serialize(eventMessage.Payload),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (purchase == null)
            {
                _logger.LogWarning("Failed to deserialize Purchase from event payload");
                return;
            }

            switch (eventMessage.EventType.ToLowerInvariant())
            {
                case "purchasecreated":
                case "purchaseupdated":
                    var document = _documentMapper.MapPurchaseToDocument(purchase);
                    await _elasticsearchService.IndexDocumentAsync(document, IndexName);
                    _logger.LogInformation("Indexed Purchase {Id}", purchase.Id);
                    break;

                case "purchasedeleted":
                    await _elasticsearchService.DeleteDocumentAsync(purchase.Id, IndexName);
                    _logger.LogInformation("Deleted Purchase {Id} from index", purchase.Id);
                    break;

                default:
                    _logger.LogWarning("Unknown event type: {EventType}", eventMessage.EventType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Purchase event {EventType}", eventMessage.EventType);
            throw;
        }
    }
}

