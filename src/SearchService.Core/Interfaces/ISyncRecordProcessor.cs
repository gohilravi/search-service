using SearchService.Core.Models;

namespace SearchService.Core.Interfaces;

public interface ISyncRecordProcessor
{
    Task ProcessSyncRecordAsync(SyncRecordInElasticSearch command);
}

