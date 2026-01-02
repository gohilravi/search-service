namespace SearchService.Models;

public class SearchResult
{
    public bool IsSuccessful { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public long TotalResults { get; set; }
    public List<object> Results { get; set; } = new();
    public Dictionary<string, object>? Aggregations { get; set; }
    public long ExecutionTimeMs { get; set; }
    
    // Legacy properties for backward compatibility
    public List<SearchResultItem> Items { get; set; } = new();
    public long TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class SearchResultItem
{
    public string Id { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string Vin { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int? Year { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, object> AdditionalFields { get; set; } = new();
    public double Score { get; set; }
}