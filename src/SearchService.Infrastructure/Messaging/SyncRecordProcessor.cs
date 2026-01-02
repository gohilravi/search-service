using System.Text.Json;
using SearchService.Core.Interfaces;
using SearchService.Core.Models;
using Microsoft.Extensions.Logging;

namespace SearchService.Infrastructure.Messaging;

public class SyncRecordProcessor : ISyncRecordProcessor
{
    private readonly IElasticsearchService _elasticsearchService;
    private readonly IDocumentMapper _documentMapper;
    private readonly ILogger<SyncRecordProcessor> _logger;
    private const string IndexName = "search_index_v1";

    public SyncRecordProcessor(
        IElasticsearchService elasticsearchService,
        IDocumentMapper documentMapper,
        ILogger<SyncRecordProcessor> logger)
    {
        _elasticsearchService = elasticsearchService;
        _documentMapper = documentMapper;
        _logger = logger;
    }

    public async Task ProcessSyncRecordAsync(SyncRecordInElasticSearch command)
    {
        try
        {
            var objectType = command.ObjectType.ToLowerInvariant();
            var operation = command.Operation.ToLowerInvariant();

            if (operation != "create" && operation != "update")
            {
                _logger.LogWarning("Unknown operation: {Operation} for ObjectType: {ObjectType}", 
                    command.Operation, command.ObjectType);
                return;
            }

            Dictionary<string, object> document;

            switch (objectType)
            {
                case "offer":
                    document = ProcessOffer(command);
                    break;
                case "purchase":
                    document = ProcessPurchase(command);
                    break;
                case "transport":
                    document = ProcessTransport(command);
                    break;
                default:
                    _logger.LogWarning("Unknown ObjectType: {ObjectType}", command.ObjectType);
                    return;
            }

            if (document == null)
            {
                _logger.LogWarning("Failed to create document for ObjectType: {ObjectType}", command.ObjectType);
                return;
            }

            // Ensure the document has the correct ID
            document["id"] = command.ElasticSearchId;

            // Index the document (Create and Update both use IndexDocumentAsync which upserts)
            await _elasticsearchService.IndexDocumentAsync(document, IndexName);
            _logger.LogInformation(
                "Successfully {Operation} {ObjectType} with ID: {Id}",
                command.Operation,
                command.ObjectType,
                command.ElasticSearchId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing sync record for ObjectType: {ObjectType}, Operation: {Operation}",
                command.ObjectType, command.Operation);
            throw;
        }
    }

    private Dictionary<string, object>? ProcessOffer(SyncRecordInElasticSearch command)
    {
        try
        {
            var offerJson = JsonSerializer.Serialize(command.Payload);
            var offer = JsonSerializer.Deserialize<Offer>(
                offerJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (offer == null)
            {
                _logger.LogWarning("Failed to deserialize Offer from payload");
                return null;
            }

            // Ensure the ID matches
            offer.Id = command.ElasticSearchId;

            return _documentMapper.MapOfferToDocument(offer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Offer");
            throw;
        }
    }

    private Dictionary<string, object>? ProcessPurchase(SyncRecordInElasticSearch command)
    {
        try
        {
            var purchaseJson = JsonSerializer.Serialize(command.Payload);
            var purchase = JsonSerializer.Deserialize<Purchase>(
                purchaseJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (purchase == null)
            {
                _logger.LogWarning("Failed to deserialize Purchase from payload");
                return null;
            }

            // Ensure the ID matches
            purchase.Id = command.ElasticSearchId;

            return _documentMapper.MapPurchaseToDocument(purchase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Purchase");
            throw;
        }
    }

    private Dictionary<string, object>? ProcessTransport(SyncRecordInElasticSearch command)
    {
        try
        {
            var transportJson = JsonSerializer.Serialize(command.Payload);
            var transport = JsonSerializer.Deserialize<Transport>(
                transportJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (transport == null)
            {
                _logger.LogWarning("Failed to deserialize Transport from payload");
                return null;
            }

            // Ensure the ID matches
            transport.Id = command.ElasticSearchId;

            return _documentMapper.MapTransportToDocument(transport);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Transport");
            throw;
        }
    }
}

