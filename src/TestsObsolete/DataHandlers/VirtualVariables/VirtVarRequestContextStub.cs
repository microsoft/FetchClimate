using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Tests.DataHandlers.VirtualVariables
{
    class VirtVarRequestContextStub : IRequestContext
    {
        FetchRequest request;
        double referenceUncrtatinty;

        public VirtVarRequestContextStub(FetchRequest request,double referenceUncrtatinty)
        {
            this.request = request;
            this.referenceUncrtatinty = referenceUncrtatinty;
        }

        public FetchRequest Request
        {
            get { return request; }
        }

        public async Task<Array> GetMaskAsync(Array uncertainty)
        {
            foreach (var item in uncertainty)
            {
                if(double.IsNaN(referenceUncrtatinty))
                    Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(double.IsNaN((double)item));
                else
                    Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(referenceUncrtatinty, (double)item, TestConstants.DoublePrecision);
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

        public async Task<FetchResponse[]> FetchDataAsync(params FetchRequest[] requests)
        {
            FetchResponse[] res = new FetchResponse[requests.Length];
            double[] valuesToReturn = new double[] {3,5,7,11,13,17}; //prime numbers for future checks
            for (int i = 0; i < res.Length; i++)
            {
                var a = RepeatValuesIntoArray(Array.CreateInstance(typeof(double),requests[i].Domain.GetDataArrayShape()), valuesToReturn[i]);
                res[i] = new FetchResponse(requests[i], a, a);
            }
            return res;
        }

        public DataStorageDefinition StorageDefinition
        {
            get { throw new NotImplementedException(); }
        }

        public Task<StorageResponse[]> GetDataAsync(params StorageRequest[] requests)
        {
            throw new NotImplementedException();
        }


        public IRequestContext CopyWithNewRequest(FetchRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
