using SearchService.Core.Interfaces;
using SearchService.Core.Models;
using Microsoft.Extensions.Logging;

namespace SearchService.Infrastructure.Messaging;

public class EventProcessor : IEventProcessor
{
    private readonly IEnumerable<IEventHandler> _eventHandlers;
    private readonly ILogger<EventProcessor> _logger;

    public EventProcessor(IEnumerable<IEventHandler> eventHandlers, ILogger<EventProcessor> logger)
    {
        _eventHandlers = eventHandlers;
        _logger = logger;
    }

    public async Task ProcessEventAsync(EventMessage eventMessage)
    {
        var handler = _eventHandlers.FirstOrDefault(h => h.CanHandle(eventMessage.EntityType));
        
        if (handler == null)
        {
            _logger.LogWarning("No handler found for entity type: {EntityType}", eventMessage.EntityType);
            return;
        }

        try
        {
            await handler.HandleAsync(eventMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling event {EventType} for {EntityType}", 
                eventMessage.EventType, eventMessage.EntityType);
            throw;
        }
    }
}

