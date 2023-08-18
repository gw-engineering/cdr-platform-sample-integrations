using System.Text;
using System.Web;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Flurl.Http;
using Polly;
using Polly.Retry;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace CdrSampleLambda;

public class Function
{
    private readonly string? _cdrUrl;
    private readonly string? _username;
    private readonly string? _password;
    private readonly AmazonS3Client _client;

    /// <summary>
    /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
    /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
    /// region the Lambda function is executed in.
    /// </summary>
    public Function()
    {
        _username = Environment.GetEnvironmentVariable("CDR_USERNAME");
        _password = Environment.GetEnvironmentVariable("CDR_PASSWORD");
        _cdrUrl = Environment.GetEnvironmentVariable("CDR_URL");

        if (string.IsNullOrEmpty(_username) || 
            string.IsNullOrEmpty(_password) || 
            string.IsNullOrEmpty(_cdrUrl))
        {
            throw new InvalidOperationException("Unable to load valid CDR Platform configuration - check environment variables.");
        }

        _client = new AmazonS3Client();
    }

    /// <summary>
    /// This method is called for every Lambda invocation. This method takes in an SQS event object and can be used 
    /// to respond to SQS messages.
    /// </summary>
    /// <param name="event"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task FunctionHandler(SQSEvent @event, ILambdaContext context)
    {
        foreach(var message in @event.Records)
        {
            await ProcessMessageAsync(S3EventNotification.ParseJson(message.Body), context);
        }
    }

    private async Task ProcessMessageAsync(S3EventNotification message, ILambdaContext context)
    {
        foreach (var messageRecord in message.Records)
        {
            // S3 object keys swap spaces for '+' character - handle that by decoding.
            var fileName = HttpUtility.UrlDecode(messageRecord.S3.Object.Key, Encoding.UTF8);

            var s3ObjectResponse = await _client.GetObjectAsync(new GetObjectRequest
                { BucketName = messageRecord.S3.Bucket.Name, Key = fileName });

            await using (s3ObjectResponse.ResponseStream)
            {
                var cdrResponse = await RequestProtectFile(s3ObjectResponse.ResponseStream, fileName);

                if (cdrResponse.StatusCode != 201) continue;

                await using var protectedFile = await cdrResponse.GetStreamAsync();

                var destinationBucketName = $"{messageRecord.S3.Bucket.Name}-protected";
                await CreateBucketIfNotExists(destinationBucketName, messageRecord.AwsRegion);
                await WriteFileToS3(protectedFile, fileName, destinationBucketName);
            }
        }
    }

    private async Task WriteFileToS3(Stream file, string fileName, string bucketName)
    {
        await _client.PutObjectAsync(new PutObjectRequest
        {
            Key = fileName,
            BucketName = bucketName,
            InputStream = file
        });
    }

    private async Task<IFlurlResponse> RequestProtectFile(Stream responseStream, string fileName)
    {
        try
        {
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
            Console.WriteLine($"Unable to rebuild file: {fileName}, API returned status code: {e.StatusCode} Message: {e.Message}");
            return new FlurlResponse(e.Call.HttpResponseMessage);
        }
    }

    private async Task CreateBucketIfNotExists(string bucketName, S3Region region)
    {
        if (!await AmazonS3Util.DoesS3BucketExistV2Async(_client, bucketName))
        {
            await _client.PutBucketAsync(new PutBucketRequest
            {
                BucketName = bucketName,
                BucketRegion = region
            });
        }
    }

    private static AsyncRetryPolicy GetRetryPolicy()
    {
        return Policy
            .Handle<FlurlHttpException>(e => e.StatusCode == 429)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: (retryCount, exception, context) =>
                {
                    var delta = ((FlurlHttpException)exception).Call.HttpResponseMessage.Headers.RetryAfter.Delta;
                    return delta ?? TimeSpan.FromSeconds(10);
                }, 
                onRetryAsync: async (e, ts, i, ctx) =>
                {
                    Console.WriteLine($"CDR Platform returned busy status - retrying {i}");
                });
    }
}