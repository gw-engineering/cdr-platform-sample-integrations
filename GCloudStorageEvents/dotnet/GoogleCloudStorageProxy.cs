using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Logging;

namespace CDRPlatform.GCP.Integration;

public class GoogleCloudStorageProxy
{
    private readonly StorageClient _storageClient;
    private readonly ILogger<Function> _logger;

    public GoogleCloudStorageProxy(ILogger<Function> logger)
    {
        _storageClient = StorageClient.Create();
        _logger = logger;
    }

    public async Task<Stream> DownloadFileAsync(string bucketName, string objectName, CancellationToken cancellationToken)
    {
        var ms = new MemoryStream();
        await _storageClient.DownloadObjectAsync(bucketName, objectName, ms, cancellationToken: cancellationToken);
        _logger.LogInformation($"Downloaded {objectName} from bucket {bucketName}.");
        return ms;
    }

    public async Task UploadFileAsync(string bucketName, string objectName, Stream fileStream, CancellationToken cancellationToken)
    {
        await _storageClient.UploadObjectAsync(bucketName, objectName, null, fileStream, cancellationToken: cancellationToken);
        _logger.LogInformation($"Uploaded {objectName} to bucket {bucketName}.");
    }
}