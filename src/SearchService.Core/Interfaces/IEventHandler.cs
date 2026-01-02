using SearchService.Core.Models;

namespace SearchService.Core.Interfaces;

public interface IEventHandler
{
    Task HandleAsync(EventMessage eventMessage);
    bool CanHandle(string entityType);
}

