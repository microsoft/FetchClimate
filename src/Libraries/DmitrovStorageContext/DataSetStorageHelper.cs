using Microsoft.Research.Science.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace Microsoft.Research.Science.FetchClimate2
{
    class DataStorageDefinition : IDataStorageDefinition
    {
        internal Dictionary<string, object> GlobalMetadata = new Dictionary<string, object>();
        internal Dictionary<string, ReadOnlyDictionary<string, object>> VariablesMetadata = new Dictionary<string, ReadOnlyDictionary<string, object>>();
        internal Dictionary<string, string[]> VariablesDimensions = new Dictionary<string, string[]>();
        internal Dictionary<string, Type> VariablesTypes = new Dictionary<string, Type>();
        internal Dictionary<string, int> DimensionsLengths = new Dictionary<string, int>();

        ReadOnlyDictionary<string, object> IDataStorageDefinition.GlobalMetadata
        {
            get { return new ReadOnlyDictionary<string,object>(this.GlobalMetadata); }
        }

        ReadOnlyDictionary<string, ReadOnlyDictionary<string, object>> IDataStorageDefinition.VariablesMetadata
        {
            get { return new ReadOnlyDictionary<string, ReadOnlyDictionary<string, object>>(this.VariablesMetadata); }
        }

        ReadOnlyDictionary<string, string[]> IDataStorageDefinition.VariablesDimensions
        {
            get { return new ReadOnlyDictionary<string, string[]>(this.VariablesDimensions); }
        }

        ReadOnlyDictionary<string, Type> IDataStorageDefinition.VariablesTypes
        {
            get { return new ReadOnlyDictionary<string, Type>(this.VariablesTypes); }
        }

        ReadOnlyDictionary<string, int> IDataStorageDefinition.DimensionsLengths
        {
            get { return new ReadOnlyDictionary<string, int>(this.DimensionsLengths); }
        }
    }

    class StorageResponse : IStorageResponse
    {
        private readonly Array data;
        private readonly IStorageRequest request;

        public StorageResponse(IStorageRequest request, Array data)
        {
            this.request = request;
            this.data = data;
        }

        public Array Data
        {
            get { return data; }
        }

        public IStorageRequest Request
        {
            get { return request; }
        }
    }


    public static class DataSetStorageHelper
    {
        static public readonly AutoRegistratingTraceSource traceSource = new AutoRegistratingTraceSource("DataSetStorageAdapter", SourceLevels.All);
        static readonly int[] RetryTimeouts = new int[] { 1000, 3000, 10000, 60000, 180000 }; // 1 sec, 3 sec, 10 sec, 1 min, 3 min.
        static readonly Random random = new Random();

        public static IStorageResponse PerformRequest(this DataSet storage, IStorageRequest r)
        {
            if (r.Shape == null && (r.Stride != null || r.Origin != null))
                throw new InvalidOperationException("Cannot perform data request with non-zero origin or stride and unknown shape");
            if (!storage.Variables.Contains(r.VariableName))
                throw new InvalidOperationException("Variable " + r.VariableName + " is not found in dataset");

            Array data = null;
            for (int i = 0; i < RetryTimeouts.Length; i++)
            {
                try
                {
                    System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                    traceSource.TraceEvent(System.Diagnostics.TraceEventType.Start,8,string.Format("requesting GetData. var \"{0}\"",r.VariableName));
                    if (r.Origin == null && r.Shape == null && r.Stride == null)
                        data = storage[r.VariableName].GetData();
                    else if (r.Stride == null) // r.Shape is not null (see preconditions)
                        data = storage[r.VariableName].GetData(r.Origin, r.Shape);
                    else
                        data = storage[r.VariableName].GetData(r.Origin, r.Stride, r.Shape);
                    sw.Stop();
                    traceSource.TraceEvent(System.Diagnostics.TraceEventType.Stop, 8, string.Format("GetData done in {1}. var \"{0}\"", r.VariableName,sw.Elapsed));
                    return new StorageResponse(r, data);
                }
                catch(Exception exc)
                {
                    int millisecSleep = (int)(RetryTimeouts[i] * (0.9 + random.NextDouble() * 0.2));
                    traceSource.TraceEvent(System.Diagnostics.TraceEventType.Error, 9, string.Format("GetData failed with {2}. var {1}. sleeping for {0} sec and retrying", millisecSleep * 0.001, r.VariableName, exc.ToString()));
                    System.Threading.Thread.Sleep(millisecSleep);
                }
            }

            traceSource.TraceEvent(System.Diagnostics.TraceEventType.Critical, 10, string.Format("Request to data set {0} failed after {1} retries", storage.URI, RetryTimeouts.Length));
            throw new InvalidOperationException(String.Format("Data is not available after {0} retries", RetryTimeouts.Length));
        }

        internal static DataStorageDefinition GetStorageDefinition(this DataSet dataSet)
        {
            var storageDefinition = new DataStorageDefinition();
            foreach (var globalMetaEntry in dataSet.Metadata)
            {
                storageDefinition.GlobalMetadata[globalMetaEntry.Key] = globalMetaEntry.Value;
            }
            foreach (var variable in dataSet.Variables)
            {
                var metadataDict = new Dictionary<string, object>();                
                storageDefinition.VariablesDimensions.Add(variable.Name, variable.Dimensions.Select(dim => dim.Name).ToArray());
                storageDefinition.VariablesTypes.Add(variable.Name, variable.TypeOfData);
                foreach (var varMetaEntry in variable.Metadata)
                {
                    metadataDict[varMetaEntry.Key] = varMetaEntry.Value;
                    
                }
                storageDefinition.VariablesMetadata[variable.Name] = new ReadOnlyDictionary<string, object>(metadataDict);
            }
            foreach (var dim in dataSet.Dimensions)
            {
                storageDefinition.DimensionsLengths.Add(dim.Name, dim.Length);
            }
            return storageDefinition;
        }
    }
}