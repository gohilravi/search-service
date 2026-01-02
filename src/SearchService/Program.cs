using Nest;
using MassTransit;
using SearchService.Interfaces;
using SearchService.Services.Search;
using SearchService.Services.Security;
using SearchService.Services.Elasticsearch;
using SearchService.Services.Messaging.Consumers;
using SearchService.Services.Messaging;
using SearchService.Services;
using SearchService.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// HttpClient for external service calls
builder.Services.AddHttpClient<IEntityDataService, EntityDataService>();

// Elasticsearch
builder.Services.AddSingleton<IElasticClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    return ElasticsearchClientFactory.CreateClient(configuration, loggerFactory);
});

builder.Services.AddScoped<IIndexManager, IndexManager>();
builder.Services.AddScoped<IDocumentMapper, DocumentMapper>();
builder.Services.AddScoped<IElasticsearchService, ElasticsearchService>();

// Entity Data Service for fetching related data
builder.Services.AddScoped<IEntityDataService, EntityDataService>();

// Sync Record Processor
builder.Services.AddScoped<ISyncRecordProcessor, SyncRecordProcessor>();

// MassTransit Configuration
builder.Services.AddMassTransit(x =>
{
    // Add consumer for SyncRecordInElasticSearch command
    x.AddConsumer<SyncRecordInElasticSearchConsumer>();

    // Configure RabbitMQ
    x.UsingRabbitMq((context, cfg) =>
    {
        var configuration = context.GetRequiredService<IConfiguration>();
        var hostName = configuration["RabbitMQ:HostName"] ?? "localhost";
        var port = ushort.Parse(configuration["RabbitMQ:Port"] ?? "5672");
        var userName = configuration["RabbitMQ:UserName"] ?? "guest";
        var password = configuration["RabbitMQ:Password"] ?? "guest";

        cfg.Host(hostName, port, "/", h =>
        {
            h.Username(userName);
            h.Password(password);
        });

        // Configure message topology
        cfg.Message<SyncRecordInElasticSearch>(topology =>
        {
            // Set the entity name to match the destination address
            topology.SetEntityName("SearchService.Models:SyncRecordInElasticSearch");
        });

        // Configure endpoint for SyncRecordInElasticSearch command
        cfg.ReceiveEndpoint("SearchService.Models:SyncRecordInElasticSearch", e =>
        {
            e.ConfigureConsumer<SyncRecordInElasticSearchConsumer>(context);
            e.PrefetchCount = 10;
            e.ConcurrentMessageLimit = 5;
            
            // Log when messages are received
            e.UseMessageRetry(r => r.Immediate(2));
            e.UseInMemoryOutbox();
        });

        // Configure retry policy
        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
        
        // Configure error handling
        cfg.UseInMemoryOutbox();
    });
});

// Application Services
builder.Services.AddScoped<ISearchService, SearchService.Services.Search.SearchService>();
builder.Services.AddScoped<IAutocompleteService, AutocompleteService>();
builder.Services.AddScoped<IAccessControlService, AccessControlService>();
builder.Services.AddScoped<IQueryFilterBuilder, QueryFilterBuilder>();
builder.Services.AddScoped<ISynonymService, SynonymService>();
builder.Services.AddScoped<IEntityDetectionService, EntityDetectionService>();
builder.Services.AddScoped<ITypoToleranceService, TypoToleranceService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();