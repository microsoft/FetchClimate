using Microsoft.Research.Science.Data;
using System.Reactive.Linq;
using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    class StorageRequest : IStorageRequest
    {
        private readonly string variableName;
        private readonly int[] origin;
        private readonly int[] stride;
        private readonly int[] shape;

        /// <summary>Constructs request for part of certain variable</summary>
        /// <param name="variableName">Name of variable in storage</param>
        /// <param name="origin">Origin of data part. Null means start of the entire data</param>
        /// <param name="stride">Stride along each dimension. Null means continuous data along each dimension</param>
        /// <param name="shape">Size of data to return. Null means entire data.</param>
        public StorageRequest(string variableName, int[] origin = null, int[] stride = null, int[] shape = null)
        {
            this.variableName = variableName;
            this.origin = origin;
            this.stride = stride;
            this.shape = shape;
        }

        public string VariableName
        {
            get { return variableName; }
        }

        public int[] Origin
        {
            get { return origin; }
        }

        public int[] Stride
        {
            get { return stride; }
        }

        public int[] Shape
        {
            get { return shape; }
        }
    }

    /// <summary>Storage context that linearizes access to some Dmitrov data set</summary>
    public class LinearizingStorageContext : IStorageContext
    {
        struct StorageTask
        {
            public TaskCompletionSource<IStorageResponse[]> Completion;
            public IStorageRequest[] Request;
        }

        private readonly Subject<StorageTask> requestHandler = new Subject<StorageTask>();
        private readonly DataSet dataSet;
        private DataStorageDefinition definition;
        private long bytesRead = 0;

        /// <summary>
        /// The number of bytes read from the data storage
        /// </summary>
        public long BytesRead
        {
            get
            {
                return bytesRead;
            }
        }

        public DataSet Storage
        {
            get
            {
                return dataSet;
            }
        }

        public LinearizingStorageContext(DataSet dataSet)
        {
            this.dataSet = dataSet;
            this.definition = dataSet.GetStorageDefinition();
            var uri = DataSetUri.Create(this.dataSet.URI);
            if (uri.ContainsParameter("dimensions"))
            {
                string dimstr = uri.GetParameterValue("dimensions");
                var dimpairs = dimstr.Split(',');
                foreach (var p in dimpairs)
                {
                    var pair = p.Split(':');
                    this.definition.DimensionsLengths[pair[0]] = int.Parse(pair[1]);
                }
            }
            requestHandler.ObserveOn(new EventLoopScheduler()).Subscribe(v =>
            {
                Perform(v);
            });
        }

        public IDataStorageDefinition StorageDefinition
        {
            get { return definition; }
        }

        public async Task<IStorageResponse[]> GetDataAsync(params IStorageRequest[] requests)
        {
            var fixedrequests = new IStorageRequest[requests.Length];
            for (int i = 0; i < requests.Length; ++i)
            {
                if (requests[i].Shape != null)
                {
                    for (int j = 0; j < requests[i].Shape.Length; ++j)
                    {
                        int top = (requests[i].Origin == null ? 0 : requests[i].Origin[j]) + (requests[i].Stride == null ? 1 : requests[i].Stride[j]) * requests[i].Shape[j];
                        if (top > definition.DimensionsLengths[definition.VariablesDimensions[requests[i].VariableName][j]])
                            throw new IndexOutOfRangeException("Requested area is out of bounds.");
                    }
                    fixedrequests[i] = requests[i];
                }
                else
                {
                    int[] shp = new int[definition.VariablesDimensions[requests[i].VariableName].Length];
                    for (int j = 0; j < shp.Length; ++j)
                        shp[j] = (definition.DimensionsLengths[definition.VariablesDimensions[requests[i].VariableName][j]] - (requests[i].Origin == null ? 0 : requests[i].Origin[j])) / (requests[i].Stride == null ? 1 : requests[i].Stride[j]);
                    fixedrequests[i] = new StorageRequest(requests[i].VariableName, requests[i].Origin, requests[i].Stride, shp);
                }
            }
            var st = new StorageTask
            {
                Completion = new TaskCompletionSource<IStorageResponse[]>(),
                Request = fixedrequests
            };
            requestHandler.OnNext(st);
            return await st.Completion.Task;
        }

        public async Task<Array> GetDataAsync(string variableName, int[] origin = null, int[] stride = null, int[] shape = null)
        {
            return (await GetDataAsync(new StorageRequest(variableName, origin, stride, shape)))[0].Data;
        }

        private void Perform(StorageTask task)
        {
            try
            {
                task.Completion.SetResult(
                    task.Request.Select(r =>
                    {
                        int[] shape = r.Shape == null
                            ? dataSet.Variables[r.VariableName].GetShape()
                            : r.Shape;
                        //lock ("bytesCounter")
                        //{
                        //    bytesRead +=
                        //        System.Runtime.InteropServices.Marshal.SizeOf(dataSet.Variables[r.VariableName].TypeOfData)
                        //        * shape.Aggregate(1L, (accShape, dimLen) => accShape * dimLen); //multipling dims length inside each request                            
                        //}
                        return dataSet.PerformRequest(r);
                    }).ToArray());
            }
            catch (Exception exc)
            {
                task.Completion.SetException(exc);
            }
        }
    }
}