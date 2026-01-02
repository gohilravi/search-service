namespace SearchService.Models;

public class Transport
{
    public int Id { get; set; }
    public int CarrierId { get; set; }
    public int PurchaseId { get; set; }
    public string PickupLocation { get; set; } = string.Empty;
    public string DeliveryLocation { get; set; } = string.Empty;
    public DateTime? ScheduleDate { get; set; }
    public string VehicleDetails { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
    
    // Related entities
    public CarrierInfo? Carrier { get; set; }
}

public class TransportInfo
{
    public int Id { get; set; }
    public int CarrierId { get; set; }
    public int PurchaseId { get; set; }
    public string PickupLocation { get; set; } = string.Empty;
    public string DeliveryLocation { get; set; } = string.Empty;
    public DateTime? ScheduleDate { get; set; }
    public string VehicleDetails { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
    
    // Nested carrier information
    public CarrierInfo? Carrier { get; set; }
}