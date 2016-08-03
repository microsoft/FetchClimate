using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    public class MissingValuesDictionary : Dictionary<string, object>
    {
        private static AutoRegistratingTraceSource traceSource = new AutoRegistratingTraceSource("MissingValuesDictionary");

        readonly string[] missingValueKeys = new string[] { "missing_value", "MissingValue" };
        IDataStorageDefinition storageDefinition;

        private readonly ReadOnlyDictionary<string,Type> dataTypes;

        public MissingValuesDictionary(IDataStorageDefinition storageDefinition)
        {
            dataTypes = new ReadOnlyDictionary<string, Type>(storageDefinition.VariablesTypes);

            foreach (var dataVarName in storageDefinition.VariablesMetadata.Keys)
            {
                //analyzing metadata
                var metadata = storageDefinition.VariablesMetadata[dataVarName];
                //looking for MV specification
                foreach (var item in missingValueKeys)
                {
                    if (metadata.ContainsKey(item)){
                        this[dataVarName] = metadata[item];
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Indexer coerces the type of MissingValue to dataTypes
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public object this[string index]
        {
            get {
                return base[index];
            }
            set
            {
                Type dataT = dataTypes[index];
                Type valT = value.GetType();
                if (valT == dataT)
                {
                    base[index] = value;                    
                }
                else
                {
                    try
                    {
                        object coercedVal = Convert.ChangeType(value, dataT);
                        traceSource.TraceEvent(System.Diagnostics.TraceEventType.Warning, 1, string.Format("The missing value attribute for variable {0} has type {1} hovever data has type {2}. Coercing the value from {3} to {4}", index, valT.ToString(), dataT.ToString(), value, coercedVal));
                        base[index] = coercedVal;
                    }
                    catch (InvalidCastException)
                    {
                        traceSource.TraceEvent(System.Diagnostics.TraceEventType.Warning, 2,string.Format( "The missing value attribute for variable {0} has type {1} which can not be converted to data has type {2}. Ignoring missing value", index, valT.ToString(), dataT.ToString(), value));
                    }                   
                }
            }
        }

        public object GetMissingValue(string variableName)
        {
            object missingValue = ContainsKey(variableName) ? this[variableName] : null;
            return missingValue;
        }
    }
}
