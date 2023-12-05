using System;
using Azure.Storage;
using CdrAzureFunctions.Interfaces;

namespace CdrAzureFunctions.Services;

public class BlobClientFactory : IBlobClientFactory
{
    public IBlobClientWrapper CreateSharedKeyCredentialBlobClient(Uri blobUri)
    {
        var credential = new StorageSharedKeyCredential(Environment.GetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_NAME"),
            Environment.GetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_KEY"));
        return new BlobClientWrapper(blobUri, credential);
    }

    public IBlobClientWrapper CreateConnectionStringBlobClient(string blobContainerName, string blobName)
    {
        var connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_DESTINATION_CONNECTIONSTRING");
        return new BlobClientWrapper(connectionString, blobContainerName, blobName);
    }
}