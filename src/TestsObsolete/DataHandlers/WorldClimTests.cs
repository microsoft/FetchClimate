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
    public class WorldClimTests
    {
        [TestMethod]
        [TestCategory("Local")]
        public async Task WorldClimAllVaraiblesValuesTest()
        {
            var storage = TestDataStorageFactory.GetStorage(TestConstants.UriWorldClim);
            WorldClim14DataSource wc = await WorldClim14DataSource.CreateAsync(storage);

            TimeRegion tr = new TimeRegion().GetMonthlyTimeseries(firstMonth: 1, lastMonth: 1);
            FetchDomain domain = FetchDomain.CreatePoints(
                new double[] { 48.25 }, // data index 12990
                new double[] { -100.25 }, //data index 9570
                 tr);

            FetchRequest tmpRequest = new FetchRequest("tmean", domain);
            FetchRequest preRequest = new FetchRequest("prec", domain);

            var handlerPrivate = new PrivateObject(wc, new PrivateType(typeof(DataHandlerFacade)));            
            var aggregatorPrivate = new PrivateObject(handlerPrivate, "valuesAggregator");

            Assert.AreEqual(-14.9, (double)(await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(storage, tmpRequest), null))).GetValue(0), 1e-2); //manual data comparision
            Assert.AreEqual(13, (double)(await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(storage, preRequest), null))).GetValue(0), 1e-1);            
        }

        [TestMethod]
        [Timeout(60000)]
        [TestCategory("Local")]
        public async Task ClusterizationTest()
        {
            var storage = TestDataStorageFactory.GetStorage(TestConstants.UriWorldClim);
            WorldClim14DataSource worldClim = await WorldClim14DataSource.CreateAsync(storage);

            TimeRegion tr = new TimeRegion(firstYear: 1950, lastYear: 2010,firstDay:1, lastDay:31);
            FetchDomain domain = FetchDomain.CreatePoints(
                //ocean region
                new double[] { 53.1,53.3,-22.8,53.2 }, //data indeces 13572,13596,13584
                new double[] {  -116.0,-115.975,124.8,-115.9},    //data indeces 7680,7683,36576,7692
                 tr
                );
            FetchRequest request = new FetchRequest("tmean", domain);

            var handlerPrivate = new PrivateObject(worldClim, new PrivateType(typeof(DataHandlerFacade)));
            var aggregatorPrivate = new PrivateObject(handlerPrivate, "valuesAggregator");

            var res = await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(storage, request), null));
            Assert.AreEqual(-13.3, (double)res.GetValue(0), 1e-2);
            Assert.AreEqual(-13.3, (double)res.GetValue(1), 1e-2);            
            Assert.AreEqual(32.6, (double)res.GetValue(2), 1e-2);
            Assert.AreEqual(-13.3, (double)res.GetValue(3), 1e-2);
        }
    }
}
