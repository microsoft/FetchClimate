using DataHandlersTests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Tests
{
    class RequestContextStub : IRequestContext
    {
        private IStorageContext storage;
        private Func<IFetchRequest, Array> externalRequestsHandler;


        private RequestContextStub(IStorageContext storage, IFetchRequest request, Func<IFetchRequest, Array> externalRequestsHandler = null)
        {
            this.externalRequestsHandler = externalRequestsHandler;
            this.storage = storage;
            Request = request;
        }


        public static IRequestContext GetStub(IStorageContext storage, IFetchRequest request, Func<IFetchRequest, Array> externalRequestsHandler = null)
        {
            return new RequestContextStub(storage, request, externalRequestsHandler);
        }

        public IFetchRequest Request
        {
            get;
            private set;
        }

        public async Task<Array> GetMaskAsync(Array uncertainty)
        {
            //Returns true for all elements
            int[] lens = Enumerable.Range(0, uncertainty.Rank).Select(i => uncertainty.GetLength(i)).ToArray();
            int totalLen = uncertainty.Length;
            Array allMask = Array.CreateInstance(typeof(bool), lens);

            GCHandle? maskHandle = GCHandle.Alloc(allMask, GCHandleType.Pinned);
            IntPtr maskPtr = maskHandle.Value.AddrOfPinnedObject();
            try
            {
                unsafe
                {
                    bool* m = (bool*)maskPtr;
                    for (int i = 0; i < totalLen; i++)
                    {
                        m[i] = true;
                    }
                }
            }
            finally
            {
                maskHandle.Value.Free();
            }
            return allMask;
        }

        public async Task<IFetchResponse[]> FetchDataAsync(params IFetchRequest[] requests)
        {
            if (externalRequestsHandler == null)
                throw new InvalidOperationException("Please pass externalRequestsHandler during stub creation");
            return requests.Select(r => new FetchResponse(r, externalRequestsHandler(r), null)).ToArray();
        }

        public IDataStorageDefinition StorageDefinition
        {
            get { return storage.StorageDefinition; }
        }

        public Task<IStorageResponse[]> GetDataAsync(params IStorageRequest[] requests)
        {
            return storage.GetDataAsync(requests);
        }


        public IRequestContext CopyWithNewRequest(FetchRequest request)
        {
            return new RequestContextStub(storage, request);
        }

        public Task<Array> GetDataAsync(string variableName, int[] origin = null, int[] stride = null, int[] shape = null)
        {
            return storage.GetDataAsync(variableName, origin, stride, shape);
        }
    }

    /// <summary>
    /// Embeded continuous time intervals
    /// </summary>
    public struct TimeSegment : ITimeSegment
    {
        public TimeSegment(int firstYear, int lastYear, int firstDay, int lastDay, int startHour, int stopHour)
            : this()
        {
            FirstYear = firstYear;
            LastYear = lastYear;
            FirstDay = firstDay;
            LastDay = lastDay;
            StartHour = startHour;
            StopHour = stopHour;
        }

        /// <summary>
        /// The first year of years enumeration
        /// </summary>
        public int FirstYear { get; private set; }
        /// <summary>
        /// The last year of years enumeration (included bounds)
        /// </summary>
        public int LastYear { get; private set; }

        /// <summary>
        /// The first day of days enumeration over years
        /// </summary>
        public int FirstDay { get; private set; }

        /// <summary>
        /// The last day of days enumeration (included bounds) over years
        /// </summary>
        public int LastDay { get; private set; }

        /// <summary>
        /// The start bound of hours interval inside days
        /// </summary>
        public int StartHour { get; private set; }

        /// <summary>
        /// The end bound of hours interval inside days
        /// </summary>
        public int StopHour { get; private set; }

        public TimeSegment MemberwiseClone()
        {
            return (TimeSegment)base.MemberwiseClone();
        }
    }
}
