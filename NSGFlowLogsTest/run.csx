#load "../shared/getEnvironmentVariable.csx"
#load "../shared/chunk.csx"

#r "Microsoft.WindowsAzure.Storage"
#r "Newtonsoft.Json"

using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Bindings.Runtime;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

public static async Task Run(TimerInfo myTimer, Binder inputQueue, ICollector<Chunk> outputQueue, TraceWriter log)
{
    log.Info($"C# NSG Flow Logs Test (timer) function executed at: {DateTime.Now}");

    var attributes = new Attribute[]
    {
        new QueueAttribute("nsgchunks"),
        new StorageAccountAttribute("AzureWebJobsStorage")
    };

    var queue = await inputQueue.BindAsync<CloudQueue>(attributes);

    var messages = await queue.GetMessagesAsync(2);

    foreach (var message in messages)
    {
        Chunk tmp = JsonConvert.DeserializeObject<Chunk>(message.AsString);
        outputQueue.Add(tmp);

        await queue.DeleteMessageAsync(message);
    }

}