using Nest;
using SearchService.Application.EventHandlers;
using SearchService.Application.Search;
using SearchService.Application.Security;
using SearchService.Core.Interfaces;
using SearchService.Infrastructure.Elasticsearch;
using SearchService.Infrastructure.Messaging;

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

// RabbitMQ Consumer
builder.Services.AddScoped<IEventProcessor, EventProcessor>();
builder.Services.AddHostedService<RabbitMQConsumer>();

// Event Handlers
builder.Services.AddScoped<IEventHandler, OfferEventHandler>();
builder.Services.AddScoped<IEventHandler, PurchaseEventHandler>();
builder.Services.AddScoped<IEventHandler, TransportEventHandler>();

// Application Services
builder.Services.AddScoped<ISearchService, SearchService>();
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
