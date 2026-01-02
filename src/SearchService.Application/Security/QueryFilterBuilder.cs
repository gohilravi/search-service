using Nest;
using SearchService.Core.Models;

namespace SearchService.Application.Security;

public interface IQueryFilterBuilder
{
    QueryContainer BuildAccessFilter(UserContext userContext, QueryContainerDescriptor<Dictionary<string, object>> q);
}

public class QueryFilterBuilder : IQueryFilterBuilder
{
    public QueryContainer BuildAccessFilter(UserContext userContext, QueryContainerDescriptor<Dictionary<string, object>> q)
    {
        return userContext.UserType.ToLowerInvariant() switch
        {
            "agent" => q.MatchAll(), // Agents see everything
            // Seller: can view offers created by them
            // When searching by VIN (or any search criteria), if the VIN matches an Offer's VIN and sellerId matches,
            // the seller can view the offer details (VIN is shared across Offer, Purchase, and Transport entities)
            "seller" => q.Bool(b => b.Must(
                q.Term(t => t.Field("entityType").Value("Offer")),
                q.Term(t => t.Field("sellerId").Value(userContext.AccountId))
            )),
            // Buyer: can view purchases associated with offers
            // When searching by VIN (or any search criteria), if the VIN matches a Purchase's VIN (which is linked to an Offer via offerId),
            // and buyerId matches, the buyer can view the purchase details
            // The VIN relationship allows matching purchases even when the search initially matches an Offer's VIN
            "buyer" => q.Bool(b => b.Must(
                q.Term(t => t.Field("entityType").Value("Purchase")),
                q.Term(t => t.Field("buyerId").Value(userContext.AccountId)),
                q.Exists(e => e.Field("offerId")) // Ensure purchase is associated with an offer
            )),
            // Carrier: can view transports associated with offers
            // When searching by VIN (or any search criteria), if the VIN matches a Transport's VIN (which is linked to an Offer via purchaseId->offerId),
            // and carrierId matches, the carrier can view the transport details
            // The VIN relationship allows matching transports even when the search initially matches an Offer's VIN
            "carrier" => q.Bool(b => b.Must(
                q.Term(t => t.Field("entityType").Value("Transport")),
                q.Term(t => t.Field("carrierId").Value(userContext.AccountId)),
                q.Exists(e => e.Field("purchaseId")) // Ensure transport is associated with a purchase (which links to offerId via Purchase.offerId)
            )),
            _ => q.MatchNone() // No access by default
        };
    }
}

