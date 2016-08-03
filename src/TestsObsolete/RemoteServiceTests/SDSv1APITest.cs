using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Research.Science.Data;
using Microsoft.Research.Science.Data.Imperative;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using RemoteServiceTests;

namespace Microsoft.Research.Science.FetchClimate2.Tests.Client
{
    [TestClass]
    public class SDSv1APITest
    {
        [ClassInitialize]
        public static void Initialize(TestContext tc)
        {
            //Microsoft.Research.Science.Data.Factory.DataSetFactory.Register(typeof(MemoryDataSet));
            //Microsoft.Research.Science.Data.Factory.DataSetFactory.Register(typeof(CsvDataSet));
        }

        private static void AssertThrow(Action a)
        {
            try
            {
                a();
                Assert.Fail();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception is caught as expected: " + ex.Message);
            }
        }

        [TestInitialize]
        public void testInit()
        {
            //ClimateService.ClearCache();
            //ClimateService.ServiceUrl = "http://fetchclimate2staging5.cloudapp.net/";
        }

        [TestMethod]
        [TestCategory("SDS APIv1")]
        [TestCategory("Uses remote Cloud deployment")]
        public void AddAxisCells()
        {
            using (var ds = DataSet.Open("msds:memory"))
            {//double method
                var ax = ds.AddAxisCells("test1", "units", -10.0, 10.0, 0.5);
                ds.Commit();
                Assert.IsTrue(ax.Metadata.ContainsKey("bounds"));
                string bndName = (string)ax.Metadata["bounds"];

                Assert.AreEqual(40, ax.Dimensions[0].Length);

                double[,] cells = (double[,])ds.Variables[bndName].GetData();
                double[] values = (double[])ax.GetData();
                Assert.AreEqual(-10, cells[0, 0], 1e-8);
                Assert.AreEqual(-9.5, cells[0, 1], 1e-8);
                Assert.IsTrue(values[0] > cells[0, 0] && values[0] < cells[0, 1]);

                Assert.AreEqual(-6.5, cells[7, 0], 1e-8);
                Assert.AreEqual(-6, cells[7, 1], 1e-8);
                Assert.IsTrue(values[7] > cells[7, 0] && values[7] < cells[7, 1]);

                Assert.AreEqual(9.5, cells[39, 0], 1e-8);
                Assert.AreEqual(10, cells[39, 1], 1e-8);
                Assert.IsTrue(values[39] > cells[39, 0] && values[39] < cells[39, 1]);
            }

            using (var ds = DataSet.Open("msds:memory"))
            {//float method and explicit cellBoundVar name
                var ax = ds.AddAxisCells("test1", "units", -10.0f, 10.0f, 0.5f, "rrttyy");
                ds.Commit();
                Assert.IsTrue(ax.Metadata.ContainsKey("bounds"));
                Assert.AreEqual("rrttyy", ax.Metadata["bounds"]);
                string bndName = (string)ax.Metadata["bounds"];

                Assert.AreEqual(40, ax.Dimensions[0].Length);

                float[,] cells = (float[,])ds.Variables[bndName].GetData();
                float[] values = (float[])ax.GetData();
                Assert.AreEqual(-10, cells[0, 0], 1e-8);
                Assert.AreEqual(-9.5, cells[0, 1], 1e-8);
                Assert.IsTrue(values[0] > cells[0, 0] && values[0] < cells[0, 1]);

                Assert.AreEqual(-6.5, cells[7, 0], 1e-8);
                Assert.AreEqual(-6, cells[7, 1], 1e-8);
                Assert.IsTrue(values[7] > cells[7, 0] && values[7] < cells[7, 1]);

                Assert.AreEqual(9.5, cells[39, 0], 1e-8);
                Assert.AreEqual(10, cells[39, 1], 1e-8);
                Assert.IsTrue(values[39] > cells[39, 0] && values[39] < cells[39, 1]);
            }

            using (var ds = DataSet.Open("msds:memory"))
            {//2 axes adding
                var ax = ds.AddAxisCells("test1", "units", -10.0f, 10.0f, 0.5f);
                var ax2 = ds.AddAxisCells("test2", "units", -11.0f, 11.0f, 0.5f);
                ds.Commit();
            }
        }

        [TestMethod]
        [TestCategory("SDS APIv1")]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchExplicitDataSource()
        {
            using (var ds = DataSet.Open("msds:memory"))
            {
                ds.Add("lon", "degrees East", new float[] { 0, 5, 10 }, "i");
                ds.Add("lat", "degrees North", new float[] { 10, 5, 0 }, "i");
                ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt", new DateTime(2011, 07, 12, 13, 10, 00), nameProvenance: "provCru", dataSource: EnvironmentalDataSource.CRU_CL_2_0);
                ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt", new DateTime(2011, 07, 12, 13, 10, 00), nameProvenance: "provNcep", dataSource: EnvironmentalDataSource.NCEP_REANALYSIS_1);
                CheckProvenance(ds["provCru"], ds["airt"].Dimensions.Select(d => d.Name).ToArray());
                CheckProvenance(ds["provNcep"], ds["airt"].Dimensions.Select(d => d.Name).ToArray());

                string[] cru = ds["provCru"].GetData() as string[];
                string[] ncep = ds["provNcep"].GetData() as string[];
                for (int i = 0; i < cru.Length; i++)
                {
                    Assert.AreNotEqual(cru[i], ncep[i]);
                }
            }
        }

        [TestMethod]
        [TestCategory("SDS APIv1")]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchUncertainty()
        {
            using (var ds = DataSet.Open("msds:memory"))
            {
                ds.AddAxis("lon", "degrees East", 0.0, 20.0, 5.0);
                ds.AddAxis("lat", "degrees North", 0.0, 10.0, 5.0);
                ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt", new DateTime(2011, 07, 12, 13, 10, 00), "airt-uncert");
                CheckUncertainty(ds["airt-uncert"], ds["airt"].Dimensions.Select(d => d.Name).ToArray());
            }

            using (var ds = DataSet.Open("msds:memory"))
            {
                ds.AddAxis("lon", "degrees East", 0.0, 20.0, 5.0);
                ds.AddAxis("lat", "degrees North", 0.0, 10.0, 5.0);
                ds.AddAxis("time", new DateTime(2000, 7, 19, 0, 0, 0), new DateTime(2000, 7, 19, 23, 0, 0), TimeSpan.FromHours(2));
                ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt", "airt-uncert");
                CheckUncertainty(ds["airt-uncert"], ds["airt"].Dimensions.Select(d => d.Name).ToArray());
            }

            using (var ds = DataSet.Open("msds:memory"))
            {
                ds.AddAxis("lon", "degrees East", 0.0, 20.0, 5.0);
                ds.AddAxis("lat", "degrees North", 0.0, 10.0, 5.0);
                ds.AddClimatologyAxisSeasonly();
                ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt", ds["lat"].ID, ds["lon"].ID, ds["time"].ID, "airt-uncert");
                CheckUncertainty(ds["airt-uncert"], ds["airt"].Dimensions.Select(d => d.Name).ToArray());
            }

            using (var ds = DataSet.Open("msds:memory"))
            {
                ds.Add("lon", "degrees East", new float[] { 0, 5, 10 }, "i");
                ds.Add("lat", "degrees North", new float[] { 10, 5, 0 }, "i");
                ds.AddClimatologyAxisSeasonly();
                ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt", "lat", "lon", "time", "airt-uncert");
                CheckUncertainty(ds["airt-uncert"], ds["airt"].Dimensions.Select(d => d.Name).ToArray());
            }

            using (var ds = DataSet.Open("msds:memory"))
            {
                ds.Add("lon", "degrees East", new float[] { 0, 5, 10 }, "i");
                ds.Add("lat", "degrees North", new float[] { 10, 5, 0 }, "i");
                ds.AddClimatologyAxisSeasonly();
                ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt", nameUncertainty: "airt-uncert");
                CheckUncertainty(ds["airt-uncert"], ds["airt"].Dimensions.Select(d => d.Name).ToArray());
            }

            using (var ds = DataSet.Open("msds:memory"))
            {
                ds.Add("lon", "degrees East", new float[] { 0, 5, 10 }, "i");
                ds.Add("lat", "degrees North", new float[] { 10, 5, 0 }, "i");
                ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt", new DateTime(2011, 07, 12, 13, 10, 00), "airt-uncert");
                CheckUncertainty(ds["airt-uncert"], ds["airt"].Dimensions.Select(d => d.Name).ToArray());
            }
        }

        [TestMethod]
        [TestCategory("SDS APIv1")]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchProvenance()
        {
            using (var ds = DataSet.Open("msds:memory"))
            {
                ds.AddAxis("lon", "degrees East", 0.0, 20.0, 5.0);
                ds.AddAxis("lat", "degrees North", 0.0, 10.0, 5.0);
                ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt", new DateTime(2011, 07, 12, 13, 10, 00), nameProvenance: "airt-prov");
                CheckProvenance(ds["airt-prov"], ds["airt"].Dimensions.Select(d => d.Name).ToArray());
            }

            using (var ds = DataSet.Open("msds:memory"))
            {
                ds.AddAxis("lon", "degrees East", 0.0, 20.0, 5.0);
                ds.AddAxis("lat", "degrees North", 0.0, 10.0, 5.0);
                ds.AddAxis("time", new DateTime(2000, 7, 19, 0, 0, 0), new DateTime(2000, 7, 19, 23, 0, 0), TimeSpan.FromHours(2));
                ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt", nameProvenance: "airt-prov");
                CheckProvenance(ds["airt-prov"], ds["airt"].Dimensions.Select(d => d.Name).ToArray());
            }

            using (var ds = DataSet.Open("msds:memory"))
            {
                ds.AddAxis("lon", "degrees East", 0.0, 20.0, 5.0);
                ds.AddAxis("lat", "degrees North", 0.0, 10.0, 5.0);
                ds.AddClimatologyAxisSeasonly();
                ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt", ds["lat"].ID, ds["lon"].ID, ds["time"].ID, nameProvenance: "airt-prov");
                CheckProvenance(ds["airt-prov"], ds["airt"].Dimensions.Select(d => d.Name).ToArray());
            }

            using (var ds = DataSet.Open("msds:memory"))
            {
                ds.Add("lon", "degrees East", new float[] { 0, 5, 10 }, "i");
                ds.Add("lat", "degrees North", new float[] { 10, 5, 0 }, "i");
                ds.AddClimatologyAxisSeasonly();
                ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt", "lat", "lon", "time", nameProvenance: "airt-prov");
                CheckProvenance(ds["airt-prov"], ds["airt"].Dimensions.Select(d => d.Name).ToArray());
            }

            using (var ds = DataSet.Open("msds:memory"))
            {
                ds.Add("lon", "degrees East", new float[] { 0, 5, 10 }, "i");
                ds.Add("lat", "degrees North", new float[] { 10, 5, 0 }, "i");
                ds.AddClimatologyAxisSeasonly();
                ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt", nameProvenance: "airt-prov");
                CheckProvenance(ds["airt-prov"], ds["airt"].Dimensions.Select(d => d.Name).ToArray());
            }

            using (var ds = DataSet.Open("msds:memory"))
            {
                ds.Add("lon", "degrees East", new float[] { 0, 5, 10 }, "i");
                ds.Add("lat", "degrees North", new float[] { 10, 5, 0 }, "i");
                ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt", new DateTime(2011, 07, 12, 13, 10, 00), nameProvenance: "airt-prov");
                CheckProvenance(ds["airt-prov"], ds["airt"].Dimensions.Select(d => d.Name).ToArray());
            }
        }

        private static void CheckUncertainty(Variable v, params string[] dims)
        {
            Assert.AreEqual(v.Rank, dims.Length);
            for (int i = 0; i < dims.Length; i++)
                Assert.AreEqual(v.Dimensions[i].Name, dims[i]);

            Array a = v.GetData();
            foreach (double c in a)
                Assert.IsTrue(double.IsNaN(c) || c > 0);
        }

        private static void CheckProvenance(Variable v, params string[] dims)
        {
            Assert.AreEqual(v.Rank, dims.Length);
            for (int i = 0; i < dims.Length; i++)
                Assert.AreEqual(v.Dimensions[i].Name, dims[i]);

            Array a = v.GetData();
            foreach (string c in a)
                Assert.IsTrue(!String.IsNullOrWhiteSpace(c));
        }

        [TestMethod]
        [TestCategory("SDS APIv1")]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchFailsTest()
        {
            // Fetching climate parameters for fixed time moment
            using (var ds = DataSet.Open("msds:memory"))
            {
                Console.WriteLine("Filling dataset...");
                ds.AddAxis("lon", "degrees East", 0, 20.0, 2);
                ds.AddAxis("lat", "degrees North", 0, 10.0, 2);

                AssertThrow(() => ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt"));
                AssertThrow(() => ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt", "lat", "lon", "time"));
            }
        }

        [TestMethod]
        [TestCategory("SDS APIv1")]
        [TestCategory("Uses remote Cloud deployment")]
        public void ClimatologyYearlyTimeseriesTest()
        {
            Random r = new Random(46);

            using (DataSet ds = DataSet.Open("msds:memory"))
            {
                Assert.AreEqual(0, ds.Variables.Count);
                ds.AddClimatologyAxisYearly();
                Assert.AreEqual(2, ds.Variables.Count);
                var clim = ds.Variables.Where(v => v.Rank == 2).First();
                DateTime dt0 = ((DateTime[,])clim.GetData(new int[] { 0, 0 }, new int[] { 1, 1 }))[0, 0];
                DateTime dt1 = ((DateTime[,])clim.GetData(new int[] { 0, 1 }, new int[] { 1, 1 }))[0, 0];
                DateTime dt2 = ((DateTime[,])clim.GetData(new int[] { 1, 0 }, new int[] { 1, 1 }))[0, 0];
                DateTime dt3 = ((DateTime[,])clim.GetData(new int[] { 1, 1 }, new int[] { 1, 1 }))[0, 0];
                Assert.AreNotEqual(dt0.Year, dt2.Year);
                Assert.AreEqual(dt0.Hour, dt2.Hour);
                Assert.AreEqual(dt0.DayOfYear, dt2.DayOfYear);
                Assert.AreNotEqual(dt1.Year, dt3.Year);
                Assert.AreEqual(dt1.Hour, dt3.Hour);
                Assert.AreEqual(dt1.DayOfYear, dt3.DayOfYear);
            }

            using (DataSet ds = DataSet.Open("msds:memory"))
            {
                Assert.AreEqual(0, ds.Variables.Count);
                ds.AddClimatologyAxisYearly(1940, 1954, 3, 230, 3, 5, 2, "tim123", "climatAx");
                Assert.AreEqual(2, ds.Variables.Count);
                var clim = ds.Variables["climatAx"];
                Assert.AreEqual("climatAx", ds.Variables["tim123"].Metadata["climatology"]);
                DateTime dt0 = ((DateTime[,])clim.GetData(new int[] { 0, 0 }, new int[] { 1, 1 }))[0, 0];
                DateTime dt1 = ((DateTime[,])clim.GetData(new int[] { 0, 1 }, new int[] { 1, 1 }))[0, 0];
                DateTime dt2 = ((DateTime[,])clim.GetData(new int[] { 1, 0 }, new int[] { 1, 1 }))[0, 0];
                DateTime dt3 = ((DateTime[,])clim.GetData(new int[] { 1, 1 }, new int[] { 1, 1 }))[0, 0];
                Assert.AreNotEqual(dt0.Year, dt2.Year);
                Assert.AreEqual(dt0.Hour, dt2.Hour);
                Assert.AreEqual(3, dt0.Hour);
                Assert.AreEqual(dt0.DayOfYear, dt2.DayOfYear);
                Assert.AreEqual(3, dt0.DayOfYear);
                Assert.AreNotEqual(dt1.Year, dt3.Year);
                Assert.AreEqual(dt1.Hour, dt3.Hour);
                Assert.AreEqual(5, dt1.Hour);
                Assert.AreEqual(dt1.DayOfYear, dt3.DayOfYear);
                Assert.AreEqual(230, dt1.DayOfYear);
            }
        }

        [TestMethod]
        [TestCategory("SDS APIv1")]
        [TestCategory("Uses remote Cloud deployment")]
        public void ClimatologyHourlyTimeseriesTest()
        {
            Random r = new Random(46);

            using (DataSet ds = DataSet.Open("msds:memory"))
            {
                Assert.AreEqual(0, ds.Variables.Count);
                ds.AddClimatologyAxisHourly();
                Assert.AreEqual(2, ds.Variables.Count);
                var clim = ds.Variables.Where(v => v.Rank == 2).First();
                DateTime dt0 = ((DateTime[,])clim.GetData(new int[] { 0, 0 }, new int[] { 1, 1 }))[0, 0];
                DateTime dt1 = ((DateTime[,])clim.GetData(new int[] { 0, 1 }, new int[] { 1, 1 }))[0, 0];
                DateTime dt2 = ((DateTime[,])clim.GetData(new int[] { 1, 0 }, new int[] { 1, 1 }))[0, 0];
                DateTime dt3 = ((DateTime[,])clim.GetData(new int[] { 1, 1 }, new int[] { 1, 1 }))[0, 0];
                Assert.AreEqual(dt0.Year, dt2.Year);
                Assert.AreNotEqual(dt0.Hour, dt2.Hour);
                Assert.AreEqual(dt0.DayOfYear, dt2.DayOfYear);
                Assert.AreEqual(dt1.Year, dt3.Year);
                Assert.AreNotEqual(dt1.Hour, dt3.Hour);
                Assert.AreEqual(dt1.DayOfYear, dt3.DayOfYear);
            }

            using (DataSet ds = DataSet.Open("msds:memory"))
            {
                Assert.AreEqual(0, ds.Variables.Count);
                ds.AddClimatologyAxisHourly(1940, 1954, 3, 230, 3, 5, 1, "tim123", "climatAx");
                Assert.AreEqual(2, ds.Variables.Count);
                var clim = ds.Variables["climatAx"];
                Assert.AreEqual("climatAx", ds.Variables["tim123"].Metadata["climatology"]);
                DateTime dt0 = ((DateTime[,])clim.GetData(new int[] { 0, 0 }, new int[] { 1, 1 }))[0, 0];
                DateTime dt1 = ((DateTime[,])clim.GetData(new int[] { 0, 1 }, new int[] { 1, 1 }))[0, 0];
                DateTime dt2 = ((DateTime[,])clim.GetData(new int[] { 1, 0 }, new int[] { 1, 1 }))[0, 0];
                DateTime dt3 = ((DateTime[,])clim.GetData(new int[] { 1, 1 }, new int[] { 1, 1 }))[0, 0];
                Assert.AreEqual(dt0.Year, dt2.Year);
                Assert.AreEqual(1940, dt0.Year);
                Assert.AreNotEqual(dt0.Hour, dt2.Hour);
                Assert.AreEqual(dt0.DayOfYear, dt2.DayOfYear);
                Assert.AreEqual(3, dt0.DayOfYear);
                Assert.AreEqual(dt1.Year, dt3.Year);
                Assert.AreEqual(1940, dt0.Year);
                Assert.AreEqual(1954, dt1.Year);
                Assert.AreNotEqual(dt1.Hour, dt3.Hour);
                Assert.AreEqual(dt1.DayOfYear, dt3.DayOfYear);
                Assert.AreEqual(230, dt1.DayOfYear);
            }
        }

        [TestMethod]
        [TestCategory("SDS APIv1")]
        [TestCategory("Uses remote Cloud deployment")]
        public void ClimatologySeasonlyTimeseriesTest()
        {
            Random r = new Random(54256);

            using (DataSet ds = DataSet.Open("msds:memory"))
            {
                Assert.AreEqual(0, ds.Variables.Count);
                ds.AddClimatologyAxisSeasonly();
                Assert.AreEqual(2, ds.Variables.Count);
                var clim = ds.Variables.Where(v => v.Rank == 2).First();
                DateTime dt0 = ((DateTime[,])clim.GetData(new int[] { 0, 0 }, new int[] { 1, 1 }))[0, 0];
                DateTime dt1 = ((DateTime[,])clim.GetData(new int[] { 0, 1 }, new int[] { 1, 1 }))[0, 0];
                DateTime dt2 = ((DateTime[,])clim.GetData(new int[] { 1, 0 }, new int[] { 1, 1 }))[0, 0];
                DateTime dt3 = ((DateTime[,])clim.GetData(new int[] { 1, 1 }, new int[] { 1, 1 }))[0, 0];
                Assert.AreEqual(dt0.Year, dt2.Year);
                Assert.AreEqual(dt0.Hour, dt2.Hour);
                Assert.AreNotEqual(dt0.DayOfYear, dt2.DayOfYear);
                Assert.AreEqual(dt1.Year, dt3.Year);
                Assert.AreEqual(dt1.Hour, dt3.Hour);
                Assert.AreNotEqual(dt1.DayOfYear, dt3.DayOfYear);
            }

            using (DataSet ds = DataSet.Open("msds:memory"))
            {
                Assert.AreEqual(0, ds.Variables.Count);
                ds.AddClimatologyAxisSeasonly(1940, 1954, 3, 230, 3, 5, 5, "tim123", "climatAx");
                Assert.AreEqual(2, ds.Variables.Count);
                var clim = ds.Variables["climatAx"];
                Assert.AreEqual("climatAx", ds.Variables["tim123"].Metadata["climatology"]);
                DateTime dt0 = ((DateTime[,])clim.GetData(new int[] { 0, 0 }, new int[] { 1, 1 }))[0, 0];
                DateTime dt1 = ((DateTime[,])clim.GetData(new int[] { 0, 1 }, new int[] { 1, 1 }))[0, 0];
                DateTime dt2 = ((DateTime[,])clim.GetData(new int[] { 1, 0 }, new int[] { 1, 1 }))[0, 0];
                DateTime dt3 = ((DateTime[,])clim.GetData(new int[] { 1, 1 }, new int[] { 1, 1 }))[0, 0];
                Assert.AreEqual(dt0.Year, dt2.Year);
                Assert.AreEqual(1940, dt0.Year);
                Assert.AreEqual(dt0.Hour, dt2.Hour);
                Assert.AreEqual(3, dt0.Hour);
                Assert.AreNotEqual(dt0.DayOfYear, dt2.DayOfYear);
                Assert.AreEqual(dt1.Year, dt3.Year);
                Assert.AreEqual(1954, dt1.Year);
                Assert.AreEqual(dt1.Hour, dt3.Hour);
                Assert.AreEqual(5, dt1.Hour);
                Assert.AreNotEqual(dt1.DayOfYear, dt3.DayOfYear);
            }
        }

        [TestMethod]
        [TestCategory("SDS APIv1")]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchClimatologyPointSetTest()
        {
            Random r = new Random(4235234);
            double[] lats = Enumerable.Repeat<int>(0, 200).Select(a => r.NextDouble() * 180.0 - 90.0).ToArray();
            double[] lons = Enumerable.Repeat<int>(0, 200).Select(a => r.NextDouble() * 360.0).ToArray();
            var ds = DataSet.Open("msds:memory");

            //ds.AddClimatologyAxisHourly
            ds.AddClimatologyAxisMonthly();

            ds.Add("Lat", lats, "points");
            ds.Add("Lon", lons, "points");

            ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt");
            Assert.IsTrue(ds.Variables.Contains("airt"));
            Assert.AreEqual(2, ds.Variables["airt"].Dimensions.Count);
            Assert.AreEqual(3, ds.Dimensions.Count);

        }

        [TestMethod]
        [TestCategory("SDS APIv1")]
        [TestCategory("Uses remote Cloud deployment")]
        public void GridEquivalence()
        {
            using (var ds = DataSet.Open("msds:memory"))
            {
                ds.AddAxis("Lat", "Degrees", 12.0, 15.0, 0.5);
                ds.AddAxis("Lon", "Degrees", 8.0, 11.0, 0.25);
                ds.AddClimatologyAxisYearly(yearStep: 30);
                ds.Fetch(ClimateParameter.FC_TEMPERATURE, "air0");
                ds.Commit();
                double[, ,] temp = (double[, ,])ds.Variables["air0"].GetData();
                Assert.AreEqual(ClimateService.FetchClimate(ClimateParameter.FC_TEMPERATURE, 12.0, 12.0, 8.0, 8.0, stopday:365), temp[0, 0, 0], 1e-8);
                Assert.AreEqual(ClimateService.FetchClimate(ClimateParameter.FC_TEMPERATURE, 13.5, 13.5, 9.25, 9.25, stopday: 365), temp[0, 3, 5], 1e-8);
                Assert.AreEqual(ClimateService.FetchClimate(ClimateParameter.FC_TEMPERATURE, 15.0, 15.0, 11.0, 11.0, stopday: 365), temp[0, 6, 12], 1e-8);
            }
        }

        [TestMethod]
        [TestCategory("SDS APIv1")]
        [TestCategory("Uses remote Cloud deployment")]
        public void CellsEquivalence()
        {
            using (var ds = DataSet.Open("msds:memory"))
            {
                ds.AddAxisCells("Lat", "Degrees", 12.0, 15.0, 0.5);
                ds.AddAxisCells("Lon", "Degrees", 8.0, 11.0, 0.25);
                ds.AddClimatologyAxisYearly(yearStep: 29);
                ds.Commit();
                ds.Fetch(ClimateParameter.FC_TEMPERATURE, "air0");
                ds.Commit();
                double[, ,] temp = (double[, ,])ds.Variables["air0"].GetData();
                Assert.AreEqual(ClimateService.FetchClimate(ClimateParameter.FC_TEMPERATURE, 12.0, 12.5, 8.0, 8.25), temp[0, 0, 0], 1e-8);
                Assert.AreEqual(ClimateService.FetchClimate(ClimateParameter.FC_TEMPERATURE, 13.5, 14.0, 9.25, 9.5), temp[0, 3, 5], 1e-8);
                Assert.AreEqual(ClimateService.FetchClimate(ClimateParameter.FC_TEMPERATURE, 14.5, 15.0, 10.75, 11.0), temp[0, 5, 11], 1e-8);
            }

            using (var ds = DataSet.Open("msds:memory"))
            {
                ds.AddAxisCells("Lat", "Degrees", 12.0f, 15.0f, 0.5f);
                ds.AddAxisCells("Lon", "Degrees", 8.0, 11.0, 0.25);
                ds.AddClimatologyAxisYearly(yearStep: 30);
                ds.Commit();
                ds.Fetch(ClimateParameter.FC_TEMPERATURE, "air0");
                ds.Commit();
                double[, ,] temp = (double[, ,])ds.Variables["air0"].GetData();
                Assert.AreEqual(ClimateService.FetchClimate(ClimateParameter.FC_TEMPERATURE, 12.0, 12.5, 8.0, 8.25), temp[0, 0, 0], 1e-8);
                Assert.AreEqual(ClimateService.FetchClimate(ClimateParameter.FC_TEMPERATURE, 13.5, 14.0, 9.25, 9.5), temp[0, 3, 5], 1e-8);
                Assert.AreEqual(ClimateService.FetchClimate(ClimateParameter.FC_TEMPERATURE, 14.5, 15.0, 10.75, 11.0), temp[0, 5, 11], 1e-8);
            }
        }

        public void CellsToGridFallBack()
        {
            using (var ds = DataSet.Open("msds:memory"))
            {
                ds.AddAxisCells("Lat", "Degrees", 12.0f, 15.0f, 0.5f); //here is cells
                ds.AddAxis("Lon", "Degrees", 8.0f, 11.0f, 0.25f); //here is grid
                ds.AddClimatologyAxisYearly(yearStep: 30);
                ds.Fetch(ClimateParameter.FC_TEMPERATURE, "air0");
                ds.Commit();
                double[, ,] temp = (double[, ,])ds.Variables["air0"].GetData();
                Assert.AreEqual(ClimateService.FetchClimate(ClimateParameter.FC_TEMPERATURE, 12.0, 12.0, 8.0, 8.0), temp[0, 0, 0], 1e-8);
                Assert.AreEqual(ClimateService.FetchClimate(ClimateParameter.FC_TEMPERATURE, 13.5, 13.5, 9.25, 9.25), temp[0, 3, 5], 1e-8);
                Assert.AreEqual(ClimateService.FetchClimate(ClimateParameter.FC_TEMPERATURE, 15.0, 15.0, 11.0, 11.0), temp[0, 6, 12], 1e-8);
            }
        }

        [TestMethod]
        [TestCategory("SDS APIv1")]
        [TestCategory("Uses remote Cloud deployment")]
        [ExpectedException(typeof(InvalidOperationException))]
        public void FetchNoTime()
        {
            using (var ds = DataSet.Open("msds:memory"))
            {
                ds.AddAxisCells("Lat", "Degrees", 12.0, 15.0, 0.5);
                ds.AddAxisCells("Lon", "Degrees", 8.0, 11.0, 0.25);
                ds.Commit();
                ds.Fetch(ClimateParameter.FC_TEMPERATURE, "air0");
                ds.Commit();
            }
        }

        [TestMethod]
        [TestCategory("SDS APIv1")]
        [TestCategory("Uses remote Cloud deployment")]
        [ExpectedException(typeof(InvalidOperationException))]
        public void FetchNoLon()
        {
            using (var ds = DataSet.Open("msds:memory"))
            {
                ds.AddAxisCells("Lat", "Degrees", 12.0, 15.0, 0.5);
                ds.AddClimatologyAxisSeasonly(dayStep: 30);
                ds.Commit();
                ds.Fetch(ClimateParameter.FC_TEMPERATURE, "air0");
                ds.Commit();
            }
        }

        [TestMethod]
        [TestCategory("SDS APIv1")]
        [TestCategory("Uses remote Cloud deployment")]
        [ExpectedException(typeof(InvalidOperationException))]
        public void FetchEmptyDs()
        {
            using (var ds = DataSet.Open("msds:memory"))
            {
                ds.Fetch(ClimateParameter.FC_TEMPERATURE, "air0");
                ds.Commit();
            }
        }

        [TestMethod]
        [TestCategory("SDS APIv1")]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchClimatologyGridTest()
        {
            var ds = DataSet.Open("msds:memory");

            ds.AddClimatologyAxisSeasonly(dayStep: 30);

            ds.AddAxis("Lat", "Degrees North", 10, 30, 2);
            ds.AddAxis("Lon", "Degrees East", 15, 35, 5);

            ds.Fetch(ClimateParameter.FC_LAND_WIND_SPEED, "wnd");
            Assert.IsTrue(ds.Variables.Contains("wnd"));
            Assert.AreEqual(3, ds.Variables["wnd"].Dimensions.Count);
            Assert.AreEqual(4, ds.Dimensions.Count);
        }

        [TestMethod]
        [TestCategory("SDS APIv1")]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTimeSlicesPointSetTest()
        {
            // Fetching climate parameters for fixed time moment
            using (var ds = DataSet.Open("msds:memory"))
            {
                Random r = new Random(4235234);
                double[] lats = Enumerable.Repeat<int>(0, 200).Select(a => r.NextDouble() * 180.0 - 90.0).ToArray();
                double[] lons = Enumerable.Repeat<int>(0, 200).Select(a => r.NextDouble() * 360.0).ToArray();

                ds.Add("Lat", lats, "points");
                ds.Add("Lon", lons, "points");

                ds.AddAxis("lon", "degrees East", 0, 20.0, 2);
                ds.AddAxis("lat", "degrees North", 0, 10.0, 2);

                var v = ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt", new DateTime(2000, 7, 19, 11, 0, 0));
                FetchTest_Check(v, new int[] { 6, 11 });
                v = ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt2", ds["lat"].ID, ds["lon"].ID, new DateTime(2000, 7, 19, 11, 0, 0));
                FetchTest_Check(v, new int[] { 6, 11 });
                v = ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt3", "lat", "lon", new DateTime(2000, 7, 19, 11, 0, 0));
                FetchTest_Check(v, new int[] { 6, 11 });


                ds.AddAxis("time", new DateTime(2000, 7, 19, 0, 0, 0), new DateTime(2000, 7, 19, 23, 0, 0), TimeSpan.FromHours(2));

                v = ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt4");
                FetchTest_Check(v, new int[] { 12, 6, 11 });
                v = ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt5", ds["lat"].ID, ds["lon"].ID, ds["time"].ID);
                FetchTest_Check(v, new int[] { 12, 6, 11 });
                v = ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt6", "lat", "lon", "time");
                FetchTest_Check(v, new int[] { 12, 6, 11 });
            }
        }

        [TestMethod]
        [TestCategory("SDS APIv1")]
        [TestCategory("Uses remote Cloud deployment")]
        public void FetchTimeSlicesGridTest()
        {
            // Fetching climate parameters for fixed time moment
            using (var ds = DataSet.Open("msds:memory"))
            {
                ds.AddAxis("lon", "degrees East", 0, 20.0, 2);
                ds.AddAxis("lat", "degrees North", 0, 10.0, 2);

                var v = ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt", new DateTime(2000, 7, 19, 11, 0, 0));
                FetchTest_Check(v, new int[] { 6, 11 });
                v = ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt2", ds["lat"].ID, ds["lon"].ID, new DateTime(2000, 7, 19, 11, 0, 0));
                FetchTest_Check(v, new int[] { 6, 11 });
                v = ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt3", "lat", "lon", new DateTime(2000, 7, 19, 11, 0, 0));
                FetchTest_Check(v, new int[] { 6, 11 });


                ds.AddAxis("time", new DateTime(2000, 7, 19, 0, 0, 0), new DateTime(2000, 7, 19, 23, 0, 0), TimeSpan.FromHours(2));

                v = ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt4");
                FetchTest_Check(v, new int[] { 12, 6, 11 });
                v = ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt5", ds["lat"].ID, ds["lon"].ID, ds["time"].ID);
                FetchTest_Check(v, new int[] { 12, 6, 11 });
                v = ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt6", "lat", "lon", "time");
                FetchTest_Check(v, new int[] { 12, 6, 11 });
            }
        }


        private void FetchTest_Check(Variable v, int[] shape)
        {
            Assert.IsFalse(v.DataSet.HasChanges);
            Assert.IsTrue(v.DataSet.IsAutocommitEnabled);
            Assert.AreEqual(shape.Length, v.Rank);
            for (int i = 0; i < v.Rank; i++)
                Assert.AreEqual(shape[i], v.GetShape()[i]);

            var data = v.GetData();
            int[] index = new int[v.Rank];
            for (int i = 0; i < data.Length; i++)
            {
                Assert.IsTrue(((double)data.GetValue(index)) > 0);
                for (int j = v.Rank; --j >= 0; )
                {
                    index[j]++;
                    if (index[j] < shape[j]) break;
                    index[j] = 0;
                }
            }

            Assert.IsNotNull(v.MissingValue);
            Assert.IsNotNull(v.Metadata["Units", SchemaVersion.Recent]);
            Assert.IsNotNull(v.Metadata["Name", SchemaVersion.Recent]);
            Assert.IsNotNull(v.Metadata["long_name", SchemaVersion.Recent]);
        }
    }
}
