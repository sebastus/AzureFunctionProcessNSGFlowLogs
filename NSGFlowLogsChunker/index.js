var _ = require('../underscore');
var azure = require('../azure-storage');

module.exports = function(context) {
    // Additional inputs can be accessed by the arguments property
    // if(arguments.length === 4) {
    //     context.log('This function has 4 inputs');
    // }
    var trigger = context.bindings.inputBlock;

    context.log('BlobName is: ' + trigger.BlobName);
    context.log('Start is: ' + trigger.Start);
    context.log('Length is: ' + trigger.Length);

    context.done();
};