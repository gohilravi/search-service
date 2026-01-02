using Nest;
using Elasticsearch.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SearchService.Services.Elasticsearch;

public static class ElasticsearchClientFactory
{
    public static IElasticClient CreateClient(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("ElasticsearchClientFactory");
        var elasticsearchUri = configuration["Elasticsearch:Uri"] ?? "http://localhost:9200";
        var username = configuration["Elasticsearch:Username"];
        var password = configuration["Elasticsearch:Password"];

        logger.LogInformation("Creating Elasticsearch client for {Uri}", elasticsearchUri);

        var connectionSettings = new ConnectionSettings(new Uri(elasticsearchUri))
            .DefaultIndex(configuration["Elasticsearch:IndexName"] ?? "offers_unified_index")
            .RequestTimeout(TimeSpan.Parse(configuration["Elasticsearch:RequestTimeout"] ?? "00:02:00"))
            .DisableDirectStreaming() // Enable request/response logging for debugging
            .SkipDeserializationForStatusCodes(400, 401, 403, 404, 500) // Skip deserialization for error codes
            .ThrowExceptions(false) // Don't throw exceptions for failed requests
            .OnRequestCompleted(details =>
            {
                if (details.Success)
                {
                    logger.LogDebug("Elasticsearch request completed successfully: {Method} {Uri}", 
                        details.HttpMethod, details.Uri);
                }
                else
                {
                    logger.LogWarning("Elasticsearch request failed: {Method} {Uri} - Status: {StatusCode} - {Error}", 
                        details.HttpMethod, details.Uri, details.HttpStatusCode, details.OriginalException?.Message);
                }
            });

        // Add authentication if credentials are provided
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("Configuring Elasticsearch with basic authentication for user: {Username}", username);
            connectionSettings.BasicAuthentication(username, password);
        }
        else
        {
            logger.LogInformation("No Elasticsearch credentials provided, connecting without authentication");
        }

        return new ElasticClient(connectionSettings);
    }
}