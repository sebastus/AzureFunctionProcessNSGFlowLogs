#load "../shared/sendDownstream.csx"
#load "../shared/getEnvironmentVariable.csx"

#r "Microsoft.WindowsAzure.Storage"

using System;
using Microsoft.Azure.WebJobs;

public static async Task Run(string inputManualMessage, string newClientContent, TraceWriter log)
{
    log.Info($"C# NSG Flow Logs Manual (Queue trigger) function processed: {inputManualMessage}, length of blob is {newClientContent.Length}");


//    await SendMessagesDownstream(newClientContent, log);
}

