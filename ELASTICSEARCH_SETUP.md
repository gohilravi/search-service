# Elasticsearch Authentication Issue - Resolution Guide

## Problem
Your SearchService is failing to connect to Elasticsearch due to missing authentication credentials:
```
security_exception: "missing authentication credentials for REST request [/offers_unified_index?pretty=true]"
```

## Solutions

### Solution 1: Run Elasticsearch without Security (Recommended for Development)

**Option A: Using Docker Command**
```bash
docker run -d --name elasticsearch \
  -p 9200:9200 \
  -e "discovery.type=single-node" \
  -e "xpack.security.enabled=false" \
  elasticsearch:7.17.22
```

**Option B: Using Docker Compose**
```bash
cd docker
docker-compose up -d elasticsearch
```

**Option C: Configure existing Elasticsearch installation**
Edit your `elasticsearch.yml` file and add:
```yaml
xpack.security.enabled: false
```
Then restart Elasticsearch.

### Solution 2: Use Elasticsearch with Authentication

If you need to keep security enabled, update your configuration:

**Step 1: Set up Elasticsearch password**
```bash
# For Docker Elasticsearch with security
docker exec -it your-elasticsearch-container elasticsearch-setup-passwords auto
```

**Step 2: Update appsettings.json**
```json
{
  "Elasticsearch": {
    "Username": "elastic",
    "Password": "your-generated-password"
  }
}
```

## Verification Steps

1. **Test Elasticsearch Connection**
   ```bash
   curl http://localhost:9200
   ```

2. **Check Cluster Health**
   ```bash
   curl http://localhost:9200/_cluster/health
   ```

3. **Test SearchService Health**
   ```bash
   curl http://localhost:5000/api/search/health
   ```

4. **Run the Application**
   ```bash
   dotnet run --project src/SearchService/SearchService.csproj
   ```

## Configuration Files Updated

✅ **appsettings.json** - Default credentials for production  
✅ **appsettings.Development.json** - No authentication for development  
✅ **ElasticsearchClientFactory.cs** - Enhanced logging and error handling  
✅ **docker-compose.yml** - Already configured without security  

## Quick Start Commands

```bash
# 1. Start Elasticsearch (Development)
docker run -d --name elasticsearch -p 9200:9200 -e "discovery.type=single-node" -e "xpack.security.enabled=false" elasticsearch:7.17.22

# 2. Wait for Elasticsearch to start (30-60 seconds)
curl http://localhost:9200

# 3. Run SearchService
dotnet run --project src/SearchService/SearchService.csproj

# 4. Test the health endpoint
curl http://localhost:5000/api/search/health
```

## Expected Health Response

**Healthy:**
```json
{
  "status": "healthy",
  "timestamp": "2026-01-02T10:30:00.000Z",
  "offerIndexExists": true
}
```

**If index doesn't exist yet:**
```json
{
  "status": "healthy",
  "timestamp": "2026-01-02T10:30:00.000Z",
  "offerIndexExists": false
}
```

The service will automatically create the index on first use.