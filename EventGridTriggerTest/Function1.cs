// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EventGridTriggerTest
{
    [StorageAccount("BlobConnectionString")]
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task RunAsync([EventGridTrigger]EventGridEvent eventGridEvent,
            [Blob("{data.url}", FileAccess.Read)] Stream inputBlob,
            IBinder binder, 
            ILogger log)
        {
            log.LogInformation("-------Begin Execution----------");

            log.LogInformation("Event Grid Data");
            log.LogInformation(JsonConvert.SerializeObject(eventGridEvent));
            log.LogInformation("******");

            StorageBlobCreatedEventData createdEventData = eventGridEvent.Data.ToObjectFromJson<StorageBlobCreatedEventData>();
            int pos = createdEventData.Url.LastIndexOf("/");
            string name = createdEventData.Url.Substring(pos + 1);

            string outputFolder = Environment.GetEnvironmentVariable("OutputFolder");
            string outputExtension = Environment.GetEnvironmentVariable("OutputExtension");
            string outputPath = $"{outputFolder}/{name}.{outputExtension}";

            var outputBlobAttribute = new BlobAttribute(outputPath, FileAccess.Write);

            try
            {
                using (var outputBlob = await binder.BindAsync<Stream>(outputBlobAttribute))
                {
                    await inputBlob.CopyToAsync(outputBlob);
                    log.LogInformation("Copy Successful. {input} to {output}", name, outputPath);
                }
            }
            catch (Exception ex)
            {

                log.LogError("Error encrypting {message}, {innermessage}", ex.Message, JsonConvert.SerializeObject(ex));
            }
            log.LogInformation("---------------");
        }
    }
}
