namespace SearchService.Models;

public class Purchase
{
    public int Id { get; set; }
    public int BuyerId { get; set; }
    public int OfferId { get; set; }
    public DateTime PurchaseDate { get; set; }
    public decimal Amount { get; set; }
    public string BuyerInfo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
    
    // Related entities
    public BuyerInfo? Buyer { get; set; }
}

public class PurchaseInfo
{
    public int Id { get; set; }
    public int BuyerId { get; set; }
    public int OfferId { get; set; }
    public DateTime PurchaseDate { get; set; }
    public decimal Amount { get; set; }
    public string BuyerInfo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
    
    // Nested buyer information
    public BuyerInfo? Buyer { get; set; }
}