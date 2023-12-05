using System;
using Azure.Storage.Blobs;
using CdrAzureFunctions.Interfaces;
using System.IO;
using System.Threading.Tasks;

namespace CdrAzureFunctions.Services;

public class FileBlobWriter : IWriteFile
{
    private readonly IBlobClientFactory _blobClientFactory;

    public FileBlobWriter(IBlobClientFactory blobClientFactory)
    {
        _blobClientFactory = blobClientFactory ?? throw new ArgumentNullException(nameof(blobClientFactory));
    }
    public async Task WriteProtectedFile(string blobContainerName, string blobName, Stream protectedFile)
    {
        var blobServiceClient =
            new BlobServiceClient(Environment.GetEnvironmentVariable("AZURE_STORAGE_DESTINATION_CONNECTIONSTRING"));

        var destinationContainer = blobServiceClient.GetBlobContainerClient(blobContainerName);
        await destinationContainer.CreateIfNotExistsAsync();

        var destBlobClient =
            _blobClientFactory.CreateConnectionStringBlobClient(blobContainerName, blobName);
        await destBlobClient.UploadAsync(protectedFile);
    }
}