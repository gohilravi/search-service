using System.Text.Json;
using SearchService.Interfaces;
using SearchService.Models;
using Microsoft.Extensions.Logging;

namespace SearchService.Services.Messaging;

public class SyncRecordProcessor : ISyncRecordProcessor
{
    private readonly IElasticsearchService _elasticsearchService;
    private readonly IEntityDataService _entityDataService;
    private readonly ILogger<SyncRecordProcessor> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public SyncRecordProcessor(
        IElasticsearchService elasticsearchService,
        IEntityDataService entityDataService,
        ILogger<SyncRecordProcessor> logger)
    {
        _elasticsearchService = elasticsearchService;
        _entityDataService = entityDataService;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task ProcessSyncRecordAsync(SyncRecordInElasticSearch command, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing sync record: {ObjectType} {Operation} {ElasticSearchId}", 
                command.ObjectType, command.Operation, command.ElasticSearchId);

            var objectType = command.ObjectType.ToLowerInvariant();
            var operation = command.Operation.ToLowerInvariant();

            switch (operation)
            {
                case "create":
                    await HandleCreateOperationAsync(objectType, command, cancellationToken);
                    break;
                case "update":
                    await HandleUpdateOperationAsync(objectType, command, cancellationToken);
                    break;
                case "delete":
                    await HandleDeleteOperationAsync(objectType, command, cancellationToken);
                    break;
                default:
                    _logger.LogWarning("Unknown operation: {Operation} for ObjectType: {ObjectType}", 
                        command.Operation, command.ObjectType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing sync record: {ObjectType} {Operation} {ElasticSearchId}", 
                command.ObjectType, command.Operation, command.ElasticSearchId);
            throw;
        }
    }

    private async Task HandleCreateOperationAsync(string objectType, SyncRecordInElasticSearch command, CancellationToken cancellationToken)
    {
        switch (objectType)
        {
            case "offer":
                await CreateOfferDocumentAsync(command, cancellationToken);
                break;
            case "purchase":
                await UpdateOffersForPurchaseCreateAsync(command, cancellationToken);
                break;
            case "transport":
                await UpdateOffersForTransportCreateAsync(command, cancellationToken);
                break;
            case "seller":
                await UpdateOffersForSellerAsync(command, cancellationToken);
                break;
            case "buyer":
                await UpdateOffersForBuyerAsync(command, cancellationToken);
                break;
            case "carrier":
                await UpdateOffersForCarrierAsync(command, cancellationToken);
                break;
            default:
                _logger.LogWarning("Unknown object type for create operation: {ObjectType}", objectType);
                break;
        }
    }

    private async Task HandleUpdateOperationAsync(string objectType, SyncRecordInElasticSearch command, CancellationToken cancellationToken)
    {
        switch (objectType)
        {
            case "offer":
                await UpdateOfferDocumentAsync(command, cancellationToken);
                break;
            case "purchase":
                await UpdateOffersForPurchaseUpdateAsync(command, cancellationToken);
                break;
            case "transport":
                await UpdateOffersForTransportUpdateAsync(command, cancellationToken);
                break;
            case "seller":
                await UpdateOffersForSellerAsync(command, cancellationToken);
                break;
            case "buyer":
                await UpdateOffersForBuyerAsync(command, cancellationToken);
                break;
            case "carrier":
                await UpdateOffersForCarrierAsync(command, cancellationToken);
                break;
            default:
                _logger.LogWarning("Unknown object type for update operation: {ObjectType}", objectType);
                break;
        }
    }

    private async Task HandleDeleteOperationAsync(string objectType, SyncRecordInElasticSearch command, CancellationToken cancellationToken)
    {
        // For delete operations, we need to either remove the document or remove references to the deleted entity
        switch (objectType)
        {
            case "offer":
                await _elasticsearchService.DeleteOfferDocumentAsync(command.ElasticSearchId, cancellationToken);
                break;
            default:
                // For other entities, we update all affected offer documents to remove or null out the deleted entity
                var affectedDocuments = await _elasticsearchService.FindOfferDocumentsByEntityIdAsync(objectType, ExtractEntityId(command.Payload), cancellationToken);
                foreach (var doc in affectedDocuments)
                {
                    await RemoveEntityFromOfferDocument(doc, objectType, ExtractEntityId(command.Payload), cancellationToken);
                }
                break;
        }
    }

    private async Task CreateOfferDocumentAsync(SyncRecordInElasticSearch command, CancellationToken cancellationToken)
    {
        try
        {
            var offer = JsonSerializer.Deserialize<Offer>(command.Payload, _jsonOptions);
            if (offer == null)
            {
                _logger.LogError("Failed to deserialize offer payload: {Payload}", command.Payload);
                return;
            }

            var elasticDocument = await BuildElasticSearchOfferDocumentAsync(offer, cancellationToken);
            elasticDocument.Id = command.ElasticSearchId;

            var success = await _elasticsearchService.IndexOfferDocumentAsync(elasticDocument, cancellationToken);
            if (!success)
            {
                _logger.LogError("Failed to index offer document {OfferId}", offer.OfferId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating offer document");
            throw;
        }
    }

    private async Task UpdateOfferDocumentAsync(SyncRecordInElasticSearch command, CancellationToken cancellationToken)
    {
        try
        {
            var offer = JsonSerializer.Deserialize<Offer>(command.Payload, _jsonOptions);
            if (offer == null)
            {
                _logger.LogError("Failed to deserialize offer payload: {Payload}", command.Payload);
                return;
            }

            var elasticDocument = await BuildElasticSearchOfferDocumentAsync(offer, cancellationToken);
            elasticDocument.Id = command.ElasticSearchId;

            var success = await _elasticsearchService.UpdateOfferDocumentAsync(command.ElasticSearchId, elasticDocument, cancellationToken);
            if (!success)
            {
                _logger.LogError("Failed to update offer document {OfferId}", offer.OfferId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating offer document");
            throw;
        }
    }

    private async Task UpdateOffersForPurchaseCreateAsync(SyncRecordInElasticSearch command, CancellationToken cancellationToken)
    {
        try
        {
            var purchase = JsonSerializer.Deserialize<Purchase>(command.Payload, _jsonOptions);
            if (purchase == null)
            {
                _logger.LogError("Failed to deserialize purchase payload: {Payload}", command.Payload);
                return;
            }

            // Find all offers related to this purchase
            var affectedDocuments = await _elasticsearchService.FindOfferDocumentsByEntityIdAsync("offer", purchase.OfferId.ToString(), cancellationToken);
            
            foreach (var doc in affectedDocuments)
            {
                // Add purchase with buyer info to the offer document
                var purchaseInfo = await BuildPurchaseInfoAsync(purchase, cancellationToken);
                if (purchaseInfo != null)
                {
                    doc.Purchases.Add(purchaseInfo);
                    await _elasticsearchService.UpdateOfferDocumentAsync(doc.Id, doc, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating offers for purchase create");
            throw;
        }
    }

    private async Task UpdateOffersForPurchaseUpdateAsync(SyncRecordInElasticSearch command, CancellationToken cancellationToken)
    {
        try
        {
            var purchase = JsonSerializer.Deserialize<Purchase>(command.Payload, _jsonOptions);
            if (purchase == null)
            {
                _logger.LogError("Failed to deserialize purchase payload: {Payload}", command.Payload);
                return;
            }

            // Find all offers that have this purchase
            var affectedDocuments = await _elasticsearchService.FindOfferDocumentsByEntityIdAsync("purchase", purchase.Id.ToString(), cancellationToken);
            
            foreach (var doc in affectedDocuments)
            {
                // Update the purchase in the purchases list
                var existingPurchase = doc.Purchases.FirstOrDefault(p => p.Id == purchase.Id);
                if (existingPurchase != null)
                {
                    var updatedPurchaseInfo = await BuildPurchaseInfoAsync(purchase, cancellationToken);
                    if (updatedPurchaseInfo != null)
                    {
                        var index = doc.Purchases.IndexOf(existingPurchase);
                        doc.Purchases[index] = updatedPurchaseInfo;
                        await _elasticsearchService.UpdateOfferDocumentAsync(doc.Id, doc, cancellationToken);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating offers for purchase update");
            throw;
        }
    }

    private async Task UpdateOffersForTransportCreateAsync(SyncRecordInElasticSearch command, CancellationToken cancellationToken)
    {
        try
        {
            var transport = JsonSerializer.Deserialize<Transport>(command.Payload, _jsonOptions);
            if (transport == null)
            {
                _logger.LogError("Failed to deserialize transport payload: {Payload}", command.Payload);
                return;
            }

            // Use ElasticsearchId directly as OfferId to find the offer document
            var offerId = command.ElasticSearchId;
            _logger.LogInformation("Searching for offer document with ID: {OfferId}", offerId);

            // Find the specific offer document using the ElasticsearchId
            var offerDocument = await _elasticsearchService.GetOfferDocumentByIdAsync(offerId, cancellationToken);
            
            if (offerDocument != null)
            {
                // Add transport with carrier info to the offer document
                var transportInfo = await BuildTransportInfoAsync(transport, cancellationToken);
                if (transportInfo != null)
                {
                    offerDocument.Transports.Add(transportInfo);
                    await _elasticsearchService.UpdateOfferDocumentAsync(offerDocument.Id, offerDocument, cancellationToken);
                    _logger.LogInformation("Successfully updated offer document {OfferId} with transport {TransportId}", offerDocument.Id, transport.Id);
                }
            }
            else
            {
                _logger.LogWarning("Offer document not found with ID: {OfferId}", offerId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating offers for transport create");
            throw;
        }
    }

    private async Task UpdateOffersForTransportUpdateAsync(SyncRecordInElasticSearch command, CancellationToken cancellationToken)
    {
        try
        {
            var transport = JsonSerializer.Deserialize<Transport>(command.Payload, _jsonOptions);
            if (transport == null)
            {
                _logger.LogError("Failed to deserialize transport payload: {Payload}", command.Payload);
                return;
            }

            // Use ElasticsearchId directly as OfferId to find the offer document
            var offerId = command.ElasticSearchId;
            _logger.LogInformation("Searching for offer document with ID: {OfferId} to update transport", offerId);

            // Find the specific offer document using the ElasticsearchId
            var offerDocument = await _elasticsearchService.GetOfferDocumentByIdAsync(offerId, cancellationToken);
            
            if (offerDocument != null)
            {
                // Update the transport in the transports list
                var existingTransport = offerDocument.Transports.FirstOrDefault(t => t.Id == transport.Id);
                if (existingTransport != null)
                {
                    var updatedTransportInfo = await BuildTransportInfoAsync(transport, cancellationToken);
                    if (updatedTransportInfo != null)
                    {
                        var index = offerDocument.Transports.IndexOf(existingTransport);
                        offerDocument.Transports[index] = updatedTransportInfo;
                        await _elasticsearchService.UpdateOfferDocumentAsync(offerDocument.Id, offerDocument, cancellationToken);
                        _logger.LogInformation("Successfully updated transport {TransportId} in offer document {OfferId}", transport.Id, offerDocument.Id);
                    }
                }
                else
                {
                    _logger.LogWarning("Transport {TransportId} not found in offer document {OfferId}", transport.Id, offerDocument.Id);
                }
            }
            else
            {
                _logger.LogWarning("Offer document not found with ID: {OfferId}", offerId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating offers for transport update");
            throw;
        }
    }

    private async Task UpdateOffersForSellerAsync(SyncRecordInElasticSearch command, CancellationToken cancellationToken)
    {
        try
        {
            var seller = JsonSerializer.Deserialize<Seller>(command.Payload, _jsonOptions);
            if (seller == null)
            {
                _logger.LogError("Failed to deserialize seller payload: {Payload}", command.Payload);
                return;
            }

            var affectedDocuments = await _elasticsearchService.FindOfferDocumentsByEntityIdAsync("seller", seller.SellerId.ToString(), cancellationToken);
            
            var sellerInfo = new SellerInfo
            {
                SellerId = seller.SellerId,
                Name = seller.Name,
                Email = seller.Email,
                CreatedAt = seller.CreatedAt,
                LastModifiedAt = seller.LastModifiedAt
            };

            foreach (var doc in affectedDocuments)
            {
                doc.Seller = sellerInfo;
                doc.SellerName = seller.Name;
                await _elasticsearchService.UpdateOfferDocumentAsync(doc.Id, doc, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating offers for seller");
            throw;
        }
    }

    private async Task UpdateOffersForBuyerAsync(SyncRecordInElasticSearch command, CancellationToken cancellationToken)
    {
        try
        {
            var buyer = JsonSerializer.Deserialize<Buyer>(command.Payload, _jsonOptions);
            if (buyer == null)
            {
                _logger.LogError("Failed to deserialize buyer payload: {Payload}", command.Payload);
                return;
            }

            var affectedDocuments = await _elasticsearchService.FindOfferDocumentsByEntityIdAsync("buyer", buyer.Id.ToString(), cancellationToken);
            
            var buyerInfo = new BuyerInfo
            {
                BuyerId = buyer.Id,
                Name = buyer.Name,
                Email = buyer.Email,
                Phone = buyer.Phone,
                Company = buyer.Company,
                CreatedAt = buyer.CreatedAt,
                LastModifiedAt = buyer.LastModifiedAt
            };

            foreach (var doc in affectedDocuments)
            {
                // Update buyer info in all purchases
                foreach (var purchase in doc.Purchases.Where(p => p.BuyerId == buyer.Id))
                {
                    purchase.Buyer = buyerInfo;
                }
                await _elasticsearchService.UpdateOfferDocumentAsync(doc.Id, doc, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating offers for buyer");
            throw;
        }
    }

    private async Task UpdateOffersForCarrierAsync(SyncRecordInElasticSearch command, CancellationToken cancellationToken)
    {
        try
        {
            var carrier = JsonSerializer.Deserialize<Carrier>(command.Payload, _jsonOptions);
            if (carrier == null)
            {
                _logger.LogError("Failed to deserialize carrier payload: {Payload}", command.Payload);
                return;
            }

            var affectedDocuments = await _elasticsearchService.FindOfferDocumentsByEntityIdAsync("carrier", carrier.Id.ToString(), cancellationToken);
            
            var carrierInfo = new CarrierInfo
            {
                Id = carrier.Id,
                Name = carrier.Name,
                Email = carrier.Email,
                Phone = carrier.Phone,
                CreatedAt = carrier.CreatedAt,
                LastModifiedAt = carrier.LastModifiedAt
            };

            foreach (var doc in affectedDocuments)
            {
                // Update carrier info in all transports
                foreach (var transport in doc.Transports.Where(t => t.CarrierId == carrier.Id))
                {
                    transport.Carrier = carrierInfo;
                }
                await _elasticsearchService.UpdateOfferDocumentAsync(doc.Id, doc, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating offers for carrier");
            throw;
        }
    }

    private async Task RemoveEntityFromOfferDocument(ElasticSearchOfferDocument doc, string entityType, string entityId, CancellationToken cancellationToken)
    {
        bool updated = false;

        switch (entityType.ToLower())
        {
            case "seller":
                doc.Seller = null;
                updated = true;
                break;
            case "purchase":
                var purchaseToRemove = doc.Purchases.FirstOrDefault(p => p.Id.ToString() == entityId);
                if (purchaseToRemove != null)
                {
                    doc.Purchases.Remove(purchaseToRemove);
                    updated = true;
                }
                break;
            case "transport":
                var transportToRemove = doc.Transports.FirstOrDefault(t => t.Id.ToString() == entityId);
                if (transportToRemove != null)
                {
                    doc.Transports.Remove(transportToRemove);
                    updated = true;
                }
                break;
            case "buyer":
                foreach (var purchase in doc.Purchases.Where(p => p.BuyerId.ToString() == entityId))
                {
                    purchase.Buyer = null;
                    updated = true;
                }
                break;
            case "carrier":
                foreach (var transport in doc.Transports.Where(t => t.CarrierId.ToString() == entityId))
                {
                    transport.Carrier = null;
                    updated = true;
                }
                break;
        }

        if (updated)
        {
            await _elasticsearchService.UpdateOfferDocumentAsync(doc.Id, doc, cancellationToken);
        }
    }

    private async Task<ElasticSearchOfferDocument> BuildElasticSearchOfferDocumentAsync(Offer offer, CancellationToken cancellationToken)
    {
        var document = new ElasticSearchOfferDocument
        {
            OfferId = offer.OfferId,
            SellerId = offer.SellerId,
            SellerNetworkId = offer.SellerNetworkId,
            SellerName = offer.SellerName,
            Vin = offer.Vin,
            VehicleYear = offer.VehicleYear,
            VehicleMake = offer.VehicleMake,
            VehicleModel = offer.VehicleModel,
            VehicleTrim = offer.VehicleTrim,
            VehicleBodyType = offer.VehicleBodyType,
            VehicleCabType = offer.VehicleCabType,
            VehicleDoorCount = offer.VehicleDoorCount,
            VehicleFuelType = offer.VehicleFuelType,
            VehicleBodyStyle = offer.VehicleBodyStyle,
            VehicleUsage = offer.VehicleUsage,
            VehicleZipCode = offer.VehicleZipCode,
            OwnershipType = offer.OwnershipType,
            OwnershipTitleType = offer.OwnershipTitleType,
            Mileage = offer.Mileage,
            IsMileageUnverifiable = offer.IsMileageUnverifiable,
            DrivetrainCondition = offer.DrivetrainCondition,
            KeyOrFobAvailable = offer.KeyOrFobAvailable,
            WorkingBatteryInstalled = offer.WorkingBatteryInstalled,
            AllTiresInflated = offer.AllTiresInflated,
            WheelsRemoved = offer.WheelsRemoved,
            WheelsRemovedDriverFront = offer.WheelsRemovedDriverFront,
            WheelsRemovedDriverRear = offer.WheelsRemovedDriverRear,
            WheelsRemovedPassengerFront = offer.WheelsRemovedPassengerFront,
            WheelsRemovedPassengerRear = offer.WheelsRemovedPassengerRear,
            BodyPanelsIntact = offer.BodyPanelsIntact,
            BodyDamageFree = offer.BodyDamageFree,
            MirrorsLightsGlassIntact = offer.MirrorsLightsGlassIntact,
            InteriorIntact = offer.InteriorIntact,
            FloodFireDamageFree = offer.FloodFireDamageFree,
            EngineTransmissionCondition = offer.EngineTransmissionCondition,
            AirbagsDeployed = offer.AirbagsDeployed,
            Status = offer.Status,
            PurchaseId = offer.PurchaseId,
            TransportId = offer.TransportId,
            NoSQLIndexId = offer.NoSQLIndexId,
            CreatedAt = offer.CreatedAt,
            LastModifiedAt = offer.LastModifiedAt
        };

        // Fetch related entities
        document.Seller = await _entityDataService.GetSellerAsync(offer.SellerId, cancellationToken);
        
        var purchases = await _entityDataService.GetPurchasesByOfferIdAsync(offer.OfferId, cancellationToken);
        foreach (var purchase in purchases)
        {
            var purchaseInfo = await BuildPurchaseInfoAsync(purchase, cancellationToken);
            if (purchaseInfo != null)
            {
                document.Purchases.Add(purchaseInfo);
            }
        }

        var transports = await _entityDataService.GetTransportsByOfferIdAsync(offer.OfferId, cancellationToken);
        foreach (var transport in transports)
        {
            var transportInfo = await BuildTransportInfoAsync(transport, cancellationToken);
            if (transportInfo != null)
            {
                document.Transports.Add(transportInfo);
            }
        }

        // Build searchable text for full-text search
        document.SearchableText = new List<string>
        {
            $"{document.VehicleYear} {document.VehicleMake} {document.VehicleModel}",
            document.Vin,
            document.SellerName,
            document.VehicleBodyType,
            document.VehicleFuelType,
            document.Status
        };

        return document;
    }

    private async Task<PurchaseInfo?> BuildPurchaseInfoAsync(Purchase purchase, CancellationToken cancellationToken)
    {
        var purchaseInfo = new PurchaseInfo
        {
            Id = purchase.Id,
            BuyerId = purchase.BuyerId,
            OfferId = purchase.OfferId,
            PurchaseDate = purchase.PurchaseDate,
            Amount = purchase.Amount,
            BuyerInfo = purchase.BuyerInfo,
            Status = purchase.Status,
            CreatedAt = purchase.CreatedAt,
            LastModifiedAt = purchase.LastModifiedAt
        };

        purchaseInfo.Buyer = await _entityDataService.GetBuyerAsync(purchase.BuyerId, cancellationToken);
        
        return purchaseInfo;
    }

    private async Task<TransportInfo?> BuildTransportInfoAsync(Transport transport, CancellationToken cancellationToken)
    {
        var transportInfo = new TransportInfo
        {
            Id = transport.Id,
            CarrierId = transport.CarrierId,
            PurchaseId = transport.PurchaseId,
            PickupLocation = transport.PickupLocation,
            DeliveryLocation = transport.DeliveryLocation,
            ScheduleDate = transport.ScheduleDate,
            VehicleDetails = transport.VehicleDetails,
            Status = transport.Status,
            CreatedAt = transport.CreatedAt,
            LastModifiedAt = transport.LastModifiedAt
        };

        // Don't make API calls - use only the data we have from the transport payload
        // The carrier information will be updated separately when carrier sync messages are received
        transportInfo.Carrier = null;

        _logger.LogInformation("Built transport info for Transport ID: {TransportId}", transport.Id);
        return transportInfo;
    }

    private static string ExtractEntityId(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            // Try common ID field names
            if (root.TryGetProperty("id", out var id))
                return id.GetString() ?? "";
            if (root.TryGetProperty("Id", out var idCap))
                return idCap.GetString() ?? "";
            if (root.TryGetProperty("offerId", out var offerId))
                return offerId.GetString() ?? "";
            if (root.TryGetProperty("OfferId", out var offerIdCap))
                return offerIdCap.GetString() ?? "";
            if (root.TryGetProperty("sellerId", out var sellerId))
                return sellerId.GetString() ?? "";
            if (root.TryGetProperty("SellerId", out var sellerIdCap))
                return sellerIdCap.GetString() ?? "";

            return "";
        }
        catch
        {
            return "";
        }
    }
}