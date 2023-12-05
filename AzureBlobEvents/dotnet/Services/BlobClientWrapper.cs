using System;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CdrAzureFunctions.Interfaces;

namespace CdrAzureFunctions.Services;

public class BlobClientWrapper : IBlobClientWrapper
{
    private readonly BlobClient _blobClient;

    public BlobClientWrapper(Uri blobUri, StorageSharedKeyCredential credential)
    {
        _blobClient = new BlobClient(blobUri, credential);
    }

    public BlobClientWrapper(string connectionString, string blobContainerName, string blobName)
    {
        _blobClient = new BlobClient(connectionString, blobContainerName, blobName);
    }

    public string Name => _blobClient.Name;
    public string BlobContainerName => _blobClient.BlobContainerName;

    public async Task<Response> DownloadToAsync(Stream destination)
    {
        return await _blobClient.DownloadToAsync(destination);
    }

    public async Task<Response<BlobContentInfo>> UploadAsync(Stream content)
    {
        return await _blobClient.UploadAsync(content);
    }
}