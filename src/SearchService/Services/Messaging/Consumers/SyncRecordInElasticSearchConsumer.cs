using MassTransit;
using SearchService.Interfaces;
using SearchService.Models;
using Microsoft.Extensions.Logging;

namespace SearchService.Services.Messaging.Consumers;

public class SyncRecordInElasticSearchConsumer : IConsumer<SyncRecordInElasticSearch>
{
    private readonly ISyncRecordProcessor _syncRecordProcessor;
    private readonly ILogger<SyncRecordInElasticSearchConsumer> _logger;

    public SyncRecordInElasticSearchConsumer(
        ISyncRecordProcessor syncRecordProcessor,
        ILogger<SyncRecordInElasticSearchConsumer> logger)
    {
        _syncRecordProcessor = syncRecordProcessor;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SyncRecordInElasticSearch> context)
    {
        var command = context.Message;
        
        _logger.LogInformation(
            "Received SyncRecordInElasticSearch command: Operation={Operation}, ObjectType={ObjectType}, Id={Id}",
            command.Operation,
            command.ObjectType,
            command.ElasticSearchId);

        try
        {
            await _syncRecordProcessor.ProcessSyncRecordAsync(command, context.CancellationToken);
            
            _logger.LogInformation(
                "Successfully processed SyncRecordInElasticSearch: Operation={Operation}, ObjectType={ObjectType}, Id={Id}",
                command.Operation,
                command.ObjectType,
                command.ElasticSearchId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Processing was cancelled for SyncRecordInElasticSearch: Operation={Operation}, ObjectType={ObjectType}, Id={Id}",
                command.Operation,
                command.ObjectType,
                command.ElasticSearchId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing SyncRecordInElasticSearch: Operation={Operation}, ObjectType={ObjectType}, Id={Id}",
                command.Operation,
                command.ObjectType,
                command.ElasticSearchId);
            throw; // Re-throw to let MassTransit handle retry logic
        }
    }
}