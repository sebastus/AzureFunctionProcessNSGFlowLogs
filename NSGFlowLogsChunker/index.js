//var _ = require('underscore');
var azure = require('azure-storage');

module.exports = function(context) {

    var trigger = context.bindings.inputBlock;
    var blobName = trigger.BlobName;
    var start = trigger.Start;
    var end = trigger.Length + start - 1;

    context.log('BlobName is: ' + blobName);
    context.log('Start is: ' + start);
    context.log('Length is: ' + trigger.Length);

    var nsgSourceDataAccount = process.env['nsgSourceDataAccount'];
    var connStr = process.env[nsgSourceDataAccount];

    var blobService = azure.createBlobService(connStr).withFilter(new azure.ExponentialRetryPolicyFilter());

    var containerName = process.env['blobContainerName'];

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


            context.done();
        }
    });

};