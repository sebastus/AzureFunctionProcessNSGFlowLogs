module.exports = function (context, inputChunk) {
    context.log('Node.js queue trigger function processed work item', inputChunk);
    context.done();
};