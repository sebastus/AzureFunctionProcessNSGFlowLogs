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
    NetworkStream stream = client.GetStream();

    int count = 5;
    Byte[] transmission = new Byte[] {};
    foreach (var message in convertToCEF(newClientContent, log)) {

        transmission = AppendToTransmission(transmission, AppendCRLF(System.Text.Encoding.ASCII.GetBytes(message)));

        // batch up the messages
        if (count-- == 0) {
            await TcpSendAsync(stream, transmission, log);
            count = 5;
            transmission = new Byte[] {};
        }

    }
    await stream.FlushAsync();
}

static async Task TcpSendAsync(NetworkStream stream, Byte[] transmission, TraceWriter log) {

    try {
        await stream.WriteAsync(transmission, 0, transmission.Length);
    } catch (Exception ex) {
        log.Error($"Exception sending to ArcSight: {ex.Message}");
    } 

}

static Byte[] AppendCRLF(Byte[] data)
{
    var dataLength = data.Length;
    Byte[] datacrlf = new Byte[dataLength + 2];
    data.CopyTo(datacrlf, 0);
    datacrlf[dataLength] = 0x0D;
    datacrlf[dataLength + 1] = 0x0A;
    return datacrlf;
}

static Byte[] AppendToTransmission(Byte[] existingMessages, Byte[] appendMessage)
{
    var dataLength = existingMessages.Length + appendMessage.Length;

    Byte[] newMessages = new Byte[dataLength];

    existingMessages.CopyTo(newMessages, 0);
    appendMessage.CopyTo(newMessages, existingMessages.Length);

    return newMessages;
}