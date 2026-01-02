# RabbitMQ Cleanup Script
# This script helps you clean up RabbitMQ to resolve exchange conflicts

Write-Host "SearchService - RabbitMQ Cleanup" -ForegroundColor Green
Write-Host "==================================" -ForegroundColor Green

Write-Host "`nStep 1: Stop SearchService if running" -ForegroundColor Yellow
Write-Host "Press Ctrl+C in the SearchService terminal if it's running"

Write-Host "`nStep 2: Clean up RabbitMQ data" -ForegroundColor Yellow
Write-Host "Choose one option:"

Write-Host "`nOption A: Delete specific exchange (Recommended)" -ForegroundColor Cyan
Write-Host "1. Open RabbitMQ Management UI: http://localhost:15672"
Write-Host "2. Login with: guest/guest"
Write-Host "3. Go to 'Exchanges' tab"
Write-Host "4. Find 'SyncRecordsInElasticSearch' exchange and delete it"
Write-Host "5. Go to 'Queues' tab"
Write-Host "6. Find 'search.sync-record-queue' queue and delete it"

Write-Host "`nOption B: Reset all RabbitMQ data (Nuclear option)" -ForegroundColor Red
Write-Host "docker-compose down rabbitmq"
Write-Host "docker volume rm search-service_rabbitmq-data"
Write-Host "docker-compose up -d rabbitmq"

Write-Host "`nOption C: Using RabbitMQ CLI commands" -ForegroundColor Cyan
Write-Host "# Delete exchange"
Write-Host "curl -u guest:guest -X DELETE http://localhost:15672/api/exchanges/%2F/SyncRecordsInElasticSearch"
Write-Host ""
Write-Host "# Delete queue"
Write-Host "curl -u guest:guest -X DELETE http://localhost:15672/api/queues/%2F/search.sync-record-queue"

Write-Host "`nStep 3: Restart SearchService" -ForegroundColor Green
Write-Host "dotnet run --project src/SearchService/SearchService.csproj"

Write-Host "`nWhy this happened:" -ForegroundColor White
Write-Host "- Exchange already existed with different settings"
Write-Host "- MassTransit couldn't match the existing exchange configuration"
Write-Host "- The simplified configuration should now work correctly"

Write-Host "`nExpected result after cleanup:" -ForegroundColor Green
Write-Host "- MassTransit will create exchanges using default conventions"
Write-Host "- No more topology conflicts"
Write-Host "- Exchange will be created automatically with proper settings"