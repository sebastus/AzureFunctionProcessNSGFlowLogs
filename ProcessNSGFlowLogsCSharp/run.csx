using System;

public static void Run(Chunk inputChunk, TraceWriter log)
{
    log.Info($"C# Queue trigger function processed: {inputChunk}");
}

public class Chunk
{
    public string BlobAccountConnectionName { get; set; }
    public long Length { get; set; }
    public long Start { get; set; }
    public string LastBlockName { get; set; }

    public override string ToString()
    {
        var msg = string.Format("Connection: {0}, Block: {1}, Start: {2}, Length: {3}", BlobAccountConnectionName, LastBlockName, Start, Length);
        return msg;
    }
}

