using System.Text.Json;
using SearchService.Core.Interfaces;
using SearchService.Core.Models;
using Microsoft.Extensions.Logging;

namespace SearchService.Application.EventHandlers;

public class OfferEventHandler : IEventHandler
{
    private readonly IElasticsearchService _elasticsearchService;
    private readonly IDocumentMapper _documentMapper;
    private readonly ILogger<OfferEventHandler> _logger;
    private const string IndexName = "search_index_v1";

    public OfferEventHandler(
        IElasticsearchService elasticsearchService,
        IDocumentMapper documentMapper,
        ILogger<OfferEventHandler> logger)
    {
        _elasticsearchService = elasticsearchService;
        _documentMapper = documentMapper;
        _logger = logger;
    }

    public bool CanHandle(string entityType)
    {
        return entityType.Equals("Offer", StringComparison.OrdinalIgnoreCase);
    }

    public async Task HandleAsync(EventMessage eventMessage)
    {
        try
        {
            var offer = JsonSerializer.Deserialize<Offer>(
                JsonSerializer.Serialize(eventMessage.Payload),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (offer == null)
            {
                _logger.LogWarning("Failed to deserialize Offer from event payload");
                return;
            }

            switch (eventMessage.EventType.ToLowerInvariant())
            {
                case "offercreated":
                case "offerupdated":
                    var document = _documentMapper.MapOfferToDocument(offer);
                    await _elasticsearchService.IndexDocumentAsync(document, IndexName);
                    _logger.LogInformation("Indexed Offer {Id}", offer.Id);
                    break;

                case "offerdeleted":
                    await _elasticsearchService.DeleteDocumentAsync(offer.Id, IndexName);
                    _logger.LogInformation("Deleted Offer {Id} from index", offer.Id);
                    break;

                default:
                    _logger.LogWarning("Unknown event type: {EventType}", eventMessage.EventType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Offer event {EventType}", eventMessage.EventType);
            throw;
        }
    }
}

