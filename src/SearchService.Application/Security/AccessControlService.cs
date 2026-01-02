using SearchService.Core.Interfaces;
using SearchService.Core.Models;

namespace SearchService.Application.Security;

public interface IAccessControlService
{
    UserContext CreateUserContext(string userType, string accountId, string userId);
    bool HasAccess(UserContext userContext, string entityType, Dictionary<string, object> document);
}

public class AccessControlService : IAccessControlService
{
    public UserContext CreateUserContext(string userType, string accountId, string userId)
    {
        return new UserContext
        {
            UserType = userType,
            AccountId = accountId,
            UserId = userId
        };
    }

    public bool HasAccess(UserContext userContext, string entityType, Dictionary<string, object> document)
    {
        return userContext.UserType.ToLowerInvariant() switch
        {
            "agent" => true, // Agents have access to all data
            "seller" => document.GetValueOrDefault("sellerId")?.ToString() == userContext.AccountId,
            "buyer" => entityType.Equals("Offer", StringComparison.OrdinalIgnoreCase) || 
                      document.GetValueOrDefault("buyerId")?.ToString() == userContext.AccountId,
            "carrier" => document.GetValueOrDefault("carrierId")?.ToString() == userContext.AccountId ||
                        entityType.Equals("Transport", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
}

