using DataHandlersTests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Tests.DataHandlers.VirtualVariables
{
    class VirtVarRequestContextStub : IRequestContext
    {
        IFetchRequest request;        

        public VirtVarRequestContextStub(IFetchRequest request)
        {
            this.request = request;            
        }

        public IFetchRequest Request
        {
            get { return request; }
        }

        public async Task<Array> GetMaskAsync(Array uncertainty)
        {
            foreach (var item in uncertainty)
            {
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(double.IsNaN((double)item));
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue((double)item < double.MaxValue);    
            }

            bool[] ones = Enumerable.Repeat(true,uncertainty.Length).ToArray();
            Array result = Array.CreateInstance(typeof(bool), Enumerable.Range(0, uncertainty.Rank).Select(d => uncertainty.GetLength(d)).ToArray());
                Buffer.BlockCopy(ones,0,result,0,uncertainty.Length*sizeof(bool));
            return result;

        }

        static Array RepeatValuesIntoArray(Array reference, double toRepeat)
        {   
            double[] ones = Enumerable.Repeat(toRepeat,reference.Length).ToArray();
            Buffer.BlockCopy(ones, 0, reference, 0, reference.Length * sizeof(double));
            return reference;
        }

        public async Task<IFetchResponse[]> FetchDataAsync(params IFetchRequest[] requests)
        {
            IFetchResponse[] res = new IFetchResponse[requests.Length];
            double[] valuesToReturn = new double[] {3,5,7,11,13,17}; //prime numbers for future checks
            for (int i = 0; i < res.Length; i++)
            {
                var a = RepeatValuesIntoArray(Array.CreateInstance(typeof(double),requests[i].Domain.GetDataArrayShape()), valuesToReturn[i]);
                res[i] = (IFetchResponse)(new FetchResponse(requests[i], a, a));
            }
            return res;
        }

        public IDataStorageDefinition StorageDefinition
        {
            get { throw new NotImplementedException(); }
        }

        public Task<IStorageResponse[]> GetDataAsync(params IStorageRequest[] requests)
        {
            throw new NotImplementedException();
        }


        public IRequestContext CopyWithNewRequest(FetchRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<Array> GetDataAsync(string variableName, int[] origin = null, int[] stride = null, int[] shape = null)
        {
            throw new NotImplementedException();
        }
    }
}
