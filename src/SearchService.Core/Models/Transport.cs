namespace SearchService.Core.Models;

public class Transport
{
    public string Id { get; set; } = string.Empty;
    public string PurchaseId { get; set; } = string.Empty;
    public string Vin { get; set; } = string.Empty;
    public string CarrierId { get; set; } = string.Empty;
    public string BuyerId { get; set; } = string.Empty;
    public string SellerId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string OriginLocation { get; set; } = string.Empty;
    public string DestinationLocation { get; set; } = string.Empty;
    public DateTime? PickupDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

