using System.Text.RegularExpressions;

namespace SearchService.Application.Search;

public interface IEntityDetectionService
{
    bool IsVin(string query);
    bool IsPhoneNumber(string query);
    bool IsId(string query);
    Dictionary<string, string> DetectEntities(string query);
}

public class EntityDetectionService : IEntityDetectionService
{
    private static readonly Regex VinPattern = new(@"^[A-HJ-NPR-Z0-9]{17}$", RegexOptions.IgnoreCase);
    private static readonly Regex PhonePattern = new(@"^\+?[\d\s\-\(\)]{10,}$");
    private static readonly Regex IdPattern = new(@"^[A-Z0-9\-]{8,}$", RegexOptions.IgnoreCase);

    public bool IsVin(string query)
    {
        return !string.IsNullOrWhiteSpace(query) && VinPattern.IsMatch(query.Trim());
    }

    public bool IsPhoneNumber(string query)
    {
        return !string.IsNullOrWhiteSpace(query) && PhonePattern.IsMatch(query.Trim());
    }

    public bool IsId(string query)
    {
        return !string.IsNullOrWhiteSpace(query) && IdPattern.IsMatch(query.Trim());
    }

    public Dictionary<string, string> DetectEntities(string query)
    {
        var entities = new Dictionary<string, string>();

        if (IsVin(query))
            entities["vin"] = query.Trim();

        if (IsPhoneNumber(query))
            entities["phone"] = query.Trim();

        if (IsId(query))
            entities["id"] = query.Trim();

        return entities;
    }
}

