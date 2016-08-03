using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Research.Science.FetchClimate2.DataSources;
using Microsoft.Research.Science.Data;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Tests.DataSources
{
    [TestClass]
    public class NcepReanalysisTests
    {
        [TestMethod]
        [TestCategory("Local")]
        public async Task CellSetDataTest()
        {
            var storage = TestDataStorageFactory.GetStorageContext(TestConstants.UriReanalysisRegular);
            NCEPReanalysisRegularGridDataHandler reanalysis = await NCEPReanalysisRegularGridDataHandler.CreateAsync(storage);

            ITimeRegion tr = new TimeRegion(firstYear: 1980, lastYear: 1980, firstDay: 2, lastDay: 3, startHour: 0, stopHour: 0).
                GetSeasonlyTimeseries(firstDay: 2, lastDay: 3, isIntervalTimeseries: false);//indeces 46756,46760; hours sice 1-1-1 00:00:00   =  17347560 , 17347524
            IFetchDomain domain = FetchDomain.CreateCells(
                //ocean region
                new double[] { 67.5, 7.5, 75.0, 0.0 }, //indeces 9,33,6,36
                new double[] { 75.0, 7.5, 107.5, 52.5 }, //indeces 30,3,43,21
                new double[] { 67.5, 7.5, 75.0, 0.0 }, //indeces 9,33,6,36
                new double[] { 75.0, 7.5, 107.5, 52.5 }, //indeces 30,3,43,21
                tr);
            FetchRequest request = new FetchRequest("air", domain);

            bool[, ] mask = new bool[4, 2];
            bool[] smask = System.Linq.Enumerable.Repeat(true, 8).ToArray();
            Buffer.BlockCopy(smask, 0, mask, 0, 8 * sizeof(bool));

            var handlerPrivate = new PrivateObject(reanalysis, new PrivateType(typeof(DataHandlerFacade)));
            var aggregatorPrivate = new PrivateObject(handlerPrivate, "valuesAggregator");

            for (int i = 0; i < 2; i++)
            {
                bool[, ] effectiveMask = i == 0 ? null : mask;
                var result = await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(storage, request),effectiveMask));                 

                //i,t
                double[, ] temps = (double[, ])result;

                Assert.AreEqual(8, temps.Length);
                Assert.AreEqual(4, temps.GetLength(0));                
                Assert.AreEqual(2, temps.GetLength(1));                

                Assert.AreEqual(-18.05, temps[0, 0], TestConstants.FloatPrecision); //reference data value (unscaled) is -25771
                Assert.AreEqual(26.95, temps[1, 0], TestConstants.FloatPrecision); //reference data value (unscaled) is -21271
                Assert.AreEqual(-25.05, temps[2, 0], TestConstants.FloatPrecision); //reference data value (unscaled) is -26471
                Assert.AreEqual(26.15, temps[3, 0], TestConstants.FloatPrecision); //reference data value (unscaled) is -21351

                Assert.AreEqual(-19.25, temps[0, 1], TestConstants.FloatPrecision); //reference data value (unscaled) is -25891
                Assert.AreEqual(25.05, temps[1, 1], TestConstants.FloatPrecision); //reference data value (unscaled) is -21461
                Assert.AreEqual(-28.55, temps[2, 1], TestConstants.FloatPrecision); //reference data value (unscaled) is -26821
                Assert.AreEqual(25.65, temps[3, 1], TestConstants.FloatPrecision); //reference data value (unscaled) is -21401             
            }
        }

        [TestMethod]
        [TestCategory("Local")]
        public async Task PointSetDataTest()
        {
            var storage = TestDataStorageFactory.GetStorageContext(TestConstants.UriReanalysisRegular);
            NCEPReanalysisRegularGridDataHandler reanalysis = await NCEPReanalysisRegularGridDataHandler.CreateAsync(storage);

            ITimeRegion tr = new TimeRegion(firstYear: 1980, lastYear: 1980, firstDay: 2, lastDay: 3, startHour: 0, stopHour: 0).
                GetSeasonlyTimeseries(firstDay: 2, lastDay: 3, isIntervalTimeseries: false); //indeces 46756,46760; hours sice 1-1-1 00:00:00   =  17347560 , 17347524
            IFetchDomain domain = FetchDomain.CreatePoints(
                //ocean region
                new double[] { 67.5, 7.5, 75.0, 0.0 }, //indeces 9,33,6,36
                new double[] { 75.0, 7.5, 107.5, 52.5 }, //indeces 30,3,43,21
                tr);
            FetchRequest request = new FetchRequest("air", domain);

            bool[, , ,] mask = new bool[4, 1, 2, 1];
            bool[] smask = System.Linq.Enumerable.Repeat(true, 8).ToArray();
            Buffer.BlockCopy(smask, 0, mask, 0, 8 * sizeof(bool));

            var handlerPrivate = new PrivateObject(reanalysis, new PrivateType(typeof(DataHandlerFacade)));
            var aggregatorPrivate = new PrivateObject(handlerPrivate, "valuesAggregator");

            for (int i = 0; i < 2; i++)
            {
                bool[, , ,] effectiveMask = i == 0 ? null : mask;
                var result = await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(storage, request), effectiveMask));                                 

                //i,t
                double[, ] temps = (double[,])result;

                Assert.AreEqual(8, temps.Length);
                Assert.AreEqual(4, temps.GetLength(0));                
                Assert.AreEqual(2, temps.GetLength(1));                

                Assert.AreEqual(-18.05, temps[0, 0], TestConstants.FloatPrecision); //reference data value (unscaled) is -25771
                Assert.AreEqual(26.95, temps[1, 0], TestConstants.FloatPrecision); //reference data value (unscaled) is -21271
                Assert.AreEqual(-25.05, temps[2, 0], TestConstants.FloatPrecision); //reference data value (unscaled) is -26471
                Assert.AreEqual(26.15, temps[3, 0], TestConstants.FloatPrecision); //reference data value (unscaled) is -21351

                Assert.AreEqual(-19.25, temps[0, 1], TestConstants.FloatPrecision); //reference data value (unscaled) is -25891
                Assert.AreEqual(25.05, temps[1, 1], TestConstants.FloatPrecision); //reference data value (unscaled) is -21461
                Assert.AreEqual(-28.55, temps[2, 1], TestConstants.FloatPrecision); //reference data value (unscaled) is -26821
                Assert.AreEqual(25.65, temps[3, 1], TestConstants.FloatPrecision); //reference data value (unscaled) is -21401             
            }
        }

        [TestMethod]
        [TestCategory("Local")]
        public async Task ReanalysisAllVaraiblesValuesTest()
        {
            var regularStorage = TestDataStorageFactory.GetStorageContext(TestConstants.UriReanalysisRegular);
            NCEPReanalysisRegularGridDataHandler regularHandler = await NCEPReanalysisRegularGridDataHandler.CreateAsync(regularStorage);

            ITimeRegion tr = new TimeRegion(firstYear: 1980, lastYear: 1980, firstDay: 1, lastDay: 1, startHour: 0, stopHour: 0); //index 46752 ; hours sice 1-1-1 00:00:00   =  17347536
            IFetchDomain tmpDomain = FetchDomain.CreatePoints(
                new double[] { -37.5 }, // data index 51
                new double[] { 137.5 }, //data index 55
                 tr);

            FetchRequest tmpRequest = new FetchRequest("air", tmpDomain);

            var handlerPrivate1 = new PrivateObject(regularHandler, new PrivateType(typeof(DataHandlerFacade)));
            var aggregatorPrivate1 = new PrivateObject(handlerPrivate1, "valuesAggregator");


            Assert.AreEqual(11.45, (double)(await (Task<Array>)(aggregatorPrivate1.Invoke("AggregateAsync", RequestContextStub.GetStub(regularStorage, tmpRequest),null))).GetValue(0), TestConstants.FloatPrecision); //manual data comparision           


            var gaussStorage = TestDataStorageFactory.GetStorageContext(TestConstants.UriReanalysisGauss);
            NCEPReanalysisGaussT62GridDataHandler gaussHandler = await NCEPReanalysisGaussT62GridDataHandler.CreateAsync(gaussStorage);

            ITimeRegion tr2 = new TimeRegion(firstYear: 1980, lastYear: 1980, firstDay: 1, lastDay: 1, startHour: 0, stopHour: 6); //index 46752 ; hours sice 1-1-1 00:00:00   =  17347536
            IFetchDomain preDomain = FetchDomain.CreatePoints(
                new double[] { 2.8571 }, // data index 45
                new double[] { 178.125 }, //data index 95
                 tr2);

            FetchRequest preRequest = new FetchRequest("prate", preDomain);

            var handlerPrivate2 = new PrivateObject(gaussHandler, new PrivateType(typeof(DataHandlerFacade)));
            var aggregatorPrivate2 = new PrivateObject(handlerPrivate2, "valuesAggregator");

            //raw data value at [45,95,46752] is   -32355
            //scale_factor = 1E-07
            //add_offset = 0.0032765
            //aditional_scaling = 2592000            
            Assert.AreEqual(106.272, (double)(await (Task<Array>)(aggregatorPrivate2.Invoke("AggregateAsync", RequestContextStub.GetStub(gaussStorage, preRequest),null))).GetValue(0), 1e-3); //manual data comparision. 1e-3 precision as Data and scale_factor/add_offset are single
        }

        [TestMethod]
        [TestCategory("Local")]        
        public async Task Bug1335() //reanalysis returns 11.7 uncertainty for future climate instead of NAN
        {
            var regularStorage = TestDataStorageFactory.GetStorageContext(TestConstants.UriReanalysisRegular);
            NCEPReanalysisRegularGridDataHandler regularHandler = await NCEPReanalysisRegularGridDataHandler.CreateAsync(regularStorage);

            ITimeRegion tr = new TimeRegion().GetMonthlyTimeseries().GetYearlyTimeseries(firstYear: 1951, lastYear: 2200);
            var request = new FetchRequest(
                "air",
                FetchDomain.CreatePoints(
                new double[] { 55.4 },
                new double[] { 37.5 },
                    tr));

            var handlerPrivate = new PrivateObject(regularHandler, new PrivateType(typeof(DataHandlerFacade)));
            var evaluatorPrivate = new PrivateObject(handlerPrivate, "uncertaintyEvaluator");

            Array dataArray = await (Task<Array>)(evaluatorPrivate.Invoke("EvaluateAsync", RequestContextStub.GetStub(regularStorage, request)));
            Assert.IsTrue(!double.IsNaN((double)dataArray.GetValue(0, 0, 0)));
            Assert.IsTrue(double.IsNaN((double)dataArray.GetValue(0, 150,0)));
            Assert.IsTrue(double.IsNaN((double)dataArray.GetValue(0, 200, 0)));
        }

        [TestMethod]
        [TestCategory("Local")]
        public async Task ReanalysisOutOfDataTest()
        {
            var storage = TestDataStorageFactory.GetStorageContext(TestConstants.UriReanalysisRegular);
            NCEPReanalysisRegularGridDataHandler regularHandler = await NCEPReanalysisRegularGridDataHandler.CreateAsync(storage);

            ITimeRegion firstDataYear = new TimeRegion(firstYear: 1245, lastYear: 1245);
            IFetchDomain dataDomain = FetchDomain.CreatePoints(
                new double[] { -37.5 }, // data index 51
                new double[] { 137.5 }, //data index 55
                 firstDataYear);            

            FetchRequest dataRequest = new FetchRequest("air", dataDomain);            

            var handlerPrivate = new PrivateObject(regularHandler, new PrivateType(typeof(DataHandlerFacade)));
            var evaluatorPrivate = new PrivateObject(handlerPrivate, "uncertaintyEvaluator");
            var aggregatorPrivate = new PrivateObject(handlerPrivate, "valuesAggregator");

            Array dataArray = await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(storage, dataRequest), null)); ;
            Array dataUncertainty = await (Task<Array>)(evaluatorPrivate.Invoke("EvaluateAsync", RequestContextStub.GetStub(storage, dataRequest)));            

            Assert.AreEqual(1, dataArray.Length);
            Assert.AreEqual(1, dataUncertainty.Length);

            Assert.IsTrue(double.IsNaN((double)dataArray.GetValue(0)));
            Assert.IsTrue(double.IsNaN((double)dataUncertainty.GetValue(0)));            
        }

        [TestMethod]
        [TestCategory("Local")]
        public async Task ReanalysisDataTest()
        {
            var storage = TestDataStorageFactory.GetStorageContext(TestConstants.UriReanalysisRegular);
            NCEPReanalysisRegularGridDataHandler regularHandler = await NCEPReanalysisRegularGridDataHandler.CreateAsync(storage);

            ITimeRegion firstDataYear = new TimeRegion(firstYear: 1948, lastYear: 1948);
            IFetchDomain dataDomain = FetchDomain.CreatePoints(
                new double[] { -37.5 }, // data index 51
                new double[] { 137.5 }, //data index 55
                 firstDataYear);

            ITimeRegion dataSubYear = new TimeRegion(firstYear: 1948, lastYear: 1948, startHour: 0, stopHour: 1);
            IFetchDomain dataSubYearDomain = FetchDomain.CreatePoints(
                new double[] { -37.5 }, // data index 51
                new double[] { 137.5 }, //data index 55
                 dataSubYear);
            

            FetchRequest dataRequest = new FetchRequest("air", dataDomain);
            FetchRequest subDataRequest = new FetchRequest("air", dataSubYearDomain);

            var handlerPrivate = new PrivateObject(regularHandler, new PrivateType(typeof(DataHandlerFacade)));
            var evaluatorPrivate = new PrivateObject(handlerPrivate, "uncertaintyEvaluator");
            var aggregatorPrivate = new PrivateObject(handlerPrivate, "valuesAggregator");
            
            Array dataArray = await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(storage, dataRequest),null));;
            Array dataUncertainty = await (Task<Array>)(evaluatorPrivate.Invoke("EvaluateAsync", RequestContextStub.GetStub(storage, dataRequest)));

            Array subDataArray = await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(storage, subDataRequest), null));

            Assert.AreEqual(1, dataArray.Length);

            Assert.IsTrue(!double.IsNaN((double)dataUncertainty.GetValue(0)));
            Assert.IsTrue((double)dataUncertainty.GetValue(0)<double.MaxValue);

            Assert.IsTrue(!double.IsNaN((double)subDataArray.GetValue(0)));
            Assert.AreNotEqual(double.MaxValue,(double)subDataArray.GetValue(0));            
        }

        [TestMethod]
        [TestCategory("Local")]        
        public async Task ReanalysisGrowingUncertainty()
        {
            var storage = TestDataStorageFactory.GetStorageContext(TestConstants.UriReanalysisRegular);
            NCEPReanalysisRegularGridDataHandler regularHandler = await NCEPReanalysisRegularGridDataHandler.CreateAsync(storage);

            ITimeRegion tr = new TimeRegion(firstYear: 1961, lastYear: 1961,firstDay:1,lastDay:1,startHour:0,stopHour:0);
            IFetchDomain dataDenseDomain = FetchDomain.CreatePoints(
                new double[] { 76.25 },
                new double[] { 31.25 }, tr);//more dense nodes

            IFetchDomain dataSparseDomain = FetchDomain.CreatePoints(
                new double[] { 1.25 },
                new double[] { 31.25 }, tr);//more rare nodes



            FetchRequest dataDenseRequest = new FetchRequest("air", dataDenseDomain);
            FetchRequest dataSparseRequest = new FetchRequest("air", dataSparseDomain);

            var handlerPrivate = new PrivateObject(regularHandler, new PrivateType(typeof(DataHandlerFacade)));
            var evaluatorPrivate = new PrivateObject(handlerPrivate, "uncertaintyEvaluator");

            Array dataDenseUncertainty = await (Task<Array>)(evaluatorPrivate.Invoke("EvaluateAsync", RequestContextStub.GetStub(storage, dataDenseRequest)));
            Array dataSparseUncertainty = await (Task<Array>)(evaluatorPrivate.Invoke("EvaluateAsync", RequestContextStub.GetStub(storage, dataSparseRequest)));
            Assert.IsFalse(double.IsNaN((double)dataDenseUncertainty.GetValue(0)));
            Assert.IsFalse(double.IsNaN((double)dataSparseUncertainty.GetValue(0)));
            Assert.IsTrue(((double)dataDenseUncertainty.GetValue(0)) < double.MaxValue);
            Assert.IsTrue(((double)dataSparseUncertainty.GetValue(0)) < double.MaxValue);
            Assert.IsTrue(((double)dataDenseUncertainty.GetValue(0)) >= 0.0);
            Assert.IsTrue(((double)dataSparseUncertainty.GetValue(0)) >= 0.0);
            Assert.IsTrue(((double)dataDenseUncertainty.GetValue(0))<((double)dataSparseUncertainty.GetValue(0)));

        }

        [TestMethod]
        [TestCategory("Local")]
        public async Task ReanslysisUncertatintyTest()
        {
            var storage = TestDataStorageFactory.GetStorageContext(TestConstants.UriReanalysisRegular);
            NCEPReanalysisRegularGridDataHandler regularHandler = await NCEPReanalysisRegularGridDataHandler.CreateAsync(storage);

            ITimeRegion tr = new TimeRegion(firstYear: 1961, lastYear: 1990);
            IFetchDomain dataDomain1 = FetchDomain.CreatePoints(
                new double[] { 37.5 }, 
                new double[] { 30.0 },tr);

            IFetchDomain dataDomain2 = FetchDomain.CreatePoints(
                new double[] { 37.5 },
                new double[] { -30.0 }, tr);



            FetchRequest dataRequest1 = new FetchRequest("air", dataDomain1);
            FetchRequest dataRequest2 = new FetchRequest("air", dataDomain2);

            var handlerPrivate = new PrivateObject(regularHandler, new PrivateType(typeof(DataHandlerFacade)));
            var evaluatorPrivate = new PrivateObject(handlerPrivate, "uncertaintyEvaluator");

            Array dataUncertainty1 = await (Task<Array>)(evaluatorPrivate.Invoke("EvaluateAsync", RequestContextStub.GetStub(storage, dataRequest1)));
            Array dataUncertainty2 = await (Task<Array>)(evaluatorPrivate.Invoke("EvaluateAsync", RequestContextStub.GetStub(storage, dataRequest2)));
            Assert.IsFalse(double.IsNaN((double)dataUncertainty1.GetValue(0)));
            Assert.IsFalse(double.IsNaN((double)dataUncertainty2.GetValue(0)));
            Assert.IsTrue(((double)dataUncertainty1.GetValue(0))<double.MaxValue);
            Assert.IsTrue(((double)dataUncertainty2.GetValue(0))<double.MaxValue);
            Assert.IsTrue(((double)dataUncertainty1.GetValue(0))>= 0.0);
            Assert.IsTrue(((double)dataUncertainty2.GetValue(0))>= 0.0);
        }

        //[TestMethod]
        //public void ReanslysisSubDayIntervalsTest()
        //{
        //    var regularStorage = TestDataStorageFactory.GetStorage(TestConstants.UriReanalysisRegular);
        //    NCEPReanalysisRegularGridDataHandler regularHandler = new NCEPReanalysisRegularGridDataHandler(regularStorage);

        //    TimeRegion dataYear = TimeRegionFactory.SingleTimeRegion(firstYear: 1950, lastYear: 1950);
        //    FetchDomain yearDomain = FetchDomain.CreatePoints(
        //        new double[] { -37.5 }, // data index 51
        //        new double[] { 137.5 }, //data index 55
        //         dataYear);

        //    TimeRegion[] separateHoursTR = new TimeRegion[23];
        //    FetchDomain[] separateHoursDomains = new FetchDomain[23];
        //    double[] means = new double[23];
        //    for (int i = 0; i < 23; i++)
        //    {
        //        separateHoursTR[i] = TimeRegionFactory.SingleTimeRegion(firstYear: 1950, lastYear: 1950,startHour:i,stopHour:i+1);
        //        separateHoursDomains[i] = FetchDomain.CreatePoints(
        //        new double[] { -37.5 }, // data index 51
        //        new double[] { 137.5 }, //data index 55
        //         separateHoursTR[i]);
        //        means[i] = (double)(regularHandler.Aggregate(new FetchRequest("air", separateHoursDomains[i])).Result.GetValue(0,0));
        //    }

        //    double mean = (double)(regularHandler.Aggregate(new FetchRequest("air", yearDomain)).Result.GetValue(0, 0));
        //    Assert.AreEqual(mean,means.Average(),TestConstants.DoublePrecision);
        //}

        [TestMethod]
        [TestCategory("Local")]
        public async Task ReanslysisNewYearCrossingTest()
        {
            var regularStorage = TestDataStorageFactory.GetStorageContext(TestConstants.UriReanalysisRegular);
            NCEPReanalysisRegularGridDataHandler regularHandler = await NCEPReanalysisRegularGridDataHandler.CreateAsync(regularStorage);

            ITimeRegion crossingTr = new TimeRegion(firstYear: 1950, lastYear: 1951, firstDay: 336, lastDay: 30);
            IFetchDomain crossingDomain = FetchDomain.CreatePoints(
                new double[] { -37.5 }, // data index 51
                new double[] { 137.5 }, //data index 55
                 crossingTr);

            ITimeRegion firstTr = new TimeRegion(firstYear: 1950, lastYear: 1950, firstDay: 336, lastDay: 365);
            IFetchDomain firstDomain = FetchDomain.CreatePoints(
                new double[] { -37.5 }, // data index 51
                new double[] { 137.5 }, //data index 55
                 firstTr);

            ITimeRegion secondTr = new TimeRegion(firstYear: 1951, lastYear: 1951, firstDay: 1, lastDay: 30);
            IFetchDomain secondDomain = FetchDomain.CreatePoints(
                new double[] { -37.5 }, // data index 51
                new double[] { 137.5 }, //data index 55
                 secondTr);

            var handlerPrivate = new PrivateObject(regularHandler, new PrivateType(typeof(DataHandlerFacade)));
            var aggregatorPrivate = new PrivateObject(handlerPrivate, "valuesAggregator");

            double crossing = (double)(await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(regularStorage, new FetchRequest("air", crossingDomain)), null))).GetValue(0);
            double first = (double)(await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(regularStorage, new FetchRequest("air", firstDomain)), null))).GetValue(0);
            double second = (double)(await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(regularStorage, new FetchRequest("air", secondDomain)), null))).GetValue(0);
            Assert.AreEqual(crossing, (first + second) / 2.0, 1e-10);
        }

        [TestMethod]
        [TestCategory("Local")]
        public async Task ReanslysisNewYearCrossingSubDayTest()
        {
            var regularStorage = TestDataStorageFactory.GetStorageContext(TestConstants.UriReanalysisRegular);
            NCEPReanalysisRegularGridDataHandler regularHandler = await NCEPReanalysisRegularGridDataHandler.CreateAsync(regularStorage);

            ITimeRegion crossingTr = new TimeRegion(firstYear: 1950, lastYear: 1951, firstDay: 336, lastDay: 30, startHour: 0, stopHour: 12);
            IFetchDomain crossingDomain = FetchDomain.CreatePoints(
                new double[] { -37.5 }, // data index 51
                new double[] { 137.5 }, //data index 55
                 crossingTr);

            ITimeRegion firstTr = new TimeRegion(firstYear: 1950, lastYear: 1950, firstDay: 336, lastDay: 365, startHour: 0, stopHour: 12);
            IFetchDomain firstDomain = FetchDomain.CreatePoints(
                new double[] { -37.5 }, // data index 51
                new double[] { 137.5 }, //data index 55
                 firstTr);

            ITimeRegion secondTr = new TimeRegion(firstYear: 1951, lastYear: 1951, firstDay: 1, lastDay: 30, startHour: 0, stopHour: 12);
            IFetchDomain secondDomain = FetchDomain.CreatePoints(
                new double[] { -37.5 }, // data index 51
                new double[] { 137.5 }, //data index 55
                 secondTr);

            var handlerPrivate = new PrivateObject(regularHandler, new PrivateType(typeof(DataHandlerFacade)));
            var aggregatorPrivate = new PrivateObject(handlerPrivate, "valuesAggregator");

            double crossing = (double)(await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(regularStorage, new FetchRequest("air", crossingDomain)), null))).GetValue(0);
            double first = (double)(await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(regularStorage, new FetchRequest("air", firstDomain)), null))).GetValue(0);
            double second = (double)(await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(regularStorage, new FetchRequest("air", secondDomain)), null))).GetValue(0);
            Assert.AreEqual(crossing, (first + second) / 2.0, 1e-6);
        }

        [TestMethod]
        [TestCategory("Local")]        
        public async Task CellGridDataLinearInterpolationTest()
        {
            var storage = TestDataStorageFactory.GetStorageContext(TestConstants.UriReanalysisRegular);
            NCEPReanalysisRegularGridDataHandler reanalysis = await NCEPReanalysisRegularGridDataHandler.CreateAsync(storage);


            ITimeRegion tr = new TimeRegion(firstYear: 1980, lastYear: 1980, firstDay: 2, lastDay: 3, startHour: 0, stopHour: 0).
                GetSeasonlyTimeseries(firstDay: 2, lastDay: 3, isIntervalTimeseries: false);//indeces 46756,46760; hours sice 1-1-1 00:00:00   =  17347560 , 17347524
            IFetchDomain domain = FetchDomain.CreateCellGrid(
                //ocean region
                new double[] { 5.0, 7.5, 10.0 }, //indeces 34,33,32
                new double[] { 40.0, 42.5, 45, 47.5 }, //indeces 16,17,18,19
                tr);
            FetchRequest request = new FetchRequest("air", domain);

            bool[,, ] mask = new bool[3, 2,  2];
            bool[] smask = System.Linq.Enumerable.Repeat(true, 12).ToArray();
            Buffer.BlockCopy(smask, 0, mask, 0, 12 * sizeof(bool));

            var handlerPrivate = new PrivateObject(reanalysis, new PrivateType(typeof(DataHandlerFacade)));
            var aggregatorPrivate = new PrivateObject(handlerPrivate, "valuesAggregator");

            for (int i = 0; i < 2; i++)
            {
                bool[, ,] effectiveMask = i == 0 ? null : mask;
                
                var result = await (Task<Array>)(aggregatorPrivate.Invoke("AggregateAsync", RequestContextStub.GetStub(storage, request), effectiveMask));                                                 

                //lon,lat,t
                double[, , ] t = (double[, , ])result;

                Assert.AreEqual(12, t.Length);
                Assert.AreEqual(3, t.GetLength(0));
                Assert.AreEqual(2, t.GetLength(1));                

                //first time layer manual calculation
                //SDS fetched data
                //C:\Users\Dmitry>sds data "msds:az?name=ReanalysisRegular&DefaultEndpointsProl=http&AccountName=fetch&AccountKey=1Y0EOrnCX6ULY8c3iMHg9rrul2BWbPHKsHUceZ7ShM/q9K0ml49gQm+PE7G7i7zCvrpuT/vT1aHzEArutw==" air[32:34,16:19,46756]

                //[32,16,46756]    -22521
                //[32,17,46756]    -22091
                //[32,18,46756]    -22221
                //[32,19,46756]    -22291
                //[33,16,46756]    -22391
                //[33,17,46756]    -21861
                //[33,18,46756]    -21921
                //[33,19,46756]    -22021
                //[34,16,46756]    -21811
                //[34,17,46756]    -21421
                //[34,18,46756]    -21461
                //[34,19,46756]    -21541

                //excel calculated values               x*0.01+239.66                       mean
                //-22521	-22091	-22221	-22291		14.45	18.75	17.45	16.75		17.5	19.425	18.525
                //-22391	-21861	-21921	-22021		15.75	21.05	20.45	19.45		20.95	23	    22.3
                //-21811	-21421	-21461	-21541		21.55	25.45	25.05	24.25

                //checking first time layer
                //ATTENTION: notice the lat indexing. it is flaped!
                //lon,lat,t
                A(17.5, t[0, 1, 0]); A(19.425, t[1, 1, 0]); A(18.525, t[2, 1, 0]);
                A(20.95, t[0, 0, 0]); A(23, t[1, 0, 0]); A(22.3, t[2, 0, 0]);


                //second time layer asserting
                //SDS data fetching
                //lon,lat,t
                //C:\Users\Dmitry>sds data "msds:az?name=ReanalysisRegular&DefaultEndpointsProl=http&AccountName=fetch&AccountKey=1Y0EOrnCX6ULY8c3iMHg9rrul2BWbPHKsHUceZ7ShM/q9K0ml49gQm+PE7G7i7zCvrpuT/vT1aHzEArutw==" air[32:34,16:19,46760]

                //[32,16,46760]    -22761
                //[32,17,46760]    -22071
                //[32,18,46760]    -22201
                //[32,19,46760]    -22391
                //[33,16,46760]    -22571
                //[33,17,46760]    -21911
                //[33,18,46760]    -21961
                //[33,19,46760]    -22111
                //[34,16,46760]    -21891
                //[34,17,46760]    -21601
                //[34,18,46760]    -21611
                //[34,19,46760]    -21631

                //excel calculated values               x*0.01+239.66                       mean
                //-22761	-22071	-22201	-22391		12.05	18.95	17.65	15.75		16.375	19.3	18
                //-22571	-21911	-21961	-22111		13.95	20.55	20.05	18.55		19.725	21.95	21.375
                //-21891	-21601	-21611	-21631		20.75	23.65	23.55	23.35				                

                //checking second time layer
                //ATTENTION: notice the lat indexing. it is flaped!
                //lon,lat,t
                A(16.375, t[0, 1, 1]); A(19.3, t[1, 1,  1]); A(18, t[2, 1, 1 ]);
                A(19.725, t[0, 0, 1]); A(21.95, t[1, 0, 1]); A(21.375, t[2, 0, 1]);
            }
        }

        void A(double exp, double act)
        {
            Assert.AreEqual(exp, act, TestConstants.FloatPrecision); //as scale_factor and add_offset are float
        }
    }
}

