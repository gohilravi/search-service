using SearchService.Core.Interfaces;
using SearchService.Core.Models;

namespace SearchService.Infrastructure.Elasticsearch;

public class DocumentMapper : IDocumentMapper
{
    public Dictionary<string, object> MapOfferToDocument(Offer offer)
    {
        return new Dictionary<string, object>
        {
            { "entityType", "Offer" },
            { "id", offer.Id },
            { "vin", offer.Vin },
            { "make", offer.Make },
            { "model", offer.Model },
            { "year", offer.Year },
            { "location", offer.Location },
            { "status", offer.Status },
            { "sellerId", offer.SellerId },
            { "price", offer.Price ?? 0 },
            { "createdAt", offer.CreatedAt },
            { "updatedAt", offer.UpdatedAt },
            { "suggest", new[] { offer.Make, offer.Model, offer.Vin, offer.Location } }
        };
    }

    public Dictionary<string, object> MapPurchaseToDocument(Purchase purchase)
    {
        return new Dictionary<string, object>
        {
            { "entityType", "Purchase" },
            { "id", purchase.Id },
            { "offerId", purchase.OfferId },
            { "vin", purchase.Vin },
            { "buyerId", purchase.BuyerId },
            { "sellerId", purchase.SellerId },
            { "status", purchase.Status },
            { "location", purchase.Location },
            { "amount", purchase.Amount ?? 0 },
            { "createdAt", purchase.CreatedAt },
            { "updatedAt", purchase.UpdatedAt },
            { "suggest", new[] { purchase.Vin, purchase.Location } }
        };
    }

    public Dictionary<string, object> MapTransportToDocument(Transport transport)
    {
        return new Dictionary<string, object>
        {
            { "entityType", "Transport" },
            { "id", transport.Id },
            { "purchaseId", transport.PurchaseId },
            { "vin", transport.Vin },
            { "carrierId", transport.CarrierId },
            { "buyerId", transport.BuyerId },
            { "sellerId", transport.SellerId },
            { "status", transport.Status },
            { "originLocation", transport.OriginLocation },
            { "destinationLocation", transport.DestinationLocation },
            { "pickupDate", transport.PickupDate.HasValue ? transport.PickupDate.Value : (object?)null },
            { "deliveryDate", transport.DeliveryDate.HasValue ? transport.DeliveryDate.Value : (object?)null },
            { "createdAt", transport.CreatedAt },
            { "updatedAt", transport.UpdatedAt },
            { "suggest", new[] { transport.Vin, transport.OriginLocation, transport.DestinationLocation } }
        };
    }
}

