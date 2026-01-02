using SearchService.Interfaces;
using SearchService.Models;

namespace SearchService.Services.Elasticsearch;

public class DocumentMapper : IDocumentMapper
{
    public Dictionary<string, object> MapOfferToDocument(Offer offer)
    {
        return new Dictionary<string, object>
        {
            { "entityType", "Offer" },
            { "id", offer.OfferId },
            { "vin", offer.Vin },
            { "make", offer.VehicleMake },
            { "model", offer.VehicleModel },
            { "year", offer.VehicleYear },
            { "location", offer.VehicleZipCode },
            { "status", offer.Status },
            { "sellerId", offer.SellerId },
            { "mileage", offer.Mileage },
            { "createdAt", offer.CreatedAt },
            { "updatedAt", offer.LastModifiedAt },
            { "suggest", new[] { offer.VehicleMake, offer.VehicleModel, offer.Vin, offer.VehicleZipCode } }
        };
    }

    public Dictionary<string, object> MapPurchaseToDocument(Purchase purchase)
    {
        return new Dictionary<string, object>
        {
            { "entityType", "Purchase" },
            { "id", purchase.Id },
            { "offerId", purchase.OfferId },
            { "buyerId", purchase.BuyerId },
            { "status", purchase.Status },
            { "amount", purchase.Amount },
            { "purchaseDate", purchase.PurchaseDate },
            { "buyerInfo", purchase.BuyerInfo },
            { "createdAt", purchase.CreatedAt },
            { "updatedAt", purchase.LastModifiedAt },
            { "suggest", new[] { purchase.Id.ToString(), purchase.OfferId.ToString() } }
        };
    }

    public Dictionary<string, object> MapTransportToDocument(Transport transport)
    {
        return new Dictionary<string, object>
        {
            { "entityType", "Transport" },
            { "id", transport.Id },
            { "purchaseId", transport.PurchaseId },
            { "carrierId", transport.CarrierId },
            { "status", transport.Status },
            { "pickupLocation", transport.PickupLocation },
            { "deliveryLocation", transport.DeliveryLocation },
            { "scheduleDate", transport.ScheduleDate.HasValue ? transport.ScheduleDate.Value : (object?)null },
            { "vehicleDetails", transport.VehicleDetails },
            { "createdAt", transport.CreatedAt },
            { "updatedAt", transport.LastModifiedAt },
            { "suggest", new[] { transport.Id.ToString(), transport.PickupLocation, transport.DeliveryLocation } }
        };
    }

    public Dictionary<string, object> MapCarrierToDocument(Carrier carrier)
    {
        return new Dictionary<string, object>
        {
            { "entityType", "Carrier" },
            { "id", carrier.Id },
            { "name", carrier.Name },
            { "email", carrier.Email },
            { "phone", carrier.Phone },
            { "createdAt", carrier.CreatedAt },
            { "updatedAt", carrier.LastModifiedAt },
            { "suggest", new[] { carrier.Name, carrier.Email } }
        };
    }

    public Dictionary<string, object> MapBuyerToDocument(Buyer buyer)
    {
        return new Dictionary<string, object>
        {
            { "entityType", "Buyer" },
            { "id", buyer.Id },
            { "name", buyer.Name },
            { "email", buyer.Email },
            { "phone", buyer.Phone },
            { "company", buyer.Company },
            { "createdAt", buyer.CreatedAt },
            { "updatedAt", buyer.LastModifiedAt },
            { "suggest", new[] { buyer.Name, buyer.Email, buyer.Company } }
        };
    }

    public Dictionary<string, object> MapSellerToDocument(Seller seller)
    {
        return new Dictionary<string, object>
        {
            { "entityType", "Seller" },
            { "id", seller.SellerId },
            { "name", seller.Name },
            { "email", seller.Email },
            { "createdAt", seller.CreatedAt },
            { "updatedAt", seller.LastModifiedAt },
            { "suggest", new[] { seller.Name, seller.Email } }
        };
    }
}