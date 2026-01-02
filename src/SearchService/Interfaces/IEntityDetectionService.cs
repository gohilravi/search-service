namespace SearchService.Interfaces;

public interface IEntityDetectionService
{
    bool IsVin(string query);
    bool IsPhoneNumber(string query);
    bool IsId(string query);
    Dictionary<string, string> DetectEntities(string query);
}