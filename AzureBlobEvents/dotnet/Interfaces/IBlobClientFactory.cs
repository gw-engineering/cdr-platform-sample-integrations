using System;

namespace CdrAzureFunctions.Interfaces;

public interface IBlobClientFactory
{
    public IBlobClientWrapper CreateSharedKeyCredentialBlobClient(Uri blobUri);

    public IBlobClientWrapper CreateConnectionStringBlobClient(string blobContainerName, string blobName);
}