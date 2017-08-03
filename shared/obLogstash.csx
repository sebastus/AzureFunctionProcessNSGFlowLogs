#r "System.Net.Http"
#load "getEnvironmentVariable.csx"

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http.Headers;

public class SingleHttpClientInstance
{
    private static readonly HttpClient HttpClient;

    static SingleHttpClientInstance()
    {
        HttpClient = new HttpClient();
    }

    public static async Task<HttpResponseMessage> SendToLogstash(HttpRequestMessage req)
    {
        HttpResponseMessage response = await HttpClient.SendAsync(req);
        return response;
    }
}

static async Task obLogstash(string standardizedEvents, TraceWriter log)
{
    string logstashAddress = getEnvironmentVariable("logstashAddress");
    if (logstashAddress.Length == 0){
        log.Error("Value for logstashAddress is required.");
        return;
    }

    ServicePointManager.Expect100Continue = true;
    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
    ServicePointManager.ServerCertificateValidationCallback =
    new System.Net.Security.RemoteCertificateValidationCallback(
        delegate { return true; });

    log.Error(string.Format("standardizedEvents as byte[]: {0}", standardizedEvents.ToCharArray()));

    string newClientContent = "{\"records\":[";
    newClientContent += standardizedEvents;
    newClientContent += "]}";

    var client = new SingleHttpClientInstance();
    try
    {
        HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, logstashAddress);
        req.Headers.Accept.Clear();
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        req.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "greg", "PepperSalam8"))));
        req.Content = new StringContent(newClientContent, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await SingleHttpClientInstance.SendToLogstash(req);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            log.Error($"StatusCode from Logstash: {response.StatusCode}, and reason: {response.ReasonPhrase}");
        }
    }
    catch (System.Net.Http.HttpRequestException e)
    {
        log.Error($"Error: \"{e.InnerException.Message}\" was caught while sending to Logstash.");
    }
    catch (Exception f)
    {
        log.Error($"Error \"{f.InnerException.Message}\" was caught while sending to Logstash. Unplanned exception.");
    }
}