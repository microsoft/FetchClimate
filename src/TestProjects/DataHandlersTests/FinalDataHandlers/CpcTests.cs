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
    public class CpcTests
    {
        [TestMethod]
        [TestCategory("Local")]
        public async Task CpcValuesTest()
        {
            var storage = TestDataStorageFactory.GetStorageContext(TestConstants.UriCpc);
            CPCDataSource.CpcDataHandler handler = await CPCDataSource.CpcDataHandler.CreateAsync(storage);

            ITimeRegion tr = new TimeRegion(firstYear:1949,lastYear:1949).GetMonthlyTimeseries(firstMonth: 4, lastMonth: 4);//data index 15
            IFetchDomain domain = FetchDomain.CreatePoints(
                new double[] { 31.75 }, //data index 116
                new double[] { 84.75 },//data index 171
                 tr);

            FetchRequest soilRequest = new FetchRequest("soilw", domain);

            var handlerPrivate = new PrivateObject(handler, new PrivateType(typeof(DataHandlerFacade)));
            var aggregatorPrivate = new PrivateObject(handlerPrivate, "valuesAggregator");

            Assert.AreEqual(101.925401325, (double)(await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(storage, soilRequest),null))).GetValue(0),TestConstants.DoublePrecision); //manual data comparision. data  -22079     
        }

        [TestMethod]
        [TestCategory("Local")]
        public async Task CpcMvTest()
        {
            var storage = TestDataStorageFactory.GetStorageContext(TestConstants.UriCpc);
            CPCDataSource.CpcDataHandler handler = await CPCDataSource.CpcDataHandler.CreateAsync(storage);

            ITimeRegion tr = new TimeRegion(firstYear: 1949, lastYear: 1949).GetMonthlyTimeseries(firstMonth: 4, lastMonth: 4);//data index 15
            IFetchDomain domain = FetchDomain.CreatePoints(
                new double[] {10.7 },
                new double[] { -148.4 },
                 tr);

            FetchRequest soilRequest = new FetchRequest("soilw", domain);

            var handlerPrivate = new PrivateObject(handler, new PrivateType(typeof(DataHandlerFacade)));
            var aggregatorPrivate = new PrivateObject(handlerPrivate, "valuesAggregator");

            Assert.IsTrue(double.IsNaN((double)(await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(storage, soilRequest),null))).GetValue(0)));     
        }    
    }
}
