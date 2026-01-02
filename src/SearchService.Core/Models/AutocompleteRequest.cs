namespace SearchService.Core.Models;

public class AutocompleteRequest
{
    public string Query { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public int Limit { get; set; } = 10;
}

