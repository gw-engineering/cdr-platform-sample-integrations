using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using CdrAzureFunctions;
using CdrAzureFunctions.Interfaces;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

[assembly: FunctionsStartup(typeof(Startup))]
namespace CdrAzureFunctions;

public class NewBlobEventGrid
{
    private readonly IWriteFile _fileWriterService;
    private readonly IProtectFile _protectFileService;

    public NewBlobEventGrid(IProtectFile protectFileService, IWriteFile fileWriterService)
    {
        _protectFileService = protectFileService ?? throw new ArgumentNullException(nameof(protectFileService));
        _fileWriterService = fileWriterService ?? throw new ArgumentNullException(nameof(fileWriterService));
    }

    [FunctionName("cdrnewblob")]
    public async Task Run(
        [ServiceBusTrigger("%AZURE_SERVICEBUS_NEWFILES_QUEUENAME%", Connection = "AZURE_SERVICEBUS_CONNECTIONSTRING")]
        string myQueueItem,
        ILogger log)
    {
        log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");

        var eventGridEvent = EventGridEvent.Parse(new BinaryData(myQueueItem));

        if (!IsSupportedEventType(eventGridEvent))
        {
            log.LogInformation($"Unsupported Event Grid Event Type: {eventGridEvent}");
            return;
        }

        var eventData = eventGridEvent.Data.ToObjectFromJson<StorageBlobCreatedEventData>();

        try
        {
            var protectResponse = await _protectFileService.ProtectFile(new Uri(eventData.Url));

            await _fileWriterService.WriteProtectedFile(protectResponse.BlobContainerName, protectResponse.Name,
                await protectResponse.GetResponseStream());
        }
        catch (Exception e)
        {
            log.LogError(e, $"Unable to process file: {eventData.Url}");
            throw;
        }
    }

    private static bool IsSupportedEventType(EventGridEvent eventGridEvent)
    {
        return eventGridEvent.EventType == "Microsoft.Storage.BlobCreated";
    }
}