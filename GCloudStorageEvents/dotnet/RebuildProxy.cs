using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace CDRPlatform.GCP.Integration;

public class RebuildProxy
{
    public const string DefaultPolicy = "{\"ContentManagementFlags\":{\"PdfContentManagement\":{\"Acroform\":1,\"ActionsAll\":1,\"EmbeddedFiles\":1,\"EmbeddedImages\":1,\"ExternalHyperlinks\":1,\"InternalHyperlinks\":1,\"Javascript\":1,\"Metadata\":1,\"Watermark\":\"\",\"DigitalSignatures\":1,\"ValueOutsideReasonableLimits\":1,\"RetainExportedStreams\":1},\"WordContentManagement\":{\"DynamicDataExchange\":1,\"EmbeddedFiles\":1,\"EmbeddedImages\":1,\"ExternalHyperlinks\":1,\"InternalHyperlinks\":1,\"Macros\":1,\"Metadata\":1,\"ReviewComments\":1},\"ExcelContentManagement\":{\"DynamicDataExchange\":1,\"EmbeddedFiles\":1,\"EmbeddedImages\":1,\"ExternalHyperlinks\":1,\"InternalHyperlinks\":1,\"Macros\":1,\"Metadata\":1,\"ReviewComments\":1},\"PowerPointContentManagement\":{\"DynamicDataExchange\":1,\"EmbeddedFiles\":1,\"EmbeddedImages\":1,\"ExternalHyperlinks\":1,\"InternalHyperlinks\":1,\"Macros\":1,\"Metadata\":1,\"ReviewComments\":1},\"ArchiveConfig\":{\"bmp\":1,\"doc\":1,\"docx\":1,\"emf\":1,\"gif\":1,\"jpg\":1,\"wav\":1,\"elf\":1,\"pe\":1,\"mp4\":1,\"mpg\":1,\"pdf\":1,\"png\":1,\"ppt\":1,\"pptx\":1,\"tif\":1,\"wmf\":1,\"xls\":1,\"xlsx\":1,\"mp3\":1,\"rtf\":1,\"coff\":1,\"macho\":1,\"svg\":1,\"webp\":1,\"unknown\":1},\"SvgConfig\":{\"ForeignObjects\":1,\"Hyperlinks\":1,\"Scripts\":1},\"WebpConfig\":{\"Metadata\":1},\"TiffConfig\":{\"GeoTiff\":1}}}";

    private readonly string _haloUrl;
    private readonly string _password;
    private readonly string _username;
    private readonly ILogger _logger;

    public RebuildProxy(ILogger logger)
    {
        _username = Environment.GetEnvironmentVariable("HALO_USERNAME") ?? throw new ArgumentException("Username is not set");
        _password = Environment.GetEnvironmentVariable("HALO_PASSWORD") ?? throw new ArgumentException("Password is not set");
        _haloUrl = Environment.GetEnvironmentVariable("HALO_URL") ?? throw new ArgumentException("Url is not set");
        _logger = logger;
    }

    /// <summary>
    /// Protects a file via the Halo Api
    /// </summary>
    /// <param name="fileStream">The file stream to protect, contents will be replaced with the rebuilt file</param>
    /// <param name="fileName">Name of the file to rebuild</param>
    /// <param name="cancellationToken">Request cancellation</param>
    public async Task<Stream> ProtectFileAsync(Stream fileStream, string fileName, CancellationToken cancellationToken)
    {
        try
        {
            var request = _haloUrl.WithBasicAuth(_username, _password)
                .SetQueryParam("format", "JSON")
                .SetQueryParam("generate-hash-types", "SHA256,SHA1,MD5")
                .SetQueryParam("response-content", "noAnalysisReport")
                .AppendPathSegments("api", "v3", "cdr-file");

            fileStream.Position = 0;

            var response = await GetRetryPolicy().ExecuteAsync(
                async () => await request.SendAsync(
                  HttpMethod.Post,
                  CreateBody(fileStream, fileName, DefaultPolicy),
                  HttpCompletionOption.ResponseHeadersRead,
                  cancellationToken)
            );

            _logger.LogInformation("Halo API returned status code '{statusCode}'", response.StatusCode);
            if (response.StatusCode != (int)HttpStatusCode.Created)
            {
                return new MemoryStream(Encoding.UTF8.GetBytes($"File could not be protected StatusCode: {response.StatusCode}"));
            }

            return await response.GetStreamAsync();
        }
        catch (FlurlHttpException e)
        {
            _logger.LogInformation("Unable to rebuild file: {fileName}, API returned status code: {statusCode} Message: {message}", fileName, e.StatusCode, e.Message);
            if (e.StatusCode == null)
            {
                e.Call.HttpResponseMessage.StatusCode = HttpStatusCode.InternalServerError;
            }

            return new MemoryStream(Encoding.UTF8.GetBytes($"File could not be protected StatusCode:{e.StatusCode}"));
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
                    var delta = (exception as FlurlHttpException)?.Call.HttpResponseMessage.Headers.RetryAfter?.Delta;
                    return delta ?? TimeSpan.FromSeconds(10);
                },
                (e, ts, i, ctx) => { Console.WriteLine($"Halo API returned busy status - retrying {i}"); return Task.CompletedTask; });
    }

    private static MultipartFormDataContent CreateBody(Stream file, string fileName, string policy)
    {
        MultipartFormDataContent content = new()
    {
        { new StreamContent(file), "file", fileName },
        { new StringContent(policy, Encoding.UTF8, "application/json"), "ContentManagementPolicy" }
    };

        return content;
    }
}