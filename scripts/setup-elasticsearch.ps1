# Setup Elasticsearch for SearchService Development
# This script helps you run Elasticsearch locally without security for development

Write-Host "SearchService - Elasticsearch Setup Script" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

Write-Host "`nOption 1: Run Elasticsearch with Docker (Recommended for Development)" -ForegroundColor Yellow
Write-Host "Command: docker run -d --name elasticsearch -p 9200:9200 -e `"discovery.type=single-node`" -e `"xpack.security.enabled=false`" elasticsearch:7.17.22"

Write-Host "`nOption 2: Run Elasticsearch with Docker Compose" -ForegroundColor Yellow
Write-Host "Use the docker-compose.yml file in the docker/ folder"

Write-Host "`nOption 3: Configure existing Elasticsearch with security" -ForegroundColor Yellow
Write-Host "If you have Elasticsearch with X-Pack security enabled, update these settings:"
Write-Host "- appsettings.json -> Elasticsearch:Username = 'elastic'"
Write-Host "- appsettings.json -> Elasticsearch:Password = 'your-password'"

Write-Host "`nTesting Connection:" -ForegroundColor Cyan
Write-Host "You can test your Elasticsearch connection by running:"
Write-Host "curl http://localhost:9200"

Write-Host "`nFor development (no authentication required):" -ForegroundColor Cyan
Write-Host "curl http://localhost:9200/_cluster/health"

Write-Host "`nConfiguration Files Updated:" -ForegroundColor Green
Write-Host "- appsettings.json: Default credentials (elastic/changeme)"
Write-Host "- appsettings.Development.json: No authentication for development"

Write-Host "`nNext Steps:" -ForegroundColor White
Write-Host "1. Start Elasticsearch using one of the options above"
Write-Host "2. Run your SearchService: dotnet run --project src/SearchService/SearchService.csproj"
Write-Host "3. Check logs to verify Elasticsearch connection"