using Microsoft.Research.Science.Data;
using Microsoft.Research.Science.FetchClimate2.DataHandlers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Tests
{
    /// <summary>
    /// Provides simple plane generating observations:
    /// for 1970 Jan Feb Mar May Temp=(lat>1)?(3/5*lat-1):(3/5*lat)  Prate=3/5*lat
    /// for 1970 Apr Temp=(lat>1)?(6/5*lat-1):(6/5*lat) Prate=6/5*lat
    /// 
    /// elevation:
    /// for lat>0 6490 meters (1 deg cooler)
    /// else 0 meters
    /// </summary>
    class GhcnStorageContextStub : LinearizingStorageContext, IStorageContext
    {
        private static DataSet GetDataSet()
        {
            DataSet data = DataSet.Open("msds:memory");

            double[] lats = new double[] { 0.0, 0.0, 90.0, 0.0, 0.0, -90.0 };
            double[] lons = new double[] { 0.0, 90.0, 0.0, 180.0, 270.0,0.0 };
            double[,] tvals = new double[,] {
        { 3.0,6.0,15.0,9.0,12.0,18.0 },        
        { 3.0,6.0,15.0,9.0,12.0,18.0 }
        };            
            DateTime[] times = new DateTime[] {
            new DateTime(1970,3,1),          
            new DateTime(1970,4,1)
        };

            data.AddVariable<double>("lat", lats, "stations");
            data.AddVariable<double>("lon", lons, "stations");
            data.AddVariable<double>("temp", tvals, "time", "stations");
            data.AddVariable<DateTime>("time", times, "time");

            return data;
        }

        public GhcnStorageContextStub()
            : base(GetDataSet())
        { }
    }

    [TestClass]
    public class GhcnTests
    {
        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public async Task GHCNManualTest()
        {           
            IStorageContext storage = new GhcnStorageContextStub();
            DataSourceHandler handler = await GHCNv2DataSource.DataHandler.CreateAsync(storage);

            IFetchDomain fd = FetchDomain.CreatePoints(new double[] { Math.Asin(1.0 / Math.Sqrt(3.0)) * 180.0 / Math.PI }, new double[] { 45.0 }, new TimeRegion(firstYear: 1970, lastYear: 1970, firstDay: 60, lastDay: 90)); //mar
            FetchRequest fr = new FetchRequest("temp", fd);

            IRequestContext requestContext = RequestContextStub.GetStub(storage,fr);

            var result = await handler.ProcessRequestAsync(requestContext);
            Assert.AreEqual(8.0, (double)result.GetValue(0), TestConstants.DoublePrecision); 
        }

        [TestMethod]
        [TestCategory("Local")]
        [Timeout(5000)]
        [TestCategory("BVT")]
        public async Task GHCNOutOfDataTests()
        {
            IStorageContext storage = new GhcnStorageContextStub();
            DataSourceHandler handler = await GHCNv2DataSource.DataHandler.CreateAsync(storage);

            IFetchDomain fd = FetchDomain.CreatePoints(new double[] { Math.Asin(1.0 / Math.Sqrt(3.0)) * 180.0 / Math.PI }, new double[] { 45.0 }, new TimeRegion(firstYear: 1970, lastYear: 1970, firstDay: 160, lastDay: 190)); //mar
            FetchRequest fr = new FetchRequest("temp", fd);

            IRequestContext requestContext = RequestContextStub.GetStub(storage, fr);

            var result = await handler.ProcessRequestAsync(requestContext);
            Assert.IsTrue(double.IsNaN((double)result.GetValue(0)));

        }

        [TestMethod]
        [TestCategory("Local")]
        public async Task GhcnPartialFutureOutOfDataFinishesTest()
        {
            System.Diagnostics.Trace.WriteLine(TestConstants.UriGHCN);
            var storage = TestDataStorageFactory.GetStorageContext(TestConstants.UriGHCN);
            DataSourceHandler handler = await GHCNv2DataSource.DataHandler.CreateAsync(storage);

            ITimeRegion tr = new TimeRegion(firstYear: 1990, lastYear: 2101).GetMonthlyTimeseries(firstMonth: 12, lastMonth: 12);//data index 2567
            IFetchDomain domain = FetchDomain.CreatePoints(
                new double[] { -25.18 }, //exact station. data index 18398
                new double[] { 151.65 },
                 tr);

            FetchRequest prateRequest = new FetchRequest("prate", domain);


            var reqContext = RequestContextStub.GetStub(storage, prateRequest);

            var result = await handler.ProcessRequestAsync(reqContext);
        }
        
        [TestMethod]
        [TestCategory("Local")]
        public async Task GhcnFutureOutOfDataFinishesTest()
        {
            System.Diagnostics.Trace.WriteLine(TestConstants.UriGHCN);
            var storage = TestDataStorageFactory.GetStorageContext(TestConstants.UriGHCN);
            DataSourceHandler handler = await GHCNv2DataSource.DataHandler.CreateAsync(storage);

            ITimeRegion tr = new TimeRegion(firstYear: 2100, lastYear: 2101).GetMonthlyTimeseries(firstMonth: 12, lastMonth: 12);//data index 2567
            IFetchDomain domain = FetchDomain.CreatePoints(
                new double[] { -25.18 }, //exact station. data index 18398
                new double[] { 151.65 },
                 tr);

            FetchRequest prateRequest = new FetchRequest("prate", domain);


            var reqContext = RequestContextStub.GetStub(storage, prateRequest);

            var result = await handler.ProcessRequestAsync(reqContext);            
        }


        /// <summary>
        /// Manual comparison
        /// </summary>
        [TestMethod]
        [TestCategory("Local")]
        public async Task GhcnPrateValuesTest()
        {
            System.Diagnostics.Trace.WriteLine(TestConstants.UriGHCN);
            var storage = TestDataStorageFactory.GetStorageContext(TestConstants.UriGHCN);
            DataSourceHandler handler = await GHCNv2DataSource.DataHandler.CreateAsync(storage);

            ITimeRegion tr = new TimeRegion(firstYear: 1914, lastYear: 1914).GetMonthlyTimeseries(firstMonth: 12, lastMonth: 12);//data index 2567
            IFetchDomain domain = FetchDomain.CreatePoints(
                new double[] { -25.18 }, //exact station. data index 18398
                new double[] { 151.65 },
                 tr);

            FetchRequest prateRequest = new FetchRequest("prate", domain);
            

            var reqContext = RequestContextStub.GetStub(storage, prateRequest);

            var result = await handler.ProcessRequestAsync(reqContext);

            Assert.AreEqual(85.1, (double)result.GetValue(0), 1e-3); //manual data comparison.
        }



        /// <summary>
        /// Manual comparison
        /// </summary>
        [TestMethod]
        [TestCategory("Local")]
        public async Task GhcnTempValuesTest()
        {
            System.Diagnostics.Trace.WriteLine(TestConstants.UriGHCN);
            string etopoLocalUri = TestConstants.UriEtopo;

            var ghcnStorage = TestDataStorageFactory.GetStorageContext(TestConstants.UriGHCN);

            DataSourceHandler handler = await GHCNv2DataSource.DataHandler.CreateAsync(ghcnStorage);            

            ITimeRegion tr = new TimeRegion(firstYear: 1921, lastYear: 1921).GetMonthlyTimeseries(firstMonth: 3, lastMonth: 3);//data index 2642
            IFetchDomain domain = FetchDomain.CreatePoints(
                new double[] { 36.27 }, //exact station. data index 3776
                new double[] { -90.97 },
                 tr);

            FetchRequest tempRequest = new FetchRequest("temp", domain);

            var reqContext = RequestContextStub.GetStub(ghcnStorage, tempRequest);

            var result = await handler.ProcessRequestAsync(reqContext);

            Assert.AreEqual(15.6, (double)result.GetValue(0), 1e-5); //manual data comparison.
        }

        /// <summary>
        /// Test FetchTestairtFromGHCN fails with -85 degC
        /// </summary>
        [TestMethod]
        [TestCategory("Local")]
        public async Task Bug1691()
        {
            double BayOfBiscaySELat = 44.5;
            double BayOfBiscaySELon = -3.5;

            double InFranceLat = 47;
            double InFranceLon = 1;

            Random r = new Random(1);
            var eps = r.NextDouble() / 10.0;
            double latDelta = InFranceLat - BayOfBiscaySELat;
            double lonDelta = InFranceLon - BayOfBiscaySELon;
            var tr = new TimeRegion(1990, 2001, 1, -1, 0, 24);

            var request = new FetchRequest(
                "temp",
                FetchDomain.CreateCellGrid(
                Enumerable.Range(0, 31).Select(i => eps + BayOfBiscaySELat + i * latDelta / 31.0).ToArray(),
                Enumerable.Range(0, 21).Select(i => eps + BayOfBiscaySELon + i * lonDelta / 21.0).ToArray(),
                tr));


            string etopoLocalUri = TestConstants.UriEtopo;

            var ghcnStorage = TestDataStorageFactory.GetStorageContext(TestConstants.UriGHCN);

            var reqContext = RequestContextStub.GetStub(ghcnStorage, request);

            DataSourceHandler handler = await GHCNv2DataSource.DataHandler.CreateAsync(ghcnStorage); 

            var result = await handler.ProcessRequestAsync(reqContext);

            Assert.IsTrue(-70.0 < (double)result.GetValue(16, 2)); //manual data comparison.            
        }
    }
}
