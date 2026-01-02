# Search Service API Testing Guide

This guide provides examples for testing the unified Search Service API that handles all 6 entity types through a master Elasticsearch Offer index.

## API Endpoints

### 1. Health Check
```http
GET /api/search/health
```

### 2. General Search
```http
POST /api/search
Content-Type: application/json

{
  "query": "Toyota Camry",
  "pageSize": 10,
  "pageNumber": 0,
  "includeAggregations": true,
  "sortField": "createdAt",
  "sortOrder": "desc",
  "userType": "buyer",
  "accountId": "buyer-123",
  "userId": "user-456"
}
```

### 3. Offer-Specific Search
```http
POST /api/search/offers
Content-Type: application/json

{
  "query": "2020 Honda",
  "status": "offered",
  "pageSize": 20,
  "pageNumber": 0,
  "includeAggregations": true,
  "userType": "seller",
  "accountId": "seller-789",
  "userId": "user-101"
}
```

### 4. Get Specific Offer
```http
GET /api/search/offers/{elasticSearchId}
```

### 5. Autocomplete
```http
GET /api/search/autocomplete?term=toyo&maxResults=10
```

### 6. Find Offers by Entity
```http
POST /api/search/entities/seller/123
POST /api/search/entities/buyer/456
POST /api/search/entities/transport/789
```

## Sample MassTransit Events

### Create Offer Event
```json
{
  "messageId": "c0760000-8c5f-f46b-c245-08de4a033d51",
  "requestId": null,
  "correlationId": null,
  "conversationId": "c0760000-8c5f-f46b-c808-08de4a033d51",
  "initiatorId": null,
  "sourceAddress": "rabbitmq://localhost/SellerService_bus/send",
  "destinationAddress": "rabbitmq://localhost/SearchService.Core.Commands:SyncRecordInElasticSearch",
  "responseAddress": null,
  "faultAddress": null,
  "messageType": [
    "urn:message:SearchService.Core.Commands:SyncRecordInElasticSearch"
  ],
  "message": {
    "elasticSearchId": "offer-es-001",
    "objectType": "Offer",
    "operation": "Create",
    "payload": "{\"OfferId\":1,\"SellerId\":1,\"SellerNetworkId\":\"SELLER-NET-001\",\"SellerName\":\"John Doe Motors\",\"Vin\":\"1HGBH41JXMN109186\",\"VehicleYear\":\"2020\",\"VehicleMake\":\"Honda\",\"VehicleModel\":\"Accord\",\"VehicleTrim\":\"Sport\",\"VehicleBodyType\":\"Sedan\",\"VehicleCabType\":\"Standard\",\"VehicleDoorCount\":4,\"VehicleFuelType\":\"Gasoline\",\"VehicleBodyStyle\":\"Sedan\",\"VehicleUsage\":\"Personal\",\"VehicleZipCode\":\"12345\",\"OwnershipType\":\"Clean\",\"OwnershipTitleType\":\"Clear\",\"Mileage\":35000,\"IsMileageUnverifiable\":false,\"DrivetrainCondition\":\"Excellent\",\"KeyOrFobAvailable\":\"Yes\",\"WorkingBatteryInstalled\":\"Yes\",\"AllTiresInflated\":\"Yes\",\"WheelsRemoved\":\"No\",\"WheelsRemovedDriverFront\":false,\"WheelsRemovedDriverRear\":false,\"WheelsRemovedPassengerFront\":false,\"WheelsRemovedPassengerRear\":false,\"BodyPanelsIntact\":\"Yes\",\"BodyDamageFree\":\"Yes\",\"MirrorsLightsGlassIntact\":\"Yes\",\"InteriorIntact\":\"Yes\",\"FloodFireDamageFree\":\"Yes\",\"EngineTransmissionCondition\":\"Excellent\",\"AirbagsDeployed\":\"No\",\"Status\":\"offered\",\"PurchaseId\":null,\"TransportId\":null,\"NoSQLIndexId\":\"550e8400-e29b-41d4-a716-446655440000\",\"CreatedAt\":\"2026-01-02T10:00:00Z\",\"LastModifiedAt\":\"2026-01-02T10:00:00Z\"}"
  }
}
```

### Create Purchase Event
```json
{
  "messageId": "c0760000-8c5f-f46b-c245-08de4a033d52",
  "message": {
    "elasticSearchId": "purchase-es-001",
    "objectType": "Purchase",
    "operation": "Create",
    "payload": "{\"Id\":1,\"BuyerId\":1,\"OfferId\":1,\"PurchaseDate\":\"2026-01-02T11:00:00Z\",\"Amount\":25000.00,\"BuyerInfo\":\"Verified buyer with excellent credit\",\"Status\":\"Confirmed\",\"CreatedAt\":\"2026-01-02T11:00:00Z\",\"LastModifiedAt\":\"2026-01-02T11:00:00Z\"}"
  }
}
```

### Create Transport Event (Your Example)
```json
{
  "messageId": "c0760000-8c5f-f46b-c245-08de4a033d51",
  "requestId": null,
  "correlationId": null,
  "conversationId": "c0760000-8c5f-f46b-c808-08de4a033d51",
  "initiatorId": null,
  "sourceAddress": "rabbitmq://localhost/DTWKWIZAK_TransportServiceAPI_bus_ab5yyyrcm94gshqtbdxrwywjrd?temporary=true",
  "destinationAddress": "rabbitmq://localhost/TransportService.Core.Commands:SyncRecordInElasticSearch",
  "responseAddress": null,
  "faultAddress": null,
  "messageType": [
    "urn:message:TransportService.Core.Commands:SyncRecordInElasticSearch"
  ],
  "message": {
    "elasticSearchId": "transport-es-001",
    "objectType": "Transport",
    "operation": "Create",
    "payload": "{\"Id\":2,\"CarrierId\":1,\"PurchaseId\":1,\"PickupLocation\":\"12345\",\"DeliveryLocation\":\"67890\",\"ScheduleDate\":\"2026-01-03T10:00:00Z\",\"VehicleDetails\":null,\"Status\":\"Assigned\",\"CreatedAt\":\"2026-01-02T13:31:30.67142Z\",\"LastModifiedAt\":\"2026-01-02T13:31:30.6714201Z\"}"
  }
}
```

### Update Seller Event
```json
{
  "message": {
    "elasticSearchId": "seller-es-001",
    "objectType": "Seller",
    "operation": "Update",
    "payload": "{\"SellerId\":1,\"Name\":\"John Doe Motors Updated\",\"Email\":\"john.updated@example.com\",\"PasswordHash\":\"$2a$11$XABw.U9VE2xMKS9JDZXgK.xRQ0VYG1kGRYz4YW7uELC/qOxs6B2Gy\",\"CreatedAt\":\"2026-01-01T10:00:00Z\",\"LastModifiedAt\":\"2026-01-02T14:00:00Z\"}"
  }
}
```

## Testing Scenarios

### Scenario 1: Complete Offer Lifecycle
1. Send Create Offer event → Creates new offer document in ES
2. Send Create Purchase event → Adds purchase info to existing offer document
3. Send Create Transport event → Adds transport info to existing offer document
4. Search for offers → Should return enriched document with all related data

### Scenario 2: Entity Updates
1. Send Update Seller event → Updates seller info in all related offer documents
2. Send Update Buyer event → Updates buyer info in related purchases within offer documents
3. Send Update Carrier event → Updates carrier info in related transports within offer documents

### Scenario 3: Complex Search Queries
1. Search by VIN: `"query": "1HGBH41JXMN109186"`
2. Search by make/model: `"query": "Honda Accord"`
3. Search by seller: `"query": "John Doe Motors"`
4. Search by status: `"status": "offered"`
5. Combined search: `"query": "Honda 2020", "status": "offered"`

### Scenario 4: Entity-Based Lookups
1. Find all offers by seller ID
2. Find all offers containing a specific purchase
3. Find all offers containing transports by a specific carrier

## Elasticsearch Index Structure

The unified index `offers_unified_index` contains documents structured as:

```json
{
  "id": "offer-es-001",
  "offerId": 1,
  "sellerId": 1,
  "vin": "1HGBH41JXMN109186",
  "vehicleMake": "Honda",
  "vehicleModel": "Accord",
  "vehicleYear": "2020",
  "status": "offered",
  "seller": {
    "sellerId": 1,
    "name": "John Doe Motors",
    "email": "john@example.com"
  },
  "purchases": [
    {
      "id": 1,
      "buyerId": 1,
      "amount": 25000.00,
      "status": "Confirmed",
      "buyer": {
        "id": 1,
        "name": "Jane Smith",
        "email": "jane@example.com"
      }
    }
  ],
  "transports": [
    {
      "id": 2,
      "carrierId": 1,
      "pickupLocation": "12345",
      "deliveryLocation": "67890",
      "status": "Assigned",
      "carrier": {
        "id": 1,
        "name": "Fast Transport LLC",
        "email": "dispatch@fasttransport.com"
      }
    }
  ],
  "searchableText": [
    "2020 Honda Accord",
    "1HGBH41JXMN109186",
    "John Doe Motors",
    "Sedan",
    "Gasoline",
    "offered"
  ]
}
```

## Error Handling

The API handles various error scenarios:
- Invalid entity types → 400 Bad Request
- Elasticsearch connection issues → 500 Internal Server Error
- Operation cancellation → 400 Bad Request
- Missing entities → 404 Not Found
- Processing failures → 500 Internal Server Error with retry via MassTransit

## Performance Considerations

1. **Index Optimization**: Uses 3 shards for better distribution
2. **Search Performance**: Multi-field search with proper boosting
3. **Autocomplete**: Edge n-gram tokenization for fast prefix matching
4. **Aggregations**: Cached facets for filters
5. **Concurrency**: Configurable concurrent message processing