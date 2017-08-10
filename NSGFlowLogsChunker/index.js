var _ = require('underscore');
var azure = require('azure-storage');

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

            context.log("error reading blob");

        } else {

            context.log("read the blob");

            var curlyBrace = blobText.indexOf('{');
            if (curlyBrace !== 0) {
                blobText = blobText.substr(curlyBrace);
            }

            var records = '{"records":[' + blobText + ']}';
            var recordsJson = JSON.parse(records);
            context.log('# of records is: ' + recordsJson.records.length);

            var outputQueueMessages = [];
            outputQueueMessages.push({BlobName: blobName, Start: start, Length: 0});
            var currentChunk = 0;

            _.each(recordsJson.records, function(record) {

                recordLength = JSON.stringify(record).length;
                context.log('recordLength is: ' + recordLength);

                if (recordLength + outputQueueMessages[currentChunk].Length > MAX_CHUNK_SIZE) {

                    newStart = outputQueueMessages[currentChunk].Start + outputQueueMessages[currentChunk].Length;

                    outputQueueMessages.push({BlobName:blobName, Start:newStart, Length:0});

                    context.log(JSON.stringify(outputQueueMessages[currentChunk]));

                    currentChunk += 1;
                }

                outputQueueMessages[currentChunk].Length += recordLength;
            });

            context.bindings.outputQueue = outputQueueMessages;
        }
        context.done();
    });

};
