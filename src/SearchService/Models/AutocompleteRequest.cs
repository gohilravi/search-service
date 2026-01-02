namespace SearchService.Models;

public class AutocompleteRequest
{
    public string Term { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public int MaxResults { get; set; } = 10;
}