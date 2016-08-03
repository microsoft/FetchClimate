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
    public class Gtopo30Tests
    {
        [TestMethod]
        [TestCategory("Local")]
        public async Task GtopoValuesTest()
        {
            var storage = TestDataStorageFactory.GetStorageContext(TestConstants.UriGtopo);
            GTOPO30DataSource.GTOPO30DataHandler gtopo = await GTOPO30DataSource.GTOPO30DataHandler.CreateAsync(storage);

            ITimeRegion tr = new TimeRegion().GetMonthlyTimeseries(firstMonth: 1, lastMonth: 1);
            IFetchDomain domain = FetchDomain.CreatePoints(
                new double[] { 55.7125 }, // data index 17485
                new double[] { 37.5125 }, //data index 26101
                 tr);

            FetchRequest elevRequest = new FetchRequest("elevation", domain);

            var handlerPrivate = new PrivateObject(gtopo, new PrivateType(typeof(DataHandlerFacade)));
            var aggregatorPrivate = new PrivateObject(handlerPrivate, "valuesAggregator");            

            Assert.AreEqual(188.0, (double)(await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(storage, elevRequest),null))).GetValue(0)); //manual data comparision            
        }        
    }
}
