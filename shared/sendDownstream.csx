#load "getEnvironmentVariable.csx"
#load "obLogstash.csx"
#load "obArcsight.csx"

public static async Task SendMessagesDownstream(string myMessages, TraceWriter log)
{
    string outputBinding = getEnvironmentVariable("outputBinding");
    if (outputBinding.Length == 0)
    {
        log.Error("Value for outputBinding is required. Permitted values are: 'logstash'.");
        return;
    }

    switch (outputBinding)
    {
        case "logstash":
            await obLogstash(myMessages, log);
            break;
        case "arcsight":
            await obArcsight(myMessages, log);
            break;
    }
}