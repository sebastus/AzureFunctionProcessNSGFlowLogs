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

    if (arcsightAddress.Length == 0 || arcsightPort.Length == 0)
    {
        log.Error("Values for arcsightAddress and arcsightPort are required.");
        return;
    }

    TcpClient client = new TcpClient(arcsightAddress, Convert.ToInt32(arcsightPort));

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