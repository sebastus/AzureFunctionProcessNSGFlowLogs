# Azure Function Process NSG Flow Logs

Network Security Group Flow Logs are emitted by the Azure Network Watcher service. This Azure Function App ingests these logs, determines which changes need to be processed and sends actionable data to a downstream processor via an Azure Storage Queue.

To learn more about Azure Network Watcher and Flow Logging, please see:</br> 

* [Azure network monitoring overview](https://docs.microsoft.com/en-us/azure/network-watcher/network-watcher-monitoring-overview)

* [Introduction to flow logging for Network Security Groups](https://docs.microsoft.com/en-us/azure/network-watcher/network-watcher-nsg-flow-logging-overview)

The Function App is comprised of 3 functions:</br>
* NSGFlowLogs - written in C#, ingests the log data from Azure Blob Storage. No knowledge of C# is needed to use it. There are a couple of configuration settings to input. There is no need to have or know how to use Visual Studio.  
* ProcessNSGFlowLogsCSharp - written in C#, this function is triggered by receipt of a queue message. Add your own logic to process the messages. In this case, you are a C# developer and have the necessary tools and experience.  
* ProcessNSGFlowLogsNodejs - written in Node.js, this function is a replica of the previous, but you can use your Node skills and tools to accomplish your desired outcome.  

# Installation

There are several ways to create an Azure Function and load your code into it. Here's one such example:

[Create your first function using the Azure CLI](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-first-azure-function-azure-cli)

This technique requires that your code be referencable in a github repo, and this is exactly what we need. If you examine a couple of the "insights-logs-" folders in this repo, you'll see a file called "function.json" in each. Function.json contains configuration details for the event hub that will trigger the function. The only setting of interest is "connection". This should contain the name of the setting that contains your event hub connection string for that hub / log category. You can have one connection string for all hubs or one for each or any mix thereof.

Because the repo needs to contain settings specific to your installation, I recommend you fork this repo and make your changes there. Then provide the address of your fork in the example above to populate your function app.

Note that the actual settings are not in the code. These are provided by you in the portal.

If you want to automate the creation of your Azure Function, there is a solution template that accomplishes this located here:

[Azure Function Deployment](https://github.com/sebastus/AzureFunctionDeployment)

