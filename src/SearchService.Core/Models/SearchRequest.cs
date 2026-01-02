namespace SearchService.Core.Models;

public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty; // Seller, Buyer, Carrier, Agent
    public string AccountId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public List<string>? EntityTypes { get; set; } // Optional filter: Offer, Purchase, Transport
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

