namespace SearchService.Models;

public class AutocompleteResult
{
    public bool IsSuccessful { get; set; }
    public List<AutocompleteSuggestion> Suggestions { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class AutocompleteSuggestion
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}