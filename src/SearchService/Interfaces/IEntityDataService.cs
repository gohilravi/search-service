using SearchService.Models;

namespace SearchService.Interfaces;

/// <summary>
/// Interface for fetching related entity data from external services or databases
/// </summary>
public interface IEntityDataService
{
    Task<SellerInfo?> GetSellerAsync(int sellerId, CancellationToken cancellationToken = default);
    Task<BuyerInfo?> GetBuyerAsync(int buyerId, CancellationToken cancellationToken = default);
    Task<CarrierInfo?> GetCarrierAsync(int carrierId, CancellationToken cancellationToken = default);
    Task<Offer?> GetOfferAsync(long offerId, CancellationToken cancellationToken = default);
    Task<Purchase?> GetPurchaseAsync(int purchaseId, CancellationToken cancellationToken = default);
    Task<Transport?> GetTransportAsync(int transportId, CancellationToken cancellationToken = default);
    Task<List<Purchase>> GetPurchasesByOfferIdAsync(long offerId, CancellationToken cancellationToken = default);
    Task<List<Transport>> GetTransportsByOfferIdAsync(long offerId, CancellationToken cancellationToken = default);
    Task<List<Transport>> GetTransportsByPurchaseIdAsync(int purchaseId, CancellationToken cancellationToken = default);
}