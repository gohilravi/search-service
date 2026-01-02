using SearchService.Interfaces;
using SearchService.Models;
using Nest;

namespace SearchService.Services.Security;

public class AccessControlService : IAccessControlService
{
    private readonly IQueryFilterBuilder _queryFilterBuilder;

    public AccessControlService(IQueryFilterBuilder queryFilterBuilder)
    {
        _queryFilterBuilder = queryFilterBuilder;
    }

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
            // Seller: can view offers created by them
            // When searching by VIN (or any search criteria), if the VIN matches an Offer's VIN and sellerId matches,
            // the seller can view the offer details (VIN is shared across Offer, Purchase, and Transport entities)
            "seller" => entityType.Equals("Offer", StringComparison.OrdinalIgnoreCase) &&
                       document.GetValueOrDefault("sellerId")?.ToString() == userContext.AccountId,
            // Buyer: can view purchases associated with offers
            // When searching by VIN (or any search criteria), if the VIN matches a Purchase's VIN (which is linked to an Offer via offerId),
            // and buyerId matches, the buyer can view the purchase details
            // The VIN relationship allows matching purchases even when the search initially matches an Offer's VIN
            "buyer" => entityType.Equals("Purchase", StringComparison.OrdinalIgnoreCase) &&
                      document.GetValueOrDefault("buyerId")?.ToString() == userContext.AccountId &&
                      document.ContainsKey("offerId") && // Purchase must be associated with an offer
                      !string.IsNullOrEmpty(document.GetValueOrDefault("offerId")?.ToString()),
            // Carrier: can view transports associated with offers
            // When searching by VIN (or any search criteria), if the VIN matches a Transport's VIN (which is linked to an Offer via purchaseId->offerId),
            // and carrierId matches, the carrier can view the transport details
            // The VIN relationship allows matching transports even when the search initially matches an Offer's VIN
            "carrier" => entityType.Equals("Transport", StringComparison.OrdinalIgnoreCase) &&
                        document.GetValueOrDefault("carrierId")?.ToString() == userContext.AccountId &&
                        document.ContainsKey("purchaseId") && // Transport must be associated with a purchase (which links to offerId via Purchase.offerId)
                        !string.IsNullOrEmpty(document.GetValueOrDefault("purchaseId")?.ToString()),
            _ => false
        };
    }

    public QueryContainer GetAccessFilter(UserContext userContext)
    {
        return _queryFilterBuilder.BuildAccessFilter(userContext);
    }
}