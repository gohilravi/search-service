namespace SearchService.Core.Models;

public class SyncRecordInElasticSearch
{
    public string ElasticSearchId { get; set; } = string.Empty;
    public string ObjectType { get; set; } = string.Empty; // offer, purchase, transport
    public string Operation { get; set; } = string.Empty; // Create, Update
    public Dictionary<string, object> Payload { get; set; } = new();
}

