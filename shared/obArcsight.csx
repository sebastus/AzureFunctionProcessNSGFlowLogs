#load "getEnvironmentVariable.csx"
#load "convertToCEF.csx"

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;

static async Task obArcsight(string newClientContent, TraceWriter log)
{
    string arcsightAddress = getEnvironmentVariable("arcsightAddress");
    string arcsightPort = getEnvironmentVariable("arcsightPort");

    if (logstashAddress.Length == 0)
    {
        log.Error("Value for arcsightAddress is required.");
        return;
    }

    TcpClient client = new TcpClient(arcsightAddress, arcsightPort);

    foreach (var message in convertToCEF(newClientContent)) {
        TcpSend(client, message, log);
    }
}

static void TcpSend(TcpClient client, string message, TraceWriter log) {

    try {
        Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
        NetworkStream stream = client.GetStream();
        stream.Write(data, 0, data.Length);
    } catch (Exception ex) {
        log.Error($"Exception sending to ArcSight: {ex.Message}");
    } 

}