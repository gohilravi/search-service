using SearchService.Core.Models;

namespace SearchService.Core.Interfaces;

public interface IEventProcessor
{
    Task ProcessEventAsync(EventMessage eventMessage);
}

