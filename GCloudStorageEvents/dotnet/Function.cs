using System;
using System.Threading;
using System.Threading.Tasks;
using CDRPlatform.GCP.Integration;
using CloudNative.CloudEvents;
using Google.Cloud.Functions.Framework;
using Google.Events.Protobuf.Cloud.Storage.V1;
using Microsoft.Extensions.Logging;

public class Function : ICloudEventFunction<StorageObjectData>
{
    private readonly ILogger _logger;
    private readonly RebuildProxy _rebuildProxy;
    private readonly GoogleCloudStorageProxy _cloudStorageProxy;
    private readonly string _outputBucket;

    public Function(ILogger<Function> logger)
    {
        _logger = logger;
        _rebuildProxy = new RebuildProxy(logger);
        _cloudStorageProxy = new GoogleCloudStorageProxy(logger);

        _outputBucket = Environment.GetEnvironmentVariable("OutputBucket") ?? throw new ArgumentException("Specified environment variable is not set.");
    }

    /// <summary>
    /// This method is called for every Google Cloud Run invocation. Each file creation in the source bucket will generate an invocation.
    /// </summary>
    /// <param name="cloudEvent">Googles Common metadata specification extracted from the request</param>
    /// <param name="data">Service specific metadata, StorageObjectData is specific to Google Cloud Storage</param>
    public async Task HandleAsync(CloudEvent cloudEvent, StorageObjectData data, CancellationToken cancellationToken)
    {
        using var _ = _logger.BeginScope(
            "Id: {id} Type {type} Bucket {bucket} File {file} Metageneration {metageneration}",
            cloudEvent.Id,
            cloudEvent.Type,
            data.Bucket,
            data.Name,
            data.Metageneration);

        _logger.LogInformation(
            "C# Google Storage Event Trigger start for file created {created:s} and updated {updated:s} ", 
            data.TimeCreated?.ToDateTimeOffset(),
            data.Updated?.ToDateTimeOffset());

        try
        {
            await ProtectBucketFileAsync(cloudEvent, data, cancellationToken);
        }
        finally
        {
            _logger.LogInformation(
                "C# Google Storage Event Trigger finished for file created {created:s} and updated {updated:s} ",
                data.TimeCreated?.ToDateTimeOffset(),
                data.Updated?.ToDateTimeOffset());
        }
    }

    private async Task ProtectBucketFileAsync(CloudEvent cloudEvent, StorageObjectData data, CancellationToken cancellationToken)
    {
        if (!IsSupportedEventType(cloudEvent.Type))
        {
            _logger.LogInformation("Unsupported Event Type: {type}", cloudEvent.Type);
            return;
        }

        using var originalFileStream = await _cloudStorageProxy.DownloadFileAsync(data.Bucket, data.Name, cancellationToken);
        using var rebuiltFileStream = await _rebuildProxy.ProtectFileAsync(originalFileStream, data.Name, cancellationToken);
        await _cloudStorageProxy.UploadFileAsync(_outputBucket, data.Name, rebuiltFileStream, cancellationToken);
    }

    public static bool IsSupportedEventType(string? cloudEventType)
    {
        return cloudEventType == "google.cloud.storage.object.v1.finalized";
    }
}
