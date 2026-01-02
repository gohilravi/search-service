# RabbitMQ Exchange Verification Script
# This script helps you verify that the RabbitMQ exchange is created correctly

Write-Host "SearchService - RabbitMQ Exchange Verification" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green

Write-Host "`nChecking RabbitMQ Management UI..." -ForegroundColor Yellow
Write-Host "1. Open browser to: http://localhost:15672"
Write-Host "2. Login with: guest / guest"
Write-Host "3. Go to 'Exchanges' tab"
Write-Host "4. Look for exchange: SyncRecordsInElasticSearch"

Write-Host "`nConfiguration Details:" -ForegroundColor Cyan
Write-Host "- Exchange Name: SyncRecordsInElasticSearch"
Write-Host "- Exchange Type: topic"
Write-Host "- Durable: true"
Write-Host "- Queue Name: search.sync-record-queue"

Write-Host "`nTesting Commands:" -ForegroundColor White
Write-Host "# Check if RabbitMQ is running"
Write-Host "curl http://localhost:15672/api/overview"
Write-Host ""
Write-Host "# List all exchanges"
Write-Host "curl -u guest:guest http://localhost:15672/api/exchanges"
Write-Host ""
Write-Host "# List all queues"
Write-Host "curl -u guest:guest http://localhost:15672/api/queues"

Write-Host "`nStartup Order:" -ForegroundColor Green
Write-Host "1. Start RabbitMQ: docker-compose up -d rabbitmq"
Write-Host "2. Wait 30 seconds for RabbitMQ to be ready"
Write-Host "3. Start SearchService: dotnet run --project src/SearchService/SearchService.csproj"
Write-Host "4. Check logs for: 'Bus started: rabbitmq://localhost/'"

Write-Host "`nTroubleshooting:" -ForegroundColor Red
Write-Host "If exchange is not created:"
Write-Host "- Check RabbitMQ logs: docker logs search-rabbitmq"
Write-Host "- Check SearchService logs for MassTransit errors"
Write-Host "- Verify RabbitMQ credentials in appsettings.json"
Write-Host "- Ensure RabbitMQ service is healthy before starting SearchService"