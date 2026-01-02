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
            "seller" => q.Term(t => t.Field("sellerId").Value(userContext.AccountId)),
            "buyer" => q.Bool(b => b.Should(
                q.Term(t => t.Field("entityType").Value("Offer")), // Available offers
                q.Term(t => t.Field("buyerId").Value(userContext.AccountId)) // Their purchases
            )),
            "carrier" => q.Bool(b => b.Should(
                q.Term(t => t.Field("carrierId").Value(userContext.AccountId)),
                q.Term(t => t.Field("entityType").Value("Transport"))
            )),
            _ => q.MatchNone() // No access by default
        };
    }
}

