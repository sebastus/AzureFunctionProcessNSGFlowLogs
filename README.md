# Azure Function Process NSG Flow Logs

Network Security Group Flow Logs are emitted by the Azure Network Watcher service. This Azure Function App ingests these logs, determines which changes need to be processed and sends actionable data to a downstream processor via an Azure Storage Queue.

To learn more about Azure Network Watcher and Flow Logging, please see:</br> 

* [Azure network monitoring overview](https://docs.microsoft.com/en-us/azure/network-watcher/network-watcher-monitoring-overview)

* [Introduction to flow logging for Network Security Groups](https://docs.microsoft.com/en-us/azure/network-watcher/network-watcher-nsg-flow-logging-overview)

The Function App is comprised of 3 functions:  
1. NSGFlowLogs - this is the single point of entry for flow logs. The function is triggered by changes to blobs as they are written to by network watcher. 
    Input: "myBlob"
    Output: "nsgprocessing"
2. NSGFlowLogsIngestTest - allows the developer to throttle the incoming messages as they move to the next step  
    Input: Q"nsgprocessing"
    Output: Q"nsgblocks"
3. NSGFlowLogsChunkerCSharp - gets messages, breaks them into 100kb chunks
    Input: Q"nsgprocessing" if running at full speed
           Q"nsgblocks" if throttled by NSGFlowLogsIngestTest
    Output: Q"nsgchunks"
4. NSGFlowLogsTest - allows developer to throttle the flow out to the collector
    Input: Q"nsgchunks"
    Output: Q"nsgchunktest"
5. NSGFlowLogsSendChunk - sends a chunk downstream to the log aggregator
    Input: Q"nsgchunktest" if throttling output
           Q"nsgchunks" if not throttling output
    Output: downstream log aggregator

If throttled on both input and output, the pipeline looks like this:

myBlob -> nsgprocessing -> nsgblocks -> nsgchunks -> nsgchunktest -> downstream log aggregator

If not throttled at all:

myBlob -> nsgprocessing -> nsgchunks -> downstream log aggregator


# Installation

There are several ways to create an Azure Function and load your code into it. Here's one such example:

[Create your first function using the Azure CLI](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-first-azure-function-azure-cli)

This technique requires that your code be referencable in a github repo, and this is exactly what we need. If you examine a couple of the "insights-logs-" folders in this repo, you'll see a file called "function.json" in each. Function.json contains configuration details for the event hub that will trigger the function. The only setting of interest is "connection". This should contain the name of the setting that contains your event hub connection string for that hub / log category. You can have one connection string for all hubs or one for each or any mix thereof.

Because the repo needs to contain settings specific to your installation, I recommend you fork this repo and make your changes there. Then provide the address of your fork in the example above to populate your function app.

Note that the actual settings are not in the code. These are provided by you in the portal.

If you want to automate the creation of your Azure Function, there is a solution template that accomplishes this located here:

[Azure Function Deployment](https://github.com/sebastus/AzureFunctionDeployment)

