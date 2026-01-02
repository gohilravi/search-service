# PowerShell script to generate realistic test data for the search service
# Generates Offers, Purchases, and Transports with realistic VINs, makes, models, and locations

param(
    [int]$OfferCount = 1000000,
    [int]$PurchaseCount = 500000,
    [int]$TransportCount = 300000,
    [string]$RabbitMQHost = "localhost",
    [int]$RabbitMQPort = 5672,
    [string]$ExchangeName = "entity_events"
)

$ErrorActionPreference = "Stop"

# Vehicle makes and models
$Makes = @("Toyota", "Honda", "Ford", "Chevrolet", "Nissan", "BMW", "Mercedes-Benz", "Audi", "Volkswagen", "Hyundai", "Kia", "Mazda", "Subaru", "Jeep", "Ram")
$Models = @{
    "Toyota" = @("Camry", "Corolla", "RAV4", "Highlander", "Prius", "Tacoma", "Tundra", "4Runner")
    "Honda" = @("Civic", "Accord", "CR-V", "Pilot", "Odyssey", "Ridgeline", "HR-V")
    "Ford" = @("F-150", "Escape", "Explorer", "Mustang", "Edge", "Fusion", "Ranger")
    "Chevrolet" = @("Silverado", "Equinox", "Tahoe", "Malibu", "Traverse", "Colorado", "Camaro")
    "Nissan" = @("Altima", "Rogue", "Sentra", "Pathfinder", "Frontier", "Titan", "Murano")
}

$Locations = @("New York, NY", "Los Angeles, CA", "Chicago, IL", "Houston, TX", "Phoenix, AZ", "Philadelphia, PA", "San Antonio, TX", "San Diego, CA", "Dallas, TX", "San Jose, CA", "Austin, TX", "Jacksonville, FL", "Fort Worth, TX", "Columbus, OH", "Charlotte, NC")

$Statuses = @("Active", "Pending", "Completed", "Cancelled")

function Generate-VIN {
    $chars = "ABCDEFGHJKLMNPRSTUVWXYZ0123456789"
    $vin = ""
    for ($i = 0; $i -lt 17; $i++) {
        $vin += $chars[(Get-Random -Maximum $chars.Length)]
    }
    return $vin
}

function Generate-EventMessage {
    param(
        [string]$EventType,
        [string]$EntityType,
        [object]$Payload
    )
    
    return @{
        eventType = $EventType
        entityType = $EntityType
        payload = $Payload
        timestamp = (Get-Date -Format "yyyy-MM-ddTHH:mm:ss.fffZ")
    } | ConvertTo-Json -Depth 10
}

function Send-ToRabbitMQ {
    param(
        [string]$Message,
        [string]$RoutingKey
    )
    
    # Note: This is a simplified version. In production, you would use RabbitMQ.Client
    # For now, we'll output to a file that can be consumed
    $Message | Out-File -FilePath "test-events.jsonl" -Append -Encoding UTF8
}

Write-Host "Starting test data generation..." -ForegroundColor Green
Write-Host "Offers: $OfferCount, Purchases: $PurchaseCount, Transports: $TransportCount" -ForegroundColor Yellow

# Generate Offers
Write-Host "Generating Offers..." -ForegroundColor Cyan
$offerIds = @()
for ($i = 1; $i -le $OfferCount; $i++) {
    $make = $Makes | Get-Random
    $model = $Models[$make] | Get-Random
    $year = Get-Random -Minimum 2015 -Maximum 2025
    $vin = Generate-VIN
    $location = $Locations | Get-Random
    $status = $Statuses | Get-Random
    $sellerId = "seller-" + (Get-Random -Minimum 1 -Maximum 1000)
    $price = Get-Random -Minimum 10000 -Maximum 100000
    
    $offerId = "offer-$i"
    $offerIds += $offerId
    
    $offer = @{
        id = $offerId
        vin = $vin
        make = $make
        model = $model
        year = $year
        location = $location
        status = $status
        sellerId = $sellerId
        price = $price
        createdAt = (Get-Date).AddDays(-(Get-Random -Maximum 365)).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        updatedAt = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
    }
    
    $event = Generate-EventMessage -EventType "OfferCreated" -EntityType "Offer" -Payload $offer
    Send-ToRabbitMQ -Message $event -RoutingKey "offer.created"
    
    if ($i % 10000 -eq 0) {
        Write-Host "Generated $i offers..." -ForegroundColor Gray
    }
}

# Generate Purchases
Write-Host "Generating Purchases..." -ForegroundColor Cyan
$purchaseIds = @()
for ($i = 1; $i -le $PurchaseCount; $i++) {
    $offerId = $offerIds | Get-Random
    $buyerId = "buyer-" + (Get-Random -Minimum 1 -Maximum 500)
    $sellerId = "seller-" + (Get-Random -Minimum 1 -Maximum 1000)
    $vin = Generate-VIN
    $location = $Locations | Get-Random
    $status = $Statuses | Get-Random
    $amount = Get-Random -Minimum 10000 -Maximum 100000
    
    $purchaseId = "purchase-$i"
    $purchaseIds += $purchaseId
    
    $purchase = @{
        id = $purchaseId
        offerId = $offerId
        vin = $vin
        buyerId = $buyerId
        sellerId = $sellerId
        status = $status
        location = $location
        amount = $amount
        createdAt = (Get-Date).AddDays(-(Get-Random -Maximum 365)).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        updatedAt = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
    }
    
    $event = Generate-EventMessage -EventType "PurchaseCreated" -EntityType "Purchase" -Payload $purchase
    Send-ToRabbitMQ -Message $event -RoutingKey "purchase.created"
    
    if ($i % 10000 -eq 0) {
        Write-Host "Generated $i purchases..." -ForegroundColor Gray
    }
}

# Generate Transports
Write-Host "Generating Transports..." -ForegroundColor Cyan
for ($i = 1; $i -le $TransportCount; $i++) {
    $purchaseId = $purchaseIds | Get-Random
    $carrierId = "carrier-" + (Get-Random -Minimum 1 -Maximum 200)
    $buyerId = "buyer-" + (Get-Random -Minimum 1 -Maximum 500)
    $sellerId = "seller-" + (Get-Random -Minimum 1 -Maximum 1000)
    $vin = Generate-VIN
    $originLocation = $Locations | Get-Random
    $destinationLocation = $Locations | Get-Random
    $status = $Statuses | Get-Random
    
    $transport = @{
        id = "transport-$i"
        purchaseId = $purchaseId
        vin = $vin
        carrierId = $carrierId
        buyerId = $buyerId
        sellerId = $sellerId
        status = $status
        originLocation = $originLocation
        destinationLocation = $destinationLocation
        pickupDate = (Get-Date).AddDays(Get-Random -Minimum -30 -Maximum 30).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        deliveryDate = $null
        createdAt = (Get-Date).AddDays(-(Get-Random -Maximum 365)).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        updatedAt = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
    }
    
    if ($status -eq "Completed") {
        $transport.deliveryDate = (Get-Date).AddDays(Get-Random -Minimum -30 -Maximum 0).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
    }
    
    $event = Generate-EventMessage -EventType "TransportCreated" -EntityType "Transport" -Payload $transport
    Send-ToRabbitMQ -Message $event -RoutingKey "transport.created"
    
    if ($i % 10000 -eq 0) {
        Write-Host "Generated $i transports..." -ForegroundColor Gray
    }
}

Write-Host "Test data generation completed!" -ForegroundColor Green
Write-Host "Events written to: test-events.jsonl" -ForegroundColor Yellow
Write-Host "Total events: $($OfferCount + $PurchaseCount + $TransportCount)" -ForegroundColor Yellow

