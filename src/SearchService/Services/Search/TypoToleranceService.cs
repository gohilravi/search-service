using SearchService.Interfaces;

namespace SearchService.Services.Search;

public class TypoToleranceService : ITypoToleranceService
{
    public string ApplyTypoTolerance(string query)
    {
        // This is a simple implementation
        // In production, you might want to use more sophisticated algorithms
        // The actual typo tolerance is handled by Elasticsearch's fuzziness parameter
        return query;
    }
}