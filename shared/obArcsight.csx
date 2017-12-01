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

    int count = 0;
    Byte[] transmission = new Byte[] {};
    foreach (var message in convertToCEF(newClientContent, log)) {

        try {
            transmission = AppendToTransmission(transmission, message);

            // batch up the messages
            if (count++ == 1000) {
                await stream.WriteAsync(transmission, 0, transmission.Length);
                count = 0;
                transmission = new Byte[] {};
            }
        } catch (Exception ex) {
            log.Error($"Exception sending to ArcSight: {ex.Message}");
        }
    }
    if (count > 0) {
        try {
            await stream.WriteAsync(transmission, 0, transmission.Length);
        } catch (Exception ex) {
            log.Error($"Exception sending to ArcSight: {ex.Message}");
        }
    }
    await stream.FlushAsync();
}

static Byte[] AppendToTransmission(Byte[] existingMessages, string appendMessage)
{
    Byte[] appendMessageBytes = Encoding.ASCII.GetBytes(appendMessage);
    Byte[] crlf = new Byte[] { 0x0D, 0x0A };

    Byte[] newMessages = new Byte[existingMessages.Length + appendMessage.Length + 2];

    existingMessages.CopyTo(newMessages, 0);
    appendMessageBytes.CopyTo(newMessages, existingMessages.Length);
    crlf.CopyTo(newMessages, existingMessages.Length + appendMessageBytes.Length);
    
    return newMessages;
}

