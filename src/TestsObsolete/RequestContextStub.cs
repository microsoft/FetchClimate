using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Tests
{
    class RequestContextStub : IRequestContext
    {
        private IStorageContext storage;
        private Func<FetchRequest, Array> externalRequestsHandler;


        private RequestContextStub(IStorageContext storage, FetchRequest request, Func<FetchRequest, Array> externalRequestsHandler = null)
        {
            this.externalRequestsHandler = externalRequestsHandler;
            this.storage = storage;
            Request = request;
        }


        public static IRequestContext GetStub(IStorageContext storage, FetchRequest request, Func<FetchRequest, Array> externalRequestsHandler = null)
        {
            return new RequestContextStub(storage, request, externalRequestsHandler);
        }

        public FetchRequest Request
        {
            get;
            private set;
        }

        public Task<Array> GetMaskAsync(Array uncertainty)
        {
            throw new NotImplementedException();
        }

        public async Task<FetchResponse[]> FetchDataAsync(params FetchRequest[] requests)
        {
            if(externalRequestsHandler==null)
                throw new InvalidOperationException("Please pass externalRequestsHandler during stub creation");
            return requests.Select(r => new FetchResponse(r, externalRequestsHandler(r),null)).ToArray();
        }

        public DataStorageDefinition StorageDefinition
        {
            get { return storage.StorageDefinition; }
        }

        public Task<StorageResponse[]> GetDataAsync(params StorageRequest[] requests)
        {
            return storage.GetDataAsync(requests);
        }


        public IRequestContext CopyWithNewRequest(FetchRequest request)
        {
            return new RequestContextStub(storage,request);
        }
    }
}
