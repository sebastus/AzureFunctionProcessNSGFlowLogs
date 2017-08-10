var _ = require('underscore');
var azure = require('azure-storage');

var outputQueue = [];
var currentChunk = -1;

const MAX_CHUNK_SIZE = 100 * 1024;

module.exports = function(context) {

    var trigger = context.bindings.inputBlock;
    var blobName = trigger.BlobName;
    var start = trigger.Start;
    var end = trigger.Length + start - 1;

    context.log('BlobName is: ' + blobName);
    context.log('Start is: ' + start);
    context.log('Length is: ' + trigger.Length);

    var nsgSourceDataAccount = process.env.nsgSourceDataAccount;
    context.log('Data account is: ' + nsgSourceDataAccount);
    var connStr = process.env[nsgSourceDataAccount];
    context.log('Connection string is: ' + connStr);

    var blobService = azure.createBlobService(connStr).withFilter(new azure.ExponentialRetryPolicyFilter());

    var containerName = process.env.blobContainerName;
    context.log('containerName is: ' + containerName);

    blobService.getBlobToText(containerName, blobName, {rangeStart:start, rangeEnd: end}, function (downloadErr, blobText, blob, downloadResponse) {

        if (downloadErr !== null) {

            console.log("error reading blob");

        } else {

            var curlyBrace = blobText.indexOf('{');
            if (curlyBrace !== 0) {
                blobText = blobText.substr(curlyBrace);
            }

            var records = '{"records":[' + blobText + ']}';
            var recordsJson = JSON.parse(records);

            outputQueue.push({BlobName: blobName, Start: start, Length: 0});
            currentChunk = 0;

            _.each(recordsJson, makeChunks);
        }
        context.done();
    });

};

function makeChunks(record) {
    
    recordLength = JSON.stringify(record).length;

    if (recordLength + outputQueue[currentChunk].Length > MAX_CHUNK_SIZE) {
        blobName = outputQueue[currentChunk].BlobName;
        newStart = outputQueue[currentChunk].Start + outputQueue[currentChunk].Length;
        outputQueue.push({BlobName:blobName, Start:newStart, Length:0});
        currentChunk += 1;
    }

    outputQueue[currentChunk].Length += recordLength;
};