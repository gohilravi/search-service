namespace SearchService.Application.Search;

public interface ISynonymService
{
    string ExpandSynonyms(string query);
    List<string> GetSynonyms(string term);
}

public class SynonymService : ISynonymService
{
    private static readonly Dictionary<string, List<string>> SynonymMap = new()
    {
        { "car", new List<string> { "vehicle", "automobile", "auto" } },
        { "vehicle", new List<string> { "car", "automobile", "auto" } },
        { "truck", new List<string> { "pickup", "lorry" } },
        { "suv", new List<string> { "sport utility vehicle", "sport-utility" } }
    };

    public string ExpandSynonyms(string query)
    {
        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var expandedTerms = new List<string>();

        foreach (var term in terms)
        {
            var lowerTerm = term.ToLowerInvariant();
            expandedTerms.Add(term);

            if (SynonymMap.TryGetValue(lowerTerm, out var synonyms))
            {
                expandedTerms.AddRange(synonyms);
            }
        }

        return string.Join(" ", expandedTerms.Distinct());
    }

    public List<string> GetSynonyms(string term)
    {
        var lowerTerm = term.ToLowerInvariant();
        return SynonymMap.TryGetValue(lowerTerm, out var synonyms) ? synonyms : new List<string>();
    }
}

