using Nest;
using MassTransit;
using SearchService.Application.Search;
using SearchService.Application.Security;
using SearchService.Core.Interfaces;
using SearchService.Infrastructure.Elasticsearch;
using SearchService.Infrastructure.Messaging;
using SearchService.Infrastructure.Messaging.Consumers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

        // Configure endpoint for SyncRecordInElasticSearch command
        cfg.ReceiveEndpoint("search.sync-record-queue", e =>
        {
            e.ConfigureConsumer<SyncRecordInElasticSearchConsumer>(context);
            e.PrefetchCount = 10;
        });

        // Configure retry policy
        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
        
        // Configure error handling
        cfg.UseInMemoryOutbox();
    });
});

// Application Services
builder.Services.AddScoped<ISearchService, SearchService.Application.Search.SearchService>();
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
