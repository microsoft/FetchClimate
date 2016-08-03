using Microsoft.Research.Science.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Tests
{
    [TestClass]
    public class Etopo1Tests
    {       
        [TestMethod]
        [TestCategory("Local")]
        public async Task EtopoValuesTest()
        {
            var storage = TestDataStorageFactory.GetStorageContext(TestConstants.UriEtopo);
            ETOPO1DataSource.ETOPO1DataHandler gtopo = await ETOPO1DataSource.ETOPO1DataHandler.CreateAsync(storage);

            ITimeRegion tr = new TimeRegion().GetMonthlyTimeseries(firstMonth: 1, lastMonth: 1);
            IFetchDomain domain = FetchDomain.CreatePoints(
                new double[] { 61.55,61.55 }, // data index 9093
                new double[] { 328.45,-31.55 }, //data index 8907 (the same point, different lon notations)
                 tr);

            FetchRequest elevRequest = new FetchRequest("Elevation", domain);

            var handlerPrivate = new PrivateObject(gtopo, new PrivateType(typeof(DataHandlerFacade)));
            var aggregatorPrivate = new PrivateObject(handlerPrivate, "valuesAggregator");            

            Assert.AreEqual(-2441.0, (double)(await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(storage, elevRequest),null))).GetValue(0), 1e-9); //manual data comparision            
            Assert.AreEqual(-2441.0, (double)(await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(storage, elevRequest),null))).GetValue(1), 1e-9); //manual data comparision            
        }
    }
}
