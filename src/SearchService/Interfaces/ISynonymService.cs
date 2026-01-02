namespace SearchService.Interfaces;

public interface ISynonymService
{
    string ExpandSynonyms(string query);
    List<string> GetSynonyms(string term);
}