#r "Newtonsoft.Json"
#load "classes.csx"

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

static System.Collections.Generic.IEnumerable<string> convertToCEF(string newClientContent, TraceWriter log)
{
    // newClientContent is a json string with records

    NSGFlowLogRecords logs = JsonConvert.DeserializeObject<NSGFlowLogRecords>(newClientContent);

    string cefRecordBase = "";
    foreach (var record in logs.records)
    {
        cefRecordBase += record.MakeCEFTime();
        cefRecordBase += "|Microsoft.Network";
        cefRecordBase += "|NETWORKSECURITYGROUPS";
        cefRecordBase += "|" + record.category;
        cefRecordBase += "|" + record.operationName;
        cefRecordBase += "|0";  // severity is always 0
        cefRecordBase += "|deviceExternalId=" + record.MakeDeviceExternalID();

        string cefOuterFlowRecord = cefRecordBase;
        foreach (var outerFlows in record.properties.flows)
        {
            cefOuterFlowRecord += " cs1=" + outerFlows.rule;

            string cefInnerFlowRecord = cefOuterFlowRecord;
            foreach (var innerFlows in outerFlows.flows)
            {
                var firstFlowTuple = new NSGFlowLogTuple(innerFlows.flowTuples[0]);
                cefInnerFlowRecord += (firstFlowTuple.GetDirection == "I" ? " dmac=" : " smac=") + innerFlows.MakeMAC();

                foreach (var flowTuple in innerFlows.flowTuples)
                {
                    var tuple = new NSGFlowLogTuple(flowTuple);
                    yield return cefInnerFlowRecord + " " + tuple.ToString();
                }
            }
        }
    }
}