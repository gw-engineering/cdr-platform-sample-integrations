using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using CdrAzureFunctions.Interfaces;
using CdrAzureFunctions.Responses;
using Flurl.Http;
using Polly;
using Polly.Retry;

namespace CdrAzureFunctions.Services;

public class FileBlobProtectService : IProtectFile
{
    private readonly IBlobClientFactory _blobClientFactory;
    private readonly string _cdrUrl;
    private readonly string _password;
    private readonly string _username;

    public FileBlobProtectService(IBlobClientFactory blobClientFactory)
    {
        _blobClientFactory = blobClientFactory ?? throw new ArgumentNullException(nameof(blobClientFactory));
        _username = Environment.GetEnvironmentVariable("CDR_USERNAME");
        _password = Environment.GetEnvironmentVariable("CDR_PASSWORD");
        _cdrUrl = Environment.GetEnvironmentVariable("CDR_URL");

        if (string.IsNullOrEmpty(_username) ||
            string.IsNullOrEmpty(_password) ||
            string.IsNullOrEmpty(_cdrUrl))
            throw new InvalidOperationException(
                "Unable to load valid CDR Platform configuration - check environment variables.");
    }

    public async Task<ProtectResponse> ProtectFile(Uri fileUri)
    {
        var blobClient = _blobClientFactory.CreateSharedKeyCredentialBlobClient(fileUri);

        using var stream = new MemoryStream();
        await blobClient.DownloadToAsync(stream);

        var cdrResponse = await RequestProtectFile(stream, blobClient.Name);

        return new ProtectResponse
        {
            BlobContainerName = blobClient.BlobContainerName,
            Name = blobClient.Name,
            ProtectRequestResponse = cdrResponse
        };
    }

    private async Task<IFlurlResponse> RequestProtectFile(Stream responseStream, string fileName)
    {
        try
        {
            responseStream.Position = 0;
            return await GetRetryPolicy()
                .ExecuteAsync(async () =>
                    await _cdrUrl
                        .WithBasicAuth(_username, _password)
                        .SetQueryParam("response-content", "noAnalysisReport")
                        .PostMultipartAsync(mp => mp
                            .AddFile("file", responseStream, fileName)));
        }
        catch (FlurlHttpException e)
        {
            Console.WriteLine(
                $"Unable to rebuild file: {fileName}, API returned status code: {e.StatusCode} Message: {e.Message}");
            if (e.StatusCode == null) e.Call.HttpResponseMessage.StatusCode = HttpStatusCode.InternalServerError;
            return new FlurlResponse(e.Call.HttpResponseMessage);
        }
    }

    private static AsyncRetryPolicy GetRetryPolicy()
    {
        return Policy
            .Handle<FlurlHttpException>(e => e.StatusCode == 429)
            .WaitAndRetryAsync(
                3,
                (retryCount, exception, context) =>
                {
                    var delta = ((FlurlHttpException)exception).Call.HttpResponseMessage.Headers.RetryAfter.Delta;
                    return delta ?? TimeSpan.FromSeconds(10);
                },
                async (e, ts, i, ctx) => { Console.WriteLine($"CDR Platform returned busy status - retrying {i}"); });
    }
}