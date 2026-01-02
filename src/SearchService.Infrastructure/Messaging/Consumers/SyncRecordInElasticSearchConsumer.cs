using MassTransit;
using SearchService.Core.Interfaces;
using SearchService.Core.Models;
using Microsoft.Extensions.Logging;

namespace SearchService.Infrastructure.Messaging.Consumers;

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
            await _syncRecordProcessor.ProcessSyncRecordAsync(command);
            _logger.LogInformation(
                "Successfully processed SyncRecordInElasticSearch: Operation={Operation}, ObjectType={ObjectType}, Id={Id}",
                command.Operation,
                command.ObjectType,
                command.ElasticSearchId);
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

