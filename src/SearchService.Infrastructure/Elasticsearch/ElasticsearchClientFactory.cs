using Nest;
using Elasticsearch.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SearchService.Infrastructure.Elasticsearch;

public static class ElasticsearchClientFactory
{
    public static IElasticClient CreateClient(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        var connectionSettings = new ConnectionSettings(new Uri(configuration["Elasticsearch:Uri"] ?? "http://localhost:9200"))
            .DefaultIndex("search_index_v1")
            .EnableApiVersioningHeader()
            .PrettyJson()
            .RequestTimeout(TimeSpan.FromMinutes(2));

        if (!string.IsNullOrEmpty(configuration["Elasticsearch:Username"]))
        {
            connectionSettings.BasicAuthentication(
                configuration["Elasticsearch:Username"],
                configuration["Elasticsearch:Password"]);
        }

        return new ElasticClient(connectionSettings);
    }
}

