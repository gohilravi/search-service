using Microsoft.Extensions.Logging;
using SearchService.Interfaces;
using SearchService.Models;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace SearchService.Services;

/// <summary>
/// Service for fetching entity data from external sources
/// This implementation uses HTTP clients to call external services
/// In production, you might want to use database repositories or other data sources
/// </summary>
public class EntityDataService : IEntityDataService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EntityDataService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public EntityDataService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<EntityDataService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<SellerInfo?> GetSellerAsync(int sellerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var baseUrl = _configuration["ExternalServices:SellerService:BaseUrl"] ?? "http://localhost:5001";
            var response = await _httpClient.GetAsync($"{baseUrl}/api/sellers/{sellerId}", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch seller {SellerId}: {StatusCode}", sellerId, response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var seller = JsonSerializer.Deserialize<Seller>(json, _jsonOptions);
            
            if (seller == null)
            {
                _logger.LogWarning("Deserialized seller {SellerId} is null", sellerId);
                return null;
            }

            return new SellerInfo
            {
                SellerId = seller.SellerId,
                Name = seller.Name,
                Email = seller.Email,
                CreatedAt = seller.CreatedAt,
                LastModifiedAt = seller.LastModifiedAt
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request to fetch seller {SellerId} was cancelled", sellerId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching seller {SellerId}", sellerId);
            return null;
        }
    }

    public async Task<BuyerInfo?> GetBuyerAsync(int buyerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var baseUrl = _configuration["ExternalServices:BuyerService:BaseUrl"] ?? "http://localhost:5002";
            var response = await _httpClient.GetAsync($"{baseUrl}/api/buyers/{buyerId}", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch buyer {BuyerId}: {StatusCode}", buyerId, response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var buyer = JsonSerializer.Deserialize<Buyer>(json, _jsonOptions);
            
            if (buyer == null)
            {
                _logger.LogWarning("Deserialized buyer {BuyerId} is null", buyerId);
                return null;
            }

            return new BuyerInfo
            {
                BuyerId = buyer.Id,
                Name = buyer.Name,
                Email = buyer.Email,
                Phone = buyer.Phone,
                Company = buyer.Company,
                CreatedAt = buyer.CreatedAt,
                LastModifiedAt = buyer.LastModifiedAt
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request to fetch buyer {BuyerId} was cancelled", buyerId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching buyer {BuyerId}", buyerId);
            return null;
        }
    }

    public async Task<CarrierInfo?> GetCarrierAsync(int carrierId, CancellationToken cancellationToken = default)
    {
        try
        {
            var baseUrl = _configuration["ExternalServices:CarrierService:BaseUrl"] ?? "http://localhost:5003";
            var response = await _httpClient.GetAsync($"{baseUrl}/api/carriers/{carrierId}", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch carrier {CarrierId}: {StatusCode}", carrierId, response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var carrier = JsonSerializer.Deserialize<Carrier>(json, _jsonOptions);
            
            if (carrier == null)
            {
                _logger.LogWarning("Deserialized carrier {CarrierId} is null", carrierId);
                return null;
            }

            return new CarrierInfo
            {
                Id = carrier.Id,
                Name = carrier.Name,
                Email = carrier.Email,
                Phone = carrier.Phone,
                CreatedAt = carrier.CreatedAt,
                LastModifiedAt = carrier.LastModifiedAt
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request to fetch carrier {CarrierId} was cancelled", carrierId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching carrier {CarrierId}", carrierId);
            return null;
        }
    }

    public async Task<Offer?> GetOfferAsync(long offerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var baseUrl = _configuration["ExternalServices:SellerService:BaseUrl"] ?? "http://localhost:5001";
            var response = await _httpClient.GetAsync($"{baseUrl}/api/offers/{offerId}", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch offer {OfferId}: {StatusCode}", offerId, response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<Offer>(json, _jsonOptions);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request to fetch offer {OfferId} was cancelled", offerId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching offer {OfferId}", offerId);
            return null;
        }
    }

    public async Task<Purchase?> GetPurchaseAsync(int purchaseId, CancellationToken cancellationToken = default)
    {
        try
        {
            var baseUrl = _configuration["ExternalServices:BuyerService:BaseUrl"] ?? "http://localhost:5002";
            var response = await _httpClient.GetAsync($"{baseUrl}/api/purchases/{purchaseId}", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch purchase {PurchaseId}: {StatusCode}", purchaseId, response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<Purchase>(json, _jsonOptions);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request to fetch purchase {PurchaseId} was cancelled", purchaseId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching purchase {PurchaseId}", purchaseId);
            return null;
        }
    }

    public async Task<Transport?> GetTransportAsync(int transportId, CancellationToken cancellationToken = default)
    {
        try
        {
            var baseUrl = _configuration["ExternalServices:TransportService:BaseUrl"] ?? "http://localhost:5003";
            var response = await _httpClient.GetAsync($"{baseUrl}/api/transports/{transportId}", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch transport {TransportId}: {StatusCode}", transportId, response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<Transport>(json, _jsonOptions);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request to fetch transport {TransportId} was cancelled", transportId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching transport {TransportId}", transportId);
            return null;
        }
    }

    public async Task<List<Purchase>> GetPurchasesByOfferIdAsync(long offerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var baseUrl = _configuration["ExternalServices:BuyerService:BaseUrl"] ?? "http://localhost:5002";
            var response = await _httpClient.GetAsync($"{baseUrl}/api/purchases/by-offer/{offerId}", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch purchases for offer {OfferId}: {StatusCode}", offerId, response.StatusCode);
                return new List<Purchase>();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<Purchase>>(json, _jsonOptions) ?? new List<Purchase>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching purchases for offer {OfferId}", offerId);
            return new List<Purchase>();
        }
    }

    public async Task<List<Transport>> GetTransportsByOfferIdAsync(long offerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var baseUrl = _configuration["ExternalServices:TransportService:BaseUrl"] ?? "http://localhost:5003";
            var response = await _httpClient.GetAsync($"{baseUrl}/api/transports/by-offer/{offerId}", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch transports for offer {OfferId}: {StatusCode}", offerId, response.StatusCode);
                return new List<Transport>();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<Transport>>(json, _jsonOptions) ?? new List<Transport>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching transports for offer {OfferId}", offerId);
            return new List<Transport>();
        }
    }

    public async Task<List<Transport>> GetTransportsByPurchaseIdAsync(int purchaseId, CancellationToken cancellationToken = default)
    {
        try
        {
            var baseUrl = _configuration["ExternalServices:TransportService:BaseUrl"] ?? "http://localhost:5003";
            var response = await _httpClient.GetAsync($"{baseUrl}/api/transports/by-purchase/{purchaseId}", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch transports for purchase {PurchaseId}: {StatusCode}", purchaseId, response.StatusCode);
                return new List<Transport>();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<Transport>>(json, _jsonOptions) ?? new List<Transport>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching transports for purchase {PurchaseId}", purchaseId);
            return new List<Transport>();
        }
    }
}