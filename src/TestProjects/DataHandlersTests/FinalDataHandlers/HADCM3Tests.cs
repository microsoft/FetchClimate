using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Microsoft.Research.Science.FetchClimate2.HADCM3DataSource;

namespace Microsoft.Research.Science.FetchClimate2.Tests.DataHandlers
{
    [TestClass]
    public class HADCM3Tests
    {
        [TestMethod]
        [TestCategory("Local")]        
        public async Task TestTemperatureValue()
        {
            var storage = TestDataStorageFactory.GetStorageContext(TestConstants.UriHADCM3_sra_tas);
            HADCM3DataHandler regularHandler = await HADCM3DataHandler.CreateAsync(storage);

            TimeRegion tr = new TimeRegion(firstYear: 2001, lastYear: 2001, firstDay: 91, lastDay: 120, startHour: 0, stopHour: 24); //index 15 ; april 2001
            IFetchDomain tmpDomain = FetchDomain.CreatePoints(
                new double[] { -75.0,-72.5 }, // data index 6,7
                new double[] { 90.0,105.0 }, //data index 24,28
                 tr);

            FetchRequest tmpRequest = new FetchRequest("tas", tmpDomain);

            var handlerPrivate = new PrivateObject(regularHandler, new PrivateType(typeof(DataHandlerFacade)));            
            var aggregatorPrivate = new PrivateObject(handlerPrivate, "valuesAggregator");

            var res = await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(storage, tmpRequest), null));
            Assert.AreEqual(-64.256323, (double)res.GetValue(0), TestConstants.FloatPrecision); //manual data comparision           
            Assert.AreEqual(-58.911108, (double)res.GetValue(1), TestConstants.FloatPrecision); //manual data comparision           
        }

        [TestMethod]
        [TestCategory("Local")]
        public async Task TestOutOfDataNan()
        {
            var storage = TestDataStorageFactory.GetStorageContext(TestConstants.UriHADCM3_sra_tas);
            HADCM3DataHandler regularHandler = await HADCM3DataHandler.CreateAsync(storage);

            TimeRegion tr = new TimeRegion(firstYear: 2301, lastYear: 2301, firstDay: 91, lastDay: 120, startHour: 0, stopHour: 24); //index 15 ; april out of data
            IFetchDomain tmpDomain = FetchDomain.CreatePoints(
                new double[] { -75.0}, // data index 6,7                
                new double[] { 90.0 },
                 tr);

            FetchRequest tmpRequest = new FetchRequest("tas", tmpDomain);

            var handlerPrivate = new PrivateObject(regularHandler, new PrivateType(typeof(DataHandlerFacade)));
            var evaluatorPrivate = new PrivateObject(handlerPrivate, "uncertaintyEvaluator");
            var aggregatorPrivate = new PrivateObject(handlerPrivate, "valuesAggregator");

            var res = await (Task<Array>)(evaluatorPrivate.Invoke("EvaluateAsync", RequestContextStub.GetStub(storage, tmpRequest)));
            Assert.IsTrue(double.IsNaN((double)res.GetValue(0))); //uncertatinty is nan            
            res = await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(storage, tmpRequest), null));
            Assert.IsTrue(double.IsNaN((double)res.GetValue(0))); // and the value is nan
            
        }

        [TestMethod]
        [TestCategory("Local")]
        public async Task TestDataCoveredNotNan()
        {
            var storage = TestDataStorageFactory.GetStorageContext(TestConstants.UriHADCM3_sra_tas);
            HADCM3DataHandler regularHandler = await HADCM3DataHandler.CreateAsync(storage);

            TimeRegion tr = new TimeRegion(firstYear: 2010, lastYear: 2015, firstDay: 91, lastDay: 120, startHour: 0, stopHour: 24); //index 15 ; april 2001
            IFetchDomain tmpDomain = FetchDomain.CreatePoints(
                new double[] { -75.0 }, // data index 6,7                
                new double[] { 90.0 },
                 tr);

            FetchRequest tmpRequest = new FetchRequest("tas", tmpDomain);

            var handlerPrivate = new PrivateObject(regularHandler, new PrivateType(typeof(DataHandlerFacade)));
            var evaluatorPrivate = new PrivateObject(handlerPrivate, "uncertaintyEvaluator");
            var aggregatorPrivate = new PrivateObject(handlerPrivate, "valuesAggregator");

            var res = await(Task<Array>)(evaluatorPrivate.Invoke("EvaluateAsync", RequestContextStub.GetStub(storage, tmpRequest)));
            Assert.IsTrue(!double.IsNaN((double)res.GetValue(0))); //uncertatinty is not nan       
            res = await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(storage, tmpRequest), null));
            Assert.IsTrue(!double.IsNaN((double)res.GetValue(0))); // and the value is not nan
        }

        [TestMethod]
        [TestCategory("Local")]
        public async Task TestUnknownUncertatintyMaxValue()
        {
            var storage = TestDataStorageFactory.GetStorageContext(TestConstants.UriHADCM3_sra_tas);
            HADCM3DataHandler regularHandler = await HADCM3DataHandler.CreateAsync(storage);

            TimeRegion tr = new TimeRegion(firstYear: 2010, lastYear: 2015, firstDay: 91, lastDay: 120, startHour: 0, stopHour: 24); //index 15 ; april 2001
            IFetchDomain tmpDomain = FetchDomain.CreatePoints(
                new double[] { -75.0 }, // data index 6,7                
                new double[] { 90.0 },
                 tr);

            FetchRequest tmpRequest = new FetchRequest("tas", tmpDomain);

            var handlerPrivate = new PrivateObject(regularHandler, new PrivateType(typeof(DataHandlerFacade)));
            var evaluatorPrivate = new PrivateObject(handlerPrivate, "uncertaintyEvaluator");

            var res = await (Task<Array>)(evaluatorPrivate.Invoke("EvaluateAsync", RequestContextStub.GetStub(storage, tmpRequest)));
            Assert.AreEqual(double.MaxValue,((double)res.GetValue(0)));
        }
    }
}