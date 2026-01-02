namespace SearchService.Interfaces;

public interface ITypoToleranceService
{
    string ApplyTypoTolerance(string query);
}