using SearchService.Models;
using Nest;

namespace SearchService.Interfaces;

public interface IAccessControlService
{
    UserContext CreateUserContext(string userType, string accountId, string userId);
    bool HasAccess(UserContext userContext, string entityType, Dictionary<string, object> document);
    QueryContainer GetAccessFilter(UserContext userContext);
}