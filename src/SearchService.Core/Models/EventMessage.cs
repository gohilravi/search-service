namespace SearchService.Core.Models;

public class EventMessage
{
    public string EventType { get; set; } = string.Empty; // OfferCreated, OfferUpdated, OfferDeleted, etc.
    public string EntityType { get; set; } = string.Empty; // Offer, Purchase, Transport
    public object Payload { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

