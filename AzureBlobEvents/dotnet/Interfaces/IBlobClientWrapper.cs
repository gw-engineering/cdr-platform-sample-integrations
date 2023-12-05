using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs.Models;

namespace CdrAzureFunctions.Interfaces;

public interface IBlobClientWrapper
{
    string Name { get; }

    string BlobContainerName { get; }

    Task<Response> DownloadToAsync(Stream destination);

    Task<Response<BlobContentInfo>> UploadAsync(Stream content);
}