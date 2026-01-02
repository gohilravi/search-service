using SearchService.Models;

namespace SearchService.Interfaces;

public interface IDocumentMapper
{
    Dictionary<string, object> MapOfferToDocument(Offer offer);
    Dictionary<string, object> MapPurchaseToDocument(Purchase purchase);
    Dictionary<string, object> MapTransportToDocument(Transport transport);
    Dictionary<string, object> MapCarrierToDocument(Carrier carrier);
    Dictionary<string, object> MapBuyerToDocument(Buyer buyer);
    Dictionary<string, object> MapSellerToDocument(Seller seller);
}