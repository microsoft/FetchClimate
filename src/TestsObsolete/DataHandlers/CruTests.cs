using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Research.Science.FetchClimate2.DataSources;
using Microsoft.Research.Science.Data;
using Microsoft.Research.Science.Data.Imperative;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Tests
{
    [TestClass]
    public class CruTests
    {
        //const string cruURI =  @"msds:nc?openMode=readOnly&file=D:\ClimateData\cru20.nc"

        double[] GenerateAxis(double start, double stop, double step)
        {
            double[] axis = new double[(int)Math.Floor((stop - start) / step) + 1];
            for (int i = 0; i < axis.Length; i++)
                axis[i] = start + step * i;
            return axis;
        }

        [TestMethod]
        [TestCategory("Local")]
        public async Task PointCubeDataTest()
        {
            var storage = TestDataStorageFactory.GetStorage(TestConstants.UriCru);
            CruCl20DataHandler cru = await CruCl20DataHandler.CreateAsync(storage);


            TimeRegion tr = new TimeRegion().GetMonthlyTimeseries(firstMonth: 2, lastMonth: 3);
            FetchDomain domain = FetchDomain.CreatePointGrid(
                GenerateAxis(67.25, 67.75, 0.25),
                GenerateAxis(179.25, 180.75, 0.25),
                 tr);
            FetchRequest request = new FetchRequest("tmp", domain);

            bool[, ,,,] mask = new bool[7, 3,1, 2,1];
            bool[] maskStreched = Enumerable.Repeat(true, 2 * 7 * 3).ToArray();
            Buffer.BlockCopy(maskStreched, 0, mask, 0, maskStreched.Length * sizeof(bool));

            var handlerPrivate = new PrivateObject(cru, new PrivateType(typeof(DataHandlerFacade)));            
            var aggregatorPrivate = new PrivateObject(handlerPrivate, "valuesAggregator");

            for (int i = 0; i < 2; i++)
            {
                Array effectiveMask = i == 0 ? null : mask;
                var res = await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(storage, request), null));

                //lon,lat,t
                double[,,] temps = (double[, ,])res;

                double precision = TestConstants.FloatPrecision; //float precision

                //manual data comparasion

                //lon,lat,t                                           //lat,lon,t
                Assert.AreEqual(-29.3, temps[0, 0, 0], precision); //corresponding varaible index [793,2155,1]
                Assert.AreEqual(-30.0, temps[0, 2, 0], precision); //corresponding varaible index [796,2155,1]
                Assert.AreEqual(-26.6, temps[4, 0, 0], precision); //corresponding varaible index [793,1,1]
                Assert.AreEqual(-26.8, temps[4, 2, 0], precision); //corresponding varaible index [796,1,1]

                Assert.AreEqual(-27.4, temps[0, 0, 1], precision); //corresponding varaible index [793,2155,2]
                Assert.AreEqual(-28.2, temps[0, 2, 1], precision); //corresponding varaible index [796,2155,2]
                Assert.AreEqual(-25.1, temps[4, 0, 1], precision); //corresponding varaible index [793,1,2]
                Assert.AreEqual(-25.2, temps[4, 2, 1], precision); //corresponding varaible index [796,1,2]
            }
        }        

        [TestMethod]
        [TestCategory("Local")]
        public async Task MonthlyMeansLandOnlyVariablesTest()
        {
            var storage = TestDataStorageFactory.GetStorage(TestConstants.UriCru);
            CruCl20DataHandler cru = await CruCl20DataHandler.CreateAsync(storage);

            TimeRegion tr = new TimeRegion().GetMonthlyTimeseries(firstMonth: 1, lastMonth: 1);
            FetchDomain oceanDomain = FetchDomain.CreateCells(
                new double[] { 22.0 },
                new double[] { -25.0 },
                new double[] { 23.0 },
                new double[] { -24.0 },
                 tr);

            FetchDomain landDomain = FetchDomain.CreateCells(
                new double[] { 24.0 },
                new double[] { -10.0 },
                new double[] { 25.0 },
                new double[] { -9.0 },
                 tr);

            FetchDomain landOceanMixDomain = FetchDomain.CreateCells(
                new double[] { 24.0 },
                new double[] { -16.0 },
                new double[] { 25.0 },
                new double[] { -14.0 },
                 tr);

            FetchRequest oceanPrecRequest = new FetchRequest("pre", oceanDomain);
            FetchRequest landPrecRequest = new FetchRequest("pre", landDomain);
            FetchRequest mixPrecRequest = new FetchRequest("pre", landOceanMixDomain);            

            var handlerPrivate = new PrivateObject(cru, new PrivateType(typeof(DataHandlerFacade)));
            var evaluatorPrivate = new PrivateObject(handlerPrivate, "uncertaintyEvaluator");
            var aggregatorPrivate = new PrivateObject(handlerPrivate, "valuesAggregator");

            double oceanUnc = (double)(await (Task<Array>)(evaluatorPrivate.Invoke("EvaluateAsync", RequestContextStub.GetStub(storage, oceanPrecRequest)))).GetValue(0);
            double landUnc = (double)(await (Task<Array>)(evaluatorPrivate.Invoke("EvaluateAsync", RequestContextStub.GetStub(storage, landPrecRequest)))).GetValue(0);
            double mixUnc = (double)(await (Task<Array>)(evaluatorPrivate.Invoke("EvaluateAsync", RequestContextStub.GetStub(storage,mixPrecRequest)))).GetValue(0);            
            
            Assert.IsTrue(double.IsNaN(oceanUnc));
            Assert.AreEqual(double.MaxValue,mixUnc);
            Assert.IsTrue(!double.IsNaN(landUnc));
            Assert.IsTrue(landUnc < double.MaxValue); //check variogram presence if this fails

            double oceanVal = (double)(await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(storage, oceanPrecRequest), null))).GetValue(0);
            double landVal = (double)(await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(storage, landPrecRequest), null))).GetValue(0);
            double mixVal = (double)(await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(storage, mixPrecRequest), null))).GetValue(0);
            
            Assert.IsTrue(double.IsNaN(oceanVal));
            Assert.IsTrue(!double.IsNaN(landVal));
            Assert.IsTrue(!double.IsNaN(mixVal));
            Assert.AreEqual(mixVal, landVal, TestConstants.DoublePrecision);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        [TestCategory("Local")]
        public async Task MonthlyMeansUnknownVarEvaluationRejectingTest()
        {
            var storage = TestDataStorageFactory.GetStorage(TestConstants.UriCru);
            CruCl20DataHandler cru = await CruCl20DataHandler.CreateAsync(storage);

            TimeRegion tr = new TimeRegion().GetMonthlyTimeseries(firstMonth: 1, lastMonth: 1);
            FetchDomain d = FetchDomain.CreateCells( //below the data
                new double[] { -10.0 },
                new double[] { -15.0 },
                new double[] { -8.0 },
                new double[] { -14.0 },
                 tr);
           
            FetchRequest r = new FetchRequest("fasdf342f34", d);

            var handlerPrivate = new PrivateObject(cru, new PrivateType(typeof(DataHandlerFacade)));
            var evaluatorPrivate = new PrivateObject(handlerPrivate, "uncertaintyEvaluator");

            var et = await (Task<Array>)(evaluatorPrivate.Invoke("EvaluateAsync", RequestContextStub.GetStub(storage, r))); 
        }


        [TestMethod]
        [TestCategory("Local")]
        public async Task MonthlyMeansOutOfDataTest()
        {
            var storage = TestDataStorageFactory.GetStorage(TestConstants.UriCru);
            CruCl20DataHandler cru = await CruCl20DataHandler.CreateAsync(storage);

            TimeRegion tr = new TimeRegion().GetMonthlyTimeseries(firstMonth: 1, lastMonth: 1);
            FetchDomain[] outOfDataDomain = new FetchDomain[2];

            outOfDataDomain[0] = FetchDomain.CreateCells( //below the data
                new double[] { -70.0 },
                new double[] { -25.0 },
                new double[] { -68.0 },
                new double[] { -24.0 },
                 tr);

            outOfDataDomain[1] = FetchDomain.CreateCells( //above the data
                new double[] { 85.0 },
                new double[] { -25.0 },
                new double[] { 87.0 },
                new double[] { -24.0 },
                 tr);

            var handlerPrivate = new PrivateObject(cru, new PrivateType(typeof(DataHandlerFacade)));
            var evaluatorPrivate = new PrivateObject(handlerPrivate, "uncertaintyEvaluator");
            var aggregatorPrivate = new PrivateObject(handlerPrivate, "valuesAggregator");

            for (int i = 0; i < 2; i++)
            {
                FetchRequest outOfDataRequest = new FetchRequest("pre", outOfDataDomain[i]);

                double unc = (double)(await (Task<Array>)(evaluatorPrivate.Invoke("EvaluateAsync", RequestContextStub.GetStub(storage, outOfDataRequest)))).GetValue(0);
                double val = (double)(await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(storage, outOfDataRequest), null))).GetValue(0);
                Assert.IsTrue(double.IsNaN(val));
                Assert.IsTrue(double.IsNaN(unc));                
            }


        }

        [TestMethod]
        [TestCategory("Local")]
        public async Task MonthlyMeansMissingValueOnWaterTest()
        {
            var storage = TestDataStorageFactory.GetStorage(TestConstants.UriCru);
            CruCl20DataHandler cru = await CruCl20DataHandler.CreateAsync(storage);

            TimeRegion tr = new TimeRegion();
            FetchDomain domain = FetchDomain.CreatePoints(
                new double[] { 0.5 },
                new double[] { 0.5 },
                 tr);
            FetchRequest request = new FetchRequest("tmp", domain);

            var handlerPrivate = new PrivateObject(cru, new PrivateType(typeof(DataHandlerFacade)));            
            var aggregatorPrivate = new PrivateObject(handlerPrivate, "valuesAggregator");

            for (int i = 0; i < 2; i++)
            {
                Array mask = i == 0 ? null : new bool[,] { { true } };
                var res = await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(storage, request), null));

                //lon,lat,t
                double[] temps = (double[])res;

                Assert.AreEqual(1, temps.Length);
                Assert.IsTrue(double.IsNaN(temps[0]));
            }


        }

        [TestMethod]
        [TestCategory("Local")]
        public async Task CruUncertaintyLandAndWaterTest()
        {
            var storage = TestDataStorageFactory.GetStorage(TestConstants.UriCru);
            CruCl20DataHandler cru = await CruCl20DataHandler.CreateAsync(storage);

            TimeRegion tr = new TimeRegion(firstYear: 1950, lastYear: 2010);
            FetchDomain domain = FetchDomain.CreateCells(
                //ocean region
                new double[] { 0.0 },
                new double[] { 0.0 },
                new double[] { 1.0 },
                new double[] { 1.0 },
                 tr);
            FetchRequest request = new FetchRequest("pre", domain);

            var handlerPrivate = new PrivateObject(cru, new PrivateType(typeof(DataHandlerFacade)));
            var evaluatorPrivate = new PrivateObject(handlerPrivate, "uncertaintyEvaluator");

            var res = await (Task<Array>)(evaluatorPrivate.Invoke("EvaluateAsync", RequestContextStub.GetStub(storage, request)));

            //i,t
            double[] prec = (double[])res;

            Assert.AreEqual(1, prec.Length);
            Assert.IsTrue(double.IsNaN(prec[0]));



            domain = FetchDomain.CreateCells(
                //land region
                new double[] { 10.0 },
                new double[] { 10.0 },
                new double[] { 11.0 },
                new double[] { 11.0 },
                 tr);
            request = new FetchRequest("pre", domain);
            res = await (Task<Array>)(evaluatorPrivate.Invoke("EvaluateAsync", RequestContextStub.GetStub(storage, request)));

            //i,t
            prec = (double[])res;

            Assert.AreEqual(1, prec.Length);
            Assert.IsTrue(!double.IsNaN(prec[0]));

        }

        [TestMethod]
        [TestCategory("Local")]
        public async Task CruAllVaraiblesValuesTest()
        {
            var storage = TestDataStorageFactory.GetStorage(TestConstants.UriCru);
            CruCl20DataHandler cru = await CruCl20DataHandler.CreateAsync(storage);

            TimeRegion tr = new TimeRegion().GetMonthlyTimeseries(firstMonth: 1, lastMonth: 1);
            FetchDomain domain = FetchDomain.CreatePoints(
                new double[] { 48.25 }, // data index 679
                new double[] { -100.25 }, //data index 478
                 tr);

            var handlerPrivate = new PrivateObject(cru, new PrivateType(typeof(DataHandlerFacade)));
            var aggregatorPrivate = new PrivateObject(handlerPrivate, "valuesAggregator");

            FetchRequest tmpRequest = new FetchRequest("tmp", domain);
            FetchRequest preRequest = new FetchRequest("pre", domain);
            FetchRequest wndRequest = new FetchRequest("wnd", domain);
            FetchRequest sunpRequest = new FetchRequest("sunp", domain);
            FetchRequest rehRequest = new FetchRequest("reh", domain);
            FetchRequest rd0Request = new FetchRequest("rd0", domain);
            FetchRequest frsRequest = new FetchRequest("frs", domain);
            FetchRequest dtrRequest = new FetchRequest("dtr", domain);

            Assert.AreEqual(4.9, (double)(await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(storage, wndRequest), null))).GetValue(0), TestConstants.FloatPrecision); //manual data comparision
            Assert.AreEqual(-15.4, (double)(await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(storage, tmpRequest), null))).GetValue(0), TestConstants.FloatPrecision);
            Assert.AreEqual(48.1, (double)(await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(storage, sunpRequest), null))).GetValue(0), TestConstants.FloatPrecision);
            Assert.AreEqual(74.5, (double)(await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(storage, rehRequest), null))).GetValue(0), TestConstants.FloatPrecision);
            Assert.AreEqual(7.6, (double)(await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(storage, rd0Request), null))).GetValue(0), TestConstants.FloatPrecision);
            Assert.AreEqual(14.0, (double)(await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(storage, preRequest), null))).GetValue(0), TestConstants.FloatPrecision);
            Assert.AreEqual(30.7, (double)(await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(storage, frsRequest), null))).GetValue(0), TestConstants.FloatPrecision);
            Assert.AreEqual(11.8, (double)(await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(storage, dtrRequest), null))).GetValue(0), TestConstants.FloatPrecision);
        }
    }
}
