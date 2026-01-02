using SearchService.Core.Models;

namespace SearchService.Core.Interfaces;

public interface IDocumentMapper
{
    Dictionary<string, object> MapOfferToDocument(Offer offer);
    Dictionary<string, object> MapPurchaseToDocument(Purchase purchase);
    Dictionary<string, object> MapTransportToDocument(Transport transport);
}

