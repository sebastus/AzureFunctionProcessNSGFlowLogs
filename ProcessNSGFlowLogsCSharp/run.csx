#load "../shared/chunk.csx"

using System;

public static void Run(Chunk inputChunk, TraceWriter log)
{
    log.Info($"C# Queue trigger function processed: {inputChunk}");
}

