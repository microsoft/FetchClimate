using Microsoft.Research.Science.Data;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.Research.Science.FetchClimate2
{
    public abstract class RequestContext : LinearizingStorageContext, IRequestContext
    {
        protected readonly IFetchEngine engine;
        protected readonly IFetchRequest request;
        protected readonly IDictionary<string, string> ds2fc;
        protected readonly ushort id;
        protected Array uncertainty;

        public RequestContext(IFetchRequest request, IFetchEngine engine, IDictionary<string, string> ds2fc, DataSet storage, ushort id)
            : base(storage)
        {
            this.id = id;
            this.engine = engine;
            this.request = request;
            this.ds2fc = ds2fc;
        }

        public IFetchRequest Request
        {
            get { return request; }
        }

        public ushort ID
        {
            get { return id; }
        }

        public abstract Task<Array> GetMaskAsync(Array uncertainty);

        /// <summary>Translates request from current data source namespace to FC namespace</summary>
        /// <param name="r">Request with local namings</param>
        /// <returns>Request with FC namings</returns>
        public IFetchRequest TranslateRequestToFC(IFetchRequest r)
        {
            string fcName;
            if (!ds2fc.TryGetValue(r.EnvironmentVariableName, out fcName))
                return r;
            else
                return new FetchRequest(fcName, r.Domain, r.ReproducibilityTimestamp, r.ParticularDataSource);
        }

        public Task<IFetchResponse[]> FetchDataAsync(params IFetchRequest[] requests)
        {
            // TODO: Consider using Task.WhenAll(...)
            return Task<IFetchResponse[]>.Factory.StartNew(() =>
                requests.Select(r =>
                {
                    var tr = TranslateRequestToFC(r);
                    var fr = engine.PerformRequestAsync(new FetchRequest(tr.EnvironmentVariableName, tr.Domain, request.ReproducibilityTimestamp, tr.ParticularDataSource)).Result;
                    return new FetchResponse(fr.Request, fr.Values, fr.Uncertainty);
                }).ToArray(), TaskCreationOptions.LongRunning);
        }

        public Array ReportUncertainty()
        {
            Array result = uncertainty;
            uncertainty = null;
            return result;
        }

        public abstract IRequestContext CopyWithNewRequest(IFetchRequest request);        
    }

    public class StandaloneRequestContext : RequestContext
    {
        public StandaloneRequestContext(IFetchRequest request, IFetchEngine engine, IDictionary<string, string> ds2fc, DataSet storage, ushort id)
            : base(request, engine, ds2fc, storage, id)
        {
        }

        public override Task<Array> GetMaskAsync(Array uncertainty)
        {
            if (uncertainty != null)
            {
                int[] dataShape = request.Domain.GetDataArrayShape();
                if (dataShape.Length != uncertainty.Rank)
                    throw new ArgumentException("Wrong rank of uncertainty array", "uncertainty");
                for (int i = 0; i < dataShape.Length; i++)
                    if (dataShape[i] != uncertainty.GetLength(i))
                        throw new ArgumentException("Wrong shape of uncertainty array", "uncertainty");
            }
            this.uncertainty = uncertainty;
            return Task<Array>.Factory.StartNew(() =>
                ArrayHelper.GetConstantArray<bool>(request.Domain.GetDataArrayShape(), true));
        }

        public override IRequestContext CopyWithNewRequest(IFetchRequest request)
        {
            return new StandaloneRequestContext(request, engine, ds2fc, Storage, id);
        }
    }

    public class DependentRequestContext : RequestContext
    {
        private readonly TaskCompletionSource<bool> evaluateUncertainty = new TaskCompletionSource<bool>();
        private readonly TaskCompletionSource<Array> maskReady = new TaskCompletionSource<Array>();

        public DependentRequestContext(IFetchRequest request, IFetchEngine engine, IDictionary<string, string> ds2fc, DataSet storage, ushort id)
            : base(request, engine, ds2fc, storage, id)
        { }

        public override Task<Array> GetMaskAsync(Array uncertainty)
        {
            if (uncertainty != null)
            {
                int[] dataShape = request.Domain.GetDataArrayShape();
                if (dataShape.Length != uncertainty.Rank)
                    throw new ArgumentException("Wrong rank of uncertainty array", "uncertainty");
                for (int i = 0; i < dataShape.Length; i++)
                    if (dataShape[i] != uncertainty.GetLength(i))
                        throw new ArgumentException("Wrong shape of uncertainty array", "uncertainty");
            }
            this.uncertainty = uncertainty;
            evaluateUncertainty.SetResult(uncertainty != null);
            return maskReady.Task;
        }

        public Task<bool> EvaluateUncertaintyTask
        {
            get { return evaluateUncertainty.Task; }
        }

        public void SetCanceled()
        {
            maskReady.SetCanceled();
        }

        /// <summary>
        /// fills maskReady with bool mask of needed elements according to referencing this.id field
        /// </summary>
        /// <param name="prov">null - all elements are needed</param>
        public void SetProvenance(Array prov)
        {
            if (prov == null)
                maskReady.SetResult(ArrayHelper.GetConstantArray<bool>(request.Domain.GetDataArrayShape(), true));

            Array mask = Array.CreateInstance(typeof(bool), request.Domain.GetDataArrayShape());

            GCHandle? provHandle = GCHandle.Alloc(prov, GCHandleType.Pinned);
            IntPtr provPtr = provHandle.Value.AddrOfPinnedObject();
            GCHandle? maskHandle = GCHandle.Alloc(mask, GCHandleType.Pinned);
            IntPtr maskPtr = maskHandle.Value.AddrOfPinnedObject();
            try
            {
                int len = prov.Length;
                unsafe
                {
                    ushort* p = (ushort*)provPtr;
                    bool* m = (bool*)maskPtr;
                    for (int i = 0; i < len; i++)
                        m[i] = p[i] == id;
                }
            }
            finally
            {
                provHandle.Value.Free();
                maskHandle.Value.Free();
            }
            maskReady.SetResult(mask);
        }

        public override IRequestContext CopyWithNewRequest(IFetchRequest request)
        {
            return new DependentRequestContext(request, engine, ds2fc, Storage, id);
        }
    }
}