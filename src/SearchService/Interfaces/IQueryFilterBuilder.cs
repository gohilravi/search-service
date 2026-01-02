using SearchService.Models;
using Nest;

namespace SearchService.Interfaces;

public interface IQueryFilterBuilder
{
    QueryContainer BuildSecurityFilter(UserContext userContext);
    QueryContainer CombineFilters(params QueryContainer[] filters);
    QueryContainer BuildAccessFilter(UserContext userContext);
    QueryContainer BuildAccessFilter(UserContext userContext, QueryContainerDescriptor<Dictionary<string, object>> q);
}