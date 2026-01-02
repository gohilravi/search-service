namespace SearchService.Models;

public class SyncRecordInElasticSearch
{
    public string ElasticSearchId { get; set; } = string.Empty;
    public string ObjectType { get; set; } = string.Empty; // Offer, Purchase, Transport, Seller, Buyer, Carrier
    public string Operation { get; set; } = string.Empty; // Create, Update, Delete
    public string Payload { get; set; } = string.Empty; // JSON string
}

public enum EntityType
{
    Offer,
    Purchase,
    Transport,
    Seller,
    Buyer,
    Carrier
}

public enum OperationType
{
    Create,
    Update,
    Delete
}