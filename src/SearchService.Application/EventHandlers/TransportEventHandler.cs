using System.Text.Json;
using SearchService.Core.Interfaces;
using SearchService.Core.Models;
using Microsoft.Extensions.Logging;

namespace SearchService.Application.EventHandlers;

public class TransportEventHandler : IEventHandler
{
    private readonly IElasticsearchService _elasticsearchService;
    private readonly IDocumentMapper _documentMapper;
    private readonly ILogger<TransportEventHandler> _logger;
    private const string IndexName = "search_index_v1";

    public TransportEventHandler(
        IElasticsearchService elasticsearchService,
        IDocumentMapper documentMapper,
        ILogger<TransportEventHandler> logger)
    {
        _elasticsearchService = elasticsearchService;
        _documentMapper = documentMapper;
        _logger = logger;
    }

    public bool CanHandle(string entityType)
    {
        return entityType.Equals("Transport", StringComparison.OrdinalIgnoreCase);
    }

    public async Task HandleAsync(EventMessage eventMessage)
    {
        try
        {
            var transport = JsonSerializer.Deserialize<Transport>(
                JsonSerializer.Serialize(eventMessage.Payload),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (transport == null)
            {
                _logger.LogWarning("Failed to deserialize Transport from event payload");
                return;
            }

            switch (eventMessage.EventType.ToLowerInvariant())
            {
                case "transportcreated":
                case "transportupdated":
                    var document = _documentMapper.MapTransportToDocument(transport);
                    await _elasticsearchService.IndexDocumentAsync(document, IndexName);
                    _logger.LogInformation("Indexed Transport {Id}", transport.Id);
                    break;

                case "transportdeleted":
                    await _elasticsearchService.DeleteDocumentAsync(transport.Id, IndexName);
                    _logger.LogInformation("Deleted Transport {Id} from index", transport.Id);
                    break;

                default:
                    _logger.LogWarning("Unknown event type: {EventType}", eventMessage.EventType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Transport event {EventType}", eventMessage.EventType);
            throw;
        }
    }
}

