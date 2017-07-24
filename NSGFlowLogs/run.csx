// Blob trigger binding to a CloudBlockBlob
#r "Microsoft.WindowsAzure.Storage"

using System;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

const int MAXDOWNLOADBYTES = 10240;

public static async Task Run(CloudBlockBlob myBlob, CloudTable checkpointTable, string subId, string resourceGroup, string nsgName, string blobYear, string blobMonth, string blobDay, string blobHour, TraceWriter log)
{
    var blobName = new BlobName(subId, resourceGroup, nsgName, blobYear, blobMonth, blobDay, blobHour);

    // get checkpoint
    Checkpoint checkpoint = await GetCheckpoint(blobName, checkpointTable, log);

    //// break up the block list into 10k chunks
    List<Chunk> chunks = new List<Chunk>();
    long currentChunkSize = 0;
    string currentChunkLastBlockName = "";
    long currentStartingByteOffset = 0;

    bool firstBlockItem = true;
    bool foundStartingOffset = false;
    bool tieOffChunk = false;
    string msg = string.Format("Current checkpoint last block name: {0}", checkpoint.LastBlockName);
    log.Info(msg);
    foreach (var blockListItem in myBlob.DownloadBlockList(BlockListingFilter.Committed))
    {
        msg = string.Format("Block name: {0}, length: {1}", blockListItem.Name, blockListItem.Length);
        log.Info(msg);

        // skip first block, but add to the offset
        // find the starting block based on checkpoint LastBlockName
        // if it's empty, start at the beginning
        if (!foundStartingOffset)
        {
            if (firstBlockItem)
            {
                currentStartingByteOffset += blockListItem.Length;
                firstBlockItem = false;
                if (checkpoint.LastBlockName == "")
                {
                    foundStartingOffset = true;
                }
            }
            else
            {
                if (blockListItem.Name == checkpoint.LastBlockName)
                {
                    foundStartingOffset = true;
                }
                currentStartingByteOffset += blockListItem.Length;
            }
        }
        else
        {
            // tieOffChunk = add current chunk to the list, initialize next chunk counters
            // conditions to account for:
            // 1) current chunk is empty & not the last block (size = 9)
            //   a) add blockListItem to current chunk
            //   b) loop
            // 2) current chunk is empty & last block (size = 9)
            //   a) do not add blockListItem to current chunk
            //   b) loop terminates
            //   c) chunk last added to the list is the last chunk
            // 3) current chunk is not empty & not the last block
            //   a) if size of block + size of chunk >10k
            //     i) add chunk to list  <-- tieOffChunk
            //     ii) reset chunk counters
            //   b) add blockListItem to chunk
            //   c) loop
            // 4) current chunk is not empty & last block
            //   a) add chunk to list  <-- tieOffChunk
            //   b) do not add blockListItem to chunk
            //   c) loop terminates
            tieOffChunk = ((blockListItem.Length == 9) && (currentChunkSize != 0)) || (currentChunkSize + blockListItem.Length > MAXDOWNLOADBYTES);            
            if (tieOffChunk)
            {
                // chunk complete, add it to the list & reset counters
                chunks.Add(new Chunk { Length = currentChunkSize, LastBlockName = currentChunkLastBlockName, Start = currentStartingByteOffset });
                currentStartingByteOffset += currentChunkSize; // the next chunk starts at this offset
                currentChunkSize = 0;
                tieOffChunk = false;
            }
            if (blockListItem.Length != 9)
            {
                currentChunkSize += blockListItem.Length;
                currentChunkLastBlockName = blockListItem.Name;
            }
        }
    }

    msg = string.Format("Chunks to download & transmit: {0}", chunks.Count);
    log.Info(msg);
    foreach (var chunk in chunks)
    {
        msg = string.Format("Starting byte offset: {0}, size of chunk: {1}, name of last block: {2}", chunk.Start, chunk.Length, chunk.LastBlockName);
        log.Info(msg);
    }

    // update the checkpoint
    if (chunks.Count > 0)
    {
        var lastChunk = chunks[chunks.Count - 1];
        PutCheckpoint(blobName, checkpointTable, lastChunk.LastBlockName, lastChunk.Start + lastChunk.Length, log);
    }
}

public static async Task<Checkpoint> GetCheckpoint(BlobName blobName, CloudTable checkpointTable, TraceWriter log)
{

    var checkpointPartitionKey = blobName.GetPartitionKey();
    var checkpointRowKey = blobName.GetRowKey();

    Checkpoint checkpoint = null;
    try
    {
        TableOperation operation = TableOperation.Retrieve<Checkpoint>(checkpointPartitionKey, checkpointRowKey);
        TableResult result = checkpointTable.Execute(operation);
        checkpoint = (Checkpoint)result.Result;

        if (checkpoint == null)
        {
            checkpoint = new Checkpoint(checkpointPartitionKey, checkpointRowKey, "", 0);
        }
    }
    catch (Exception ex)
    {
        var msg = string.Format("Error fetching checkpoint for blob: {0}", ex.Message);
        log.Info(msg);
    }

   return checkpoint;

}

public static async Task PutCheckpoint(BlobName blobName, CloudTable checkpointTable, string lastBlockName, long startingByteOffset, TraceWriter log)
{
    var newCheckpoint = new Checkpoint(
        blobName.GetPartitionKey(), blobName.GetRowKey(),
        lastBlockName, startingByteOffset);
    TableOperation operation = TableOperation.InsertOrReplace(newCheckpoint);
    checkpointTable.Execute(operation);
}
public class BlobName
{
    public string SubscriptionId { get; set; }
    public string ResourceGroupName { get; set; }
    public string NsgName { get; set; }
    public string Year { get; set; }
    public string Month { get; set; }
    public string Day { get; set; }
    public string Hour { get; set; }

    public BlobName(string subscriptionId, string resourceGroupName, string nsgName, string year, string month, string day, string hour)
    {
        SubscriptionId = subscriptionId;
        ResourceGroupName = resourceGroupName;
        NsgName = nsgName;
        Year = year;
        Month = month;
        Day = day;
        Hour = hour;
    }

    public string GetPartitionKey()
    {
        return string.Format("{0}_{1}_{2}_{3}_{4}", SubscriptionId.Replace("-", "_"), ResourceGroupName, NsgName, Year, Month);
    }

    public string GetRowKey()
    {
        return string.Format("{0}_{1}", Day, Hour);
    }
}

public class Checkpoint : TableEntity, IDisposable
{
    public Checkpoint() { }

    public Checkpoint(string partitionKey, string rowKey, string blockName, long offset)
    {
        PartitionKey = partitionKey;
        RowKey = rowKey;
        LastBlockName = blockName;
        StartingByteOffset = offset;
    }

    public string LastBlockName { get; set; }
    public long StartingByteOffset { get; set; }

    bool disposed = false;
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
            return;

        if (disposing)
        {
        }

        disposed = true;
    }
}

public class Chunk
{
    public long Length { get; set; }
    public long Start { get; set; }
    public string LastBlockName { get; set; }
}

