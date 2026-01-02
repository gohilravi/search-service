namespace SearchService.Models;

/// <summary>
/// Master Elasticsearch document that represents the unified index
/// Contains offer information with nested purchases and transports
/// </summary>
public class ElasticSearchOfferDocument
{
    public string Id { get; set; } = string.Empty; // ElasticSearchId from sync event
    public long OfferId { get; set; }
    public int SellerId { get; set; }
    public string SellerNetworkId { get; set; } = string.Empty;
    public string SellerName { get; set; } = string.Empty;
    
    // Vehicle Identification Number
    public string Vin { get; set; } = string.Empty;
    
    // Vehicle Information
    public string VehicleYear { get; set; } = string.Empty;
    public string VehicleMake { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    public string VehicleTrim { get; set; } = string.Empty;
    public string VehicleBodyType { get; set; } = string.Empty;
    public string VehicleCabType { get; set; } = string.Empty;
    public int VehicleDoorCount { get; set; }
    public string VehicleFuelType { get; set; } = string.Empty;
    public string VehicleBodyStyle { get; set; } = string.Empty;
    public string VehicleUsage { get; set; } = string.Empty;
    
    // Location
    public string VehicleZipCode { get; set; } = string.Empty;
    
    // Ownership
    public string OwnershipType { get; set; } = string.Empty;
    public string OwnershipTitleType { get; set; } = string.Empty;
    
    // Condition
    public int Mileage { get; set; }
    public bool IsMileageUnverifiable { get; set; }
    public string DrivetrainCondition { get; set; } = string.Empty;
    public string KeyOrFobAvailable { get; set; } = string.Empty;
    public string WorkingBatteryInstalled { get; set; } = string.Empty;
    public string AllTiresInflated { get; set; } = string.Empty;
    public string WheelsRemoved { get; set; } = string.Empty;
    public bool WheelsRemovedDriverFront { get; set; }
    public bool WheelsRemovedDriverRear { get; set; }
    public bool WheelsRemovedPassengerFront { get; set; }
    public bool WheelsRemovedPassengerRear { get; set; }
    public string BodyPanelsIntact { get; set; } = string.Empty;
    public string BodyDamageFree { get; set; } = string.Empty;
    public string MirrorsLightsGlassIntact { get; set; } = string.Empty;
    public string InteriorIntact { get; set; } = string.Empty;
    public string FloodFireDamageFree { get; set; } = string.Empty;
    public string EngineTransmissionCondition { get; set; } = string.Empty;
    public string AirbagsDeployed { get; set; } = string.Empty;
    
    // Offer Meta
    public string Status { get; set; } = string.Empty;
    public Guid? PurchaseId { get; set; }
    public Guid? TransportId { get; set; }
    public Guid NoSQLIndexId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
    
    // Related entities - These are nested objects in Elasticsearch
    public SellerInfo? Seller { get; set; }
    public List<PurchaseInfo> Purchases { get; set; } = new();
    public List<TransportInfo> Transports { get; set; } = new();
    
    // Additional search fields for efficient querying
    public List<string> SearchableText { get; set; } = new(); // Contains all searchable text for multi-field search
    public Dictionary<string, string> Tags { get; set; } = new(); // For custom tagging and filtering
}