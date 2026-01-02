using SearchService.Models;

namespace SearchService.Interfaces;

public interface ISyncRecordProcessor
{
    Task ProcessSyncRecordAsync(SyncRecordInElasticSearch command, CancellationToken cancellationToken = default);
}