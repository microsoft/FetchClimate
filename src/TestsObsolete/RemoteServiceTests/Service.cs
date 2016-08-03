using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Diagnostics;
using Microsoft.Research.Science.Data;

namespace Microsoft.Research.Science.FetchClimate2.Tests
{
    [TestClass]
    public class Service
    {
        //private readonly string serviceUri = "http://127.0.0.1:81";
        private readonly string serviceUri = "http://fetchclimate2.cloudapp.net/";
        //private readonly string serviceUri = "http://fetchclimate2staging5.cloudapp.net/";
        private readonly Random r = new Random();

        private readonly double BayOfBiscaySELat = 44.5;
        private readonly double BayOfBiscaySELon = -3.5;

        private readonly double InFranceLat = 47;
        private readonly double InFranceLon = 1;

        /// <summary>Returns single time interval with 10 years from 1990 to 2000        
        private TimeRegion CreateSingleTimeInterval()
        {
            return new TimeRegion(1990, 2001, 1, -1, 0, 24);
        }

        private TimeRegion CreateTestTimeRegion(bool isIntervalYearly, bool isIntervalDaily, bool isIntervalHourly)
        {
            return new TimeRegion(1990, 2001, 1, -1, 0, 24, isIntervalYearly, isIntervalDaily, isIntervalHourly);
        }

        private FetchDomain CreatePointsDomain(TimeRegion tr)
        {
            var eps = r.NextDouble() / 10.0;
            Trace.WriteLine(String.Format("Using eps={0}", eps));
            return FetchDomain.CreatePoints(
                new double[] { BayOfBiscaySELat + eps, InFranceLat + eps },
                new double[] { BayOfBiscaySELon + eps, InFranceLon + eps },
                tr);
        }

        private FetchDomain CreateCellsDomain(TimeRegion tr)
        {
            var eps = r.NextDouble() / 10.0;
            Trace.WriteLine(String.Format("Using eps={0}", eps));
            return FetchDomain.CreateCells(
                new double[] { BayOfBiscaySELat + eps, InFranceLat + eps },
                new double[] { BayOfBiscaySELon + eps, InFranceLon + eps },
                new double[] { BayOfBiscaySELat + 1.0 + eps, InFranceLat + 1.0 + eps },
                new double[] { BayOfBiscaySELon + 1.0 + eps, InFranceLon + 1.0 + eps },
                tr);
        }

        private FetchDomain CreatePointGridDomain(TimeRegion tr)
        {
            var eps = r.NextDouble() / 10.0;
            Trace.WriteLine(String.Format("Using eps={0}", eps));
            double latDelta = InFranceLat - BayOfBiscaySELat;
            double lonDelta = InFranceLon - BayOfBiscaySELon;
            return FetchDomain.CreatePointGrid(
                Enumerable.Range(0, 31).Select(i => eps + BayOfBiscaySELat + i * latDelta / 31.0).ToArray(),
                Enumerable.Range(0, 21).Select(i => eps + BayOfBiscaySELon + i * lonDelta / 21.0).ToArray(),
                tr);
        }

        private FetchDomain CreateCellGridDomain(TimeRegion tr)
        {
            var eps = r.NextDouble() / 10.0;
            Trace.WriteLine(String.Format("Using eps={0}", eps));
            double latDelta = InFranceLat - BayOfBiscaySELat;
            double lonDelta = InFranceLon - BayOfBiscaySELon;
            return FetchDomain.CreateCellGrid(
                Enumerable.Range(0, 31).Select(i => eps + BayOfBiscaySELat + i * latDelta / 31.0).ToArray(),
                Enumerable.Range(0, 21).Select(i => eps + BayOfBiscaySELon + i * lonDelta / 21.0).ToArray(),
                tr);
        }

        /// <summary>
        /// tests if <paramref name="value"/> lies in given range or is NaN or just is NaN if no range is given
        /// </summary>
        /// <param name="value">value to test</param>
        /// <param name="expectation">null (NaN expected) or pair of minimal and maximal possible values</param>
        private void TestValue(double value, Tuple<double, double> expectation)
        {
            double calcUncertatinty = 1e-7;
            if (expectation != null)
                Assert.IsTrue(double.IsNaN(value) || (value + calcUncertatinty >= expectation.Item1 && value - calcUncertatinty <= expectation.Item2));
            else
                Assert.IsTrue(double.IsNaN(value),"NaN is expected");
        }

        private void TestRequests(string variableName, string[] dataSources, Tuple<double, double> saneRange, bool isNaNoverLand, bool isNaNoverOcean)
        {
            RemoteFetchClient client = new RemoteFetchClient(new Uri(serviceUri));

            for (int i = 0; i < 8; ++i)
            {
                for (int k = 0; k < 4; ++k)
                {
                    bool[] times = new bool[3];
                    times[0] = !(i > 3);
                    times[1] = !((i % 4) > 1);
                    times[2] = !((i % 2) > 0);
                    int timeDims = times.Count(x => !x);
                    int spatDims = k / 2 + 1;
                    int timeDimsShift = spatDims - 1;
                    FetchRequest request;
                    if (k == 0) request = new FetchRequest(variableName, CreatePointsDomain(CreateTestTimeRegion(times[0], times[1], times[2])), dataSources);
                    else if (k == 1) request = new FetchRequest(variableName, CreateCellsDomain(CreateTestTimeRegion(times[0], times[1], times[2])), dataSources);
                    else if (k == 2) request = new FetchRequest(variableName, CreatePointGridDomain(CreateTestTimeRegion(times[0], times[1], times[2])), dataSources);
                    else request = new FetchRequest(variableName, CreateCellGridDomain(CreateTestTimeRegion(times[0], times[1], times[2])), dataSources);

                    Trace.WriteLine(String.Format("Testing. Spatial domain type: {0}. Time domain type: years: {1}, days: {2}, hours: {3}.",
                        k == 0 ? "2 points" : k == 1 ? "2 cells" : k == 2 ? "point grid" : "cell grid",
                        times[0] ? "1 interval" : "2 points",
                        times[1] ? "1 interval" : "2 points",
                        times[2] ? "1 interval" : "2 points"));

                    using (var result = client.FetchAsync(request, s => Trace.WriteLine(s)).Result)
                    {
                        var v = result.Variables["values"];
                        Assert.IsTrue(v.Dimensions.Count == spatDims + timeDims);
                        switch (k)
                        {
                            case 0:
                            case 1:
                                Assert.AreEqual(2, v.Dimensions[0].Length);
                                break;
                            case 2:
                                Assert.AreEqual(request.Domain.Lons.Length, v.Dimensions[0].Length);
                                Assert.AreEqual(request.Domain.Lats.Length, v.Dimensions[1].Length);
                                break;
                            case 3:
                                Assert.AreEqual(request.Domain.Lons.Length - 1, v.Dimensions[0].Length);
                                Assert.AreEqual(request.Domain.Lats.Length - 1, v.Dimensions[1].Length);
                                break;
                        }
                        for (int j = 1; j <= timeDims; ++j) Assert.AreEqual(2, v.Dimensions[j + timeDimsShift].Length);

                        if (dataSources != null && dataSources.Length > 1)
                        {
                            var p = result.Variables["provenance"];
                            Assert.IsTrue(p.Dimensions.Count == spatDims + timeDims);
                            for (int j = 1; j <= timeDims; ++j) Assert.AreEqual(2, p.Dimensions[j + timeDimsShift].Length);
                            switch (k)
                            {
                                case 0:
                                case 1:
                                    Assert.AreEqual(2, p.Dimensions[0].Length);
                                    break;
                                case 2:
                                    Assert.AreEqual(request.Domain.Lons.Length, p.Dimensions[0].Length);
                                    Assert.AreEqual(request.Domain.Lats.Length, p.Dimensions[1].Length);
                                    break;
                                case 3:
                                    Assert.AreEqual(request.Domain.Lons.Length - 1, p.Dimensions[0].Length);
                                    Assert.AreEqual(request.Domain.Lats.Length - 1, p.Dimensions[1].Length);
                                    break;
                            }
                        }

                        var sd = result.Variables["sd"];
                        Assert.IsTrue(sd.Dimensions.Count == spatDims + timeDims);
                        for (int j = 1; j <= timeDims; ++j) Assert.AreEqual(2, sd.Dimensions[j + timeDimsShift].Length);
                        switch (k)
                        {
                            case 0:
                            case 1:
                                Assert.AreEqual(2, sd.Dimensions[0].Length);
                                break;
                            case 2:
                                Assert.AreEqual(request.Domain.Lons.Length, sd.Dimensions[0].Length);
                                Assert.AreEqual(request.Domain.Lats.Length, sd.Dimensions[1].Length);
                                break;
                            case 3:
                                Assert.AreEqual(request.Domain.Lons.Length - 1, sd.Dimensions[0].Length);
                                Assert.AreEqual(request.Domain.Lats.Length - 1, sd.Dimensions[1].Length);
                                break;
                        }

                        if (spatDims == 1)
                        {
                            if (timeDims == 0)
                            {
                                double[] vals = (double[])v.GetData();
                                TestValue(vals[0], isNaNoverOcean ? null : saneRange);
                                TestValue(vals[1], isNaNoverLand ? null : saneRange);
                            }
                            else if (timeDims == 1)
                            {
                                double[,] vals = (double[,])v.GetData();
                                TestValue(vals[0, 0], isNaNoverOcean ? null : saneRange);
                                TestValue(vals[0, 1], isNaNoverOcean ? null : saneRange);
                                TestValue(vals[1, 0], isNaNoverLand ? null : saneRange);
                                TestValue(vals[1, 1], isNaNoverLand ? null : saneRange);
                            }
                            else if (timeDims == 2)
                            {
                                double[, ,] vals = (double[, ,])v.GetData();
                                for (int j1 = 0; j1 < 2; ++j1) for (int j2 = 0; j2 < 2; ++j2)
                                    {
                                        TestValue(vals[0, j1, j2], isNaNoverOcean ? null : saneRange);
                                        TestValue(vals[1, j1, j2], isNaNoverLand ? null : saneRange);
                                    }
                            }
                            else if (timeDims == 3)
                            {
                                double[, , ,] vals = (double[, , ,])v.GetData();
                                for (int j1 = 0; j1 < 2; ++j1) for (int j2 = 0; j2 < 2; ++j2) for (int j3 = 0; j3 < 2; ++j3)
                                        {
                                            TestValue(vals[0, j1, j2, j3], isNaNoverOcean ? null : saneRange);
                                            TestValue(vals[1, j1, j2, j3], isNaNoverLand ? null : saneRange);
                                        }
                            }
                        } 
                        else //grids
                        {
                            int lonOceanFirst = 0;
                            int lonOceanLast = 2;
                            int latOceanFirst = 0;
                            int latOceanLast = 5;
                            if (timeDims == 0)
                            {
                                double[,] vals = (double[,])v.GetData();
                                for (int s1 = 0; s1 < v.Dimensions[0].Length; ++s1) for (int s2 = 0; s2 < v.Dimensions[1].Length; ++s2) TestValue(vals[s1, s2], saneRange);
                                if (isNaNoverLand)
                                    for (int s1 = 27; s1 < v.Dimensions[0].Length; ++s1) for (int s2 = 0; s2 < v.Dimensions[1].Length; ++s2) TestValue(vals[s1, s2], null);
                                if (isNaNoverOcean)
                                    for (int s1 = lonOceanFirst; s1 <= lonOceanLast; ++s1) for (int s2 = latOceanFirst; s2 <= latOceanLast; ++s2) TestValue(vals[s1, s2], null);
                            }
                            else if (timeDims == 1)
                            {
                                double[, ,] vals = (double[, ,])v.GetData();
                                for (int s1 = 0; s1 < v.Dimensions[0].Length; ++s1) for (int s2 = 0; s2 < v.Dimensions[1].Length; ++s2) for (int j1 = 0; j1 < 2; ++j1) TestValue(vals[s1, s2, j1], saneRange);
                                if (isNaNoverLand)
                                    for (int s1 = 27; s1 < v.Dimensions[0].Length; ++s1) for (int s2 = 0; s2 < v.Dimensions[1].Length; ++s2) for (int j1 = 0; j1 < 2; ++j1) TestValue(vals[s1, s2, j1], null);
                                if (isNaNoverOcean)
                                    for (int s1 = lonOceanFirst; s1 <= lonOceanLast; ++s1) for (int s2 = latOceanFirst; s2 <= latOceanLast; ++s2) for (int j1 = 0; j1 < 2; ++j1) TestValue(vals[s1, s2, j1], null);
                            }
                            else if (timeDims == 2)
                            {
                                double[, , ,] vals = (double[, , ,])v.GetData();
                                for (int s1 = 0; s1 < v.Dimensions[0].Length; ++s1) for (int s2 = 0; s2 < v.Dimensions[1].Length; ++s2) for (int j1 = 0; j1 < 2; ++j1) for (int j2 = 0; j2 < 2; ++j2)
                                                TestValue(vals[s1, s2, j1, j2], saneRange);
                                if (isNaNoverLand)
                                    for (int s1 = 27; s1 < v.Dimensions[0].Length; ++s1) for (int s2 = 0; s2 < v.Dimensions[1].Length; ++s2) for (int j1 = 0; j1 < 2; ++j1) for (int j2 = 0; j2 < 2; ++j2)
                                                    TestValue(vals[s1, s2, j1, j2], null);
                                if (isNaNoverOcean)
                                    for (int s1 = lonOceanFirst; s1 <= lonOceanLast; ++s1) for (int s2 = latOceanFirst; s2 <= latOceanLast; ++s2) for (int j1 = 0; j1 < 2; ++j1) for (int j2 = 0; j2 < 2; ++j2)
                                                    TestValue(vals[s1, s2, j1, j2], null);
                            }
                            else if (timeDims == 3)
                            {
                                double[, , , ,] vals = (double[, , , ,])v.GetData();
                                for (int s1 = 0; s1 < v.Dimensions[0].Length; ++s1) for (int s2 = 0; s2 < v.Dimensions[1].Length; ++s2)
                                        for (int j1 = 0; j1 < 2; ++j1) for (int j2 = 0; j2 < 2; ++j2) for (int j3 = 0; j3 < 2; ++j3)
                                                    TestValue(vals[s1, s2, j1, j2, j3], saneRange);
                                if (isNaNoverLand)
                                    for (int s1 = 27; s1 < v.Dimensions[0].Length; ++s1) for (int s2 = 0; s2 < v.Dimensions[1].Length; ++s2)
                                            for (int j1 = 0; j1 < 2; ++j1) for (int j2 = 0; j2 < 2; ++j2) for (int j3 = 0; j3 < 2; ++j3)
                                                        TestValue(vals[s1, s2, j1, j2, j3], null);
                                if (isNaNoverOcean)
                                    for (int s1 = lonOceanFirst; s1 <= lonOceanLast; ++s1) for (int s2 = latOceanFirst; s2 <= latOceanLast; ++s2)
                                            for (int j1 = 0; j1 < 2; ++j1) for (int j2 = 0; j2 < 2; ++j2) for (int j3 = 0; j3 < 2; ++j3)
                                                        TestValue(vals[s1, s2, j1, j2, j3], null);
                            }
                        }
                    }
                    Trace.WriteLine("Passed");
                }
            }
        }

        private void VariableRequestTest(string name)
        {

            RemoteFetchClient client = new RemoteFetchClient(new Uri(serviceUri));
            
            FetchRequest request = new FetchRequest(name,
                CreateCellGridDomain(CreateSingleTimeInterval()));
            using (var result = client.FetchAsync(request, s => Trace.WriteLine(s)).Result)
            {
                var v = result.Variables["values"];
                Assert.IsTrue(v.Dimensions.Count == 2 && 
                    v.Dimensions[0].Length == request.Domain.Lons.Length - 1 &&
                    v.Dimensions[1].Length == request.Domain.Lats.Length - 1 );

                var p = result.Variables["provenance"];
                Assert.IsTrue(p.Dimensions.Count == 2 && 
                    p.Dimensions[0].Length == request.Domain.Lons.Length - 1 &&
                    p.Dimensions[1].Length == request.Domain.Lats.Length - 1 );

                var sd = result.Variables["sd"];
                Assert.IsTrue(sd.Dimensions.Count == 2 &&
                    sd.Dimensions[0].Length == request.Domain.Lons.Length - 1 &&
                    sd.Dimensions[1].Length == request.Domain.Lats.Length - 1);
            }
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void MarksRequestsTest()
        {
            double[] latmin = null, latmax, lonmin, lonmax = null;
            int[] startday, stopday, startyear, stopyear, starthour, stophour;

            //ClimateService.ServiceUrl = serviceUri;
            ClimateService.ServiceUrl = serviceUri;
            using (DataSet ds = DataSet.Open(@"Data\423b28bf6a4357f14b64f2b16ab759cb6b5961db.csv?openMode=readOnly"))
            {
                using (DataSet resultDs = ds.Clone("msds:memory"))
                {
                    latmin = ((double[])ds.Variables["LatMin"].GetData()).Select(e => (double)e).ToArray();
                    latmax = ((double[])ds.Variables["LatMax"].GetData()).Select(e => (double)e).ToArray();
                    lonmin = ((double[])ds.Variables["LonMin"].GetData()).Select(e => (double)e).ToArray();
                    lonmax = ((double[])ds.Variables["LonMax"].GetData()).Select(e => (double)e).ToArray();
                    startday = ((int[])ds.Variables["StartDay"].GetData());
                    stopday = ((int[])ds.Variables["StopDay"].GetData());
                    starthour = ((int[])ds.Variables["StartHour"].GetData());
                    stophour = ((int[])ds.Variables["StartHour"].GetData());
                    startyear = ((int[])ds.Variables["StartYear"].GetData());
                    stopyear = ((int[])ds.Variables["StartYear"].GetData());

                    TimeRegion tr = new TimeRegion(startyear[0], stopyear[0]);                    
                    FetchRequest fr = new FetchRequest("prate", FetchDomain.CreatePoints(latmin, lonmax, tr),new string[] { "CRU CL 2.0" } );
                    
                    var result = ClimateService.FetchAsync(fr).Result;
                    //resultDs.AddVariable<double>("result",result, "cells_dim");
                    ;
                }
            }
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void GetConfigurationTest()
        {
            RemoteFetchClient client = new RemoteFetchClient(new Uri(serviceUri));
            var latestConfig = client.GetConfiguration(DateTime.MaxValue);
            var latestConfig2 = client.GetConfiguration(latestConfig.TimeStamp);
            Assert.AreEqual(latestConfig, latestConfig2);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        [ExpectedException(typeof(InvalidOperationException))]
        public void GetConfigurationForEarlyTimestampTest()
        {
            RemoteFetchClient client = new RemoteFetchClient(new Uri(serviceUri));
            var latestConfig = client.GetConfiguration(new DateTime(1950,1,1));
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestairt()
        {
            TestRequests("airt", null, new Tuple<double, double>(-75, 70), false, false);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestairtFromWorldClim()
        {
            TestRequests("airt", new string[] { "WorldClim 1.4" }, new Tuple<double, double>(-75, 70), false, false);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestairtFromCRU()
        {
            TestRequests("airt", new string[] { "CRU CL 2.0" }, new Tuple<double, double>(-75, 70), false, false);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestairtFromNCEP()
        {
            TestRequests("airt", new string[] { "NCEP/NCAR Reanalysis 1 (regular grid)" }, new Tuple<double, double>(-75, 70), false, false);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestairtFromGHCN()
        {
            TestRequests("airt", new string[] { "GHCNv2" }, new Tuple<double, double>(-75, 70), false, false);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestairt_land()
        {
            TestRequests("airt_land", null, new Tuple<double, double>(-75, 70), false, true);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestairt_landFromWorldClim()
        {
            TestRequests("airt_land", new string[] { "WorldClim 1.4" }, new Tuple<double, double>(-75, 70), false, true);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestairt_landFromCRU()
        {
            TestRequests("airt_land", new string[] { "CRU CL 2.0" }, new Tuple<double, double>(-75, 70), false, true);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestairt_ocean()
        {
            TestRequests("airt_ocean", null, new Tuple<double, double>(-75, 70), true, false);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestairt_oceanFromFC1LegacySupport()
        {
            TestRequests("airt_ocean", new string[] { "FC1 Variables" }, new Tuple<double, double>(-75, 70), true, false);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestprate()
        {
            TestRequests("prate", null, new Tuple<double, double>(0, 11870), false, false);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestprateFromNCEP()
        {
            TestRequests("prate", new string[] { "NCEP/NCAR Reanalysis 1 (Gauss T62)" }, new Tuple<double, double>(0, 9296), false, false);//ref value Mawsynram, India
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestprateFromWorldClim()
        {
            TestRequests("prate", new string[] { "WorldClim 1.4" }, new Tuple<double, double>(0, 9296), false, false);//ref value Mawsynram, India
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestprateFromCRU()
        {
            TestRequests("prate", new string[] { "CRU CL 2.0" }, new Tuple<double, double>(0, 9296), false, false);//ref value Mawsynram, India
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestprateFromGHCN()
        {
            TestRequests("prate", new string[] { "GHCNv2" }, new Tuple<double, double>(0, 9296), false, false); //ref value Mawsynram, India
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestrelhum()
        {
            TestRequests("relhum", null, new Tuple<double, double>(0, 100), false, false);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestrelhumFromCRU()
        {
            TestRequests("relhum", new string[] { "CRU CL 2.0" }, new Tuple<double, double>(0, 100), false, false);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestrelhum_land()
        {
            TestRequests("relhum_land", null, new Tuple<double, double>(0, 100), false, true);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestrelhum_landFromFC1LegacySupport()
        {
            TestRequests("relhum_land", new string[] { "FC1 Variables" }, new Tuple<double, double>(0, 100), false, true);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestdtr()
        {
            TestRequests("dtr", null, new Tuple<double, double>(0, 145), false, false);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestdtrFromCRU()
        {
            TestRequests("dtr", new string[] { "CRU CL 2.0" }, new Tuple<double, double>(0, 145), false, false);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestfrs()
        {
            TestRequests("frs", null, new Tuple<double, double>(0, 31), false, false);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestfrsFromCRU()
        {
            TestRequests("frs", new string[] { "CRU CL 2.0" }, new Tuple<double, double>(0, 31), false, false);
        }

        //[TestMethod]
        //[TestCategory("Uses remote Cloud deployment")]
        //public void FetchTestwet()
        //{
        //    TestRequests("wet", null, new Tuple<double, double>(0, 31), false, false);
        //}

        //[TestMethod]
        //[TestCategory("Uses remote Cloud deployment")]
        //public void FetchTestwetFromCRU()
        //{
        //    TestRequests("wet", new string[] { "CRU CL 2.0" }, new Tuple<double, double>(0, 31), false, false);
        //}

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestsunp()
        {
            TestRequests("sunp", null, new Tuple<double, double>(0, 100), false, false);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestsunpFromCRU()
        {
            TestRequests("sunp", new string[] { "CRU CL 2.0" }, new Tuple<double, double>(0, 100), false, false);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestwindspeed()
        {
            TestRequests("windspeed", null, new Tuple<double, double>(0, 115), false, false);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestwindspeedFromCRU()
        {
            TestRequests("windspeed", new string[] { "CRU CL 2.0" }, new Tuple<double, double>(0, 115), false, false);
        }

        //[TestMethod]
        //[TestCategory("Uses remote Cloud deployment")]
        //public void FetchTestpet()
        //{
        //    TestRequests("pet", null, new Tuple<double, double>(0, 1270), false, false);
        //}

        //[TestMethod]
        //[TestCategory("Uses remote Cloud deployment")]
        //public void FetchTestpetFromMalmstromPET()
        //{
        //    TestRequests("pet", new string[] { "Malmstrom PET" }, new Tuple<double, double>(0, 1270), false, false);
        //}

        //[TestMethod]
        //[TestCategory("Uses remote Cloud deployment")]
        //public void FetchTestwvsp()
        //{
        //    TestRequests("wvsp", null, new Tuple<double, double>(0, 310), false, false);
        //}

        //[TestMethod]
        //[TestCategory("Uses remote Cloud deployment")]
        //public void FetchTestwvspFromWagnerWVSP()
        //{
        //    TestRequests("wvsp", new string[] { "WagnerWVSP" }, new Tuple<double, double>(0, 310), false, false);
        //}

        //[TestMethod]
        //[TestCategory("Uses remote Cloud deployment")]
        //public void FetchTestwvp()
        //{
        //    TestRequests("wvp", null, new Tuple<double, double>(0, 310), false, false);
        //}

        //[TestMethod]
        //[TestCategory("Uses remote Cloud deployment")]
        //public void FetchTestwvpFromWagnerWVSP()
        //{
        //    TestRequests("wvp", new string[] { "WagnerWVSP" }, new Tuple<double, double>(0, 310), false, false);
        //}

        //[TestMethod]
        //[TestCategory("Uses remote Cloud deployment")]
        //public void FetchTestabshum()
        //{
        //    TestRequests("abshum", null, new Tuple<double, double>(0, 340), false, false);
        //}

        //[TestMethod]
        //[TestCategory("Uses remote Cloud deployment")]
        //public void FetchTestabshumFromWagnerWVSP()
        //{
        //    TestRequests("abshum", new string[] { "WagnerWVSP" }, new Tuple<double, double>(0, 340), false, false);
        //}

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestelev_land()
        {
            TestRequests("elev_land", null, new Tuple<double, double>(-100, 9000), false, true);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestelev_landFromGTOPO30()
        {
            TestRequests("elev_land", new string[] { "GTOPO30" }, new Tuple<double, double>(-100, 9000), false, true);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestelev()
        {
            TestRequests("elev", null, new Tuple<double, double>(-12000, 9000), false, false);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestelevFromETOPO1()
        {
            TestRequests("elev", new string[] { "ETOPO1" }, new Tuple<double, double>(-12000, 9000), false, false);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestdepth_ocean()
        {
            TestRequests("depth_ocean", null, new Tuple<double, double>(0, 12000), true, false);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestdepth_oceanFromFC1LegacySupport()
        {
            TestRequests("depth_ocean", new string[] { "FC1 Variables" }, new Tuple<double, double>(0, 12000), true, false);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestsoilmoist()
        {
            TestRequests("soilmoist", null, new Tuple<double, double>(0, 1000), false, true); //1000 as the units are mm/m
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTestsoilmoistFromCpcSoilMoisture()
        {
            TestRequests("soilmoist", new string[] { "CpcSoilMoisture" }, new Tuple<double, double>(0, 475), false, true); //475 as it is the max valu of the datasource
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void PointRequestTest()
        {
            var tr = new TimeRegion(1990,2000,1,358).GetYearlyTimeseries(1990,2000,1,true).GetSeasonlyTimeseries(1,358,1,true);
            var r = new FetchRequest(
                "airt",
                FetchDomain.CreatePoints(
                    new double[] { 50, 52, 54 },
                    new double[] { 40, 42, 38 },
                    tr), new string[] { "CRU CL 2.0" });
            RemoteFetchClient client = new RemoteFetchClient(new Uri(serviceUri));
            var answer = client.FetchAsync(r).Result;
        } 
    }
}
