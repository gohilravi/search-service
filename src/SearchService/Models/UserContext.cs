namespace SearchService.Models;

public class UserContext
{
    public string UserType { get; set; } = string.Empty; // Seller, Buyer, Carrier, Agent
    public string AccountId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
}