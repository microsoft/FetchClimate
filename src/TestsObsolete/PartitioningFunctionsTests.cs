using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FetchWorker;
using Frontend.Controllers;
using System.Linq;
using Microsoft.Research.Science.Data;

namespace Microsoft.Research.Science.FetchClimate2.Tests
{
    [TestClass]
    public class PartitioningFunctionsTests
    {
        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void SubrequestExtractionTest()
        {
            var tr = new TimeRegion(firstYear: 2000, lastYear: 2001).GetMonthlyTimeseries();
            var request = new FetchRequest(
                "pet",
                FetchDomain.CreatePointGrid(
                    Enumerable.Range(0, 150).Select(i => 5.0 + i * 0.1).ToArray(), // 5.0 .. 20.0
                    Enumerable.Range(0, 170).Select(i => 70.0 + i * 0.1).ToArray(),
                    tr),
                new DateTime(2013, 11, 17));
            //let's divide it into 31 part
            // 170 / 31 = 5
            // 170 % 31 = 15
            //so we gotta get 15 parts 6 points wide and 16 parts 5 points wide
            var answer1 = new FetchRequest(
                "pet",
                FetchDomain.CreatePointGrid(
                    Enumerable.Range(0, 150).Select(i => 5.0 + i * 0.1).ToArray(), // 5.0 .. 20.0
                    Enumerable.Range(0, 6).Select(i => 70.0 + i * 0.1).ToArray(),
                    tr),
                new DateTime(2013, 11, 17));
            var answer15 = new FetchRequest(
                "pet",
                FetchDomain.CreatePointGrid(
                    Enumerable.Range(0, 150).Select(i => 5.0 + i * 0.1).ToArray(), // 5.0 .. 20.0
                    Enumerable.Range(0, 6).Select(i => 78.4 + i * 0.1).ToArray(),
                    tr),
                new DateTime(2013, 11, 17));
            var answer16 = new FetchRequest(
                "pet",
                FetchDomain.CreatePointGrid(
                    Enumerable.Range(0, 150).Select(i => 5.0 + i * 0.1).ToArray(), // 5.0 .. 20.0
                    Enumerable.Range(0, 5).Select(i => 79.0 + i * 0.1).ToArray(),
                    tr),
                new DateTime(2013, 11, 17));
            var answer31 = new FetchRequest(
                "pet",
                FetchDomain.CreatePointGrid(
                    Enumerable.Range(0, 150).Select(i => 5.0 + i * 0.1).ToArray(), // 5.0 .. 20.0
                    Enumerable.Range(0, 5).Select(i => 86.5 + i * 0.1).ToArray(),
                    tr),
                new DateTime(2013, 11, 17));

            var guess1 = JobManager.EvaluateSubrequestData(request, 31, 0);
            var guess15 = JobManager.EvaluateSubrequestData(request, 31, 14);
            var guess16 = JobManager.EvaluateSubrequestData(request, 31, 15);
            var guess31 = JobManager.EvaluateSubrequestData(request, 31, 30);

            Assert.AreEqual(answer1.EnvironmentVariableName, guess1.Item1.EnvironmentVariableName);
            Assert.AreEqual(answer1.ParticularDataSource, guess1.Item1.ParticularDataSource);
            Assert.AreEqual(answer1.ReproducibilityTimestamp, guess1.Item1.ReproducibilityTimestamp);
            Assert.AreEqual(answer1.Domain.SpatialRegionType, guess1.Item1.Domain.SpatialRegionType);
            Assert.AreEqual(answer15.EnvironmentVariableName, guess15.Item1.EnvironmentVariableName);
            Assert.AreEqual(answer15.ParticularDataSource, guess15.Item1.ParticularDataSource);
            Assert.AreEqual(answer15.ReproducibilityTimestamp, guess15.Item1.ReproducibilityTimestamp);
            Assert.AreEqual(answer15.Domain.SpatialRegionType, guess15.Item1.Domain.SpatialRegionType);
            Assert.AreEqual(answer16.EnvironmentVariableName, guess16.Item1.EnvironmentVariableName);
            Assert.AreEqual(answer16.ParticularDataSource, guess16.Item1.ParticularDataSource);
            Assert.AreEqual(answer16.ReproducibilityTimestamp, guess16.Item1.ReproducibilityTimestamp);
            Assert.AreEqual(answer16.Domain.SpatialRegionType, guess16.Item1.Domain.SpatialRegionType);
            Assert.AreEqual(answer31.EnvironmentVariableName, guess31.Item1.EnvironmentVariableName);
            Assert.AreEqual(answer31.ParticularDataSource, guess31.Item1.ParticularDataSource);
            Assert.AreEqual(answer31.ReproducibilityTimestamp, guess31.Item1.ReproducibilityTimestamp);
            Assert.AreEqual(answer31.Domain.SpatialRegionType, guess31.Item1.Domain.SpatialRegionType);
            for (int i = 0; i < answer1.Domain.Lats.Length; ++i) Assert.AreEqual(answer1.Domain.Lats[i], guess1.Item1.Domain.Lats[i], TestConstants.DoublePrecision);
            for (int i = 0; i < answer1.Domain.Lons.Length; ++i) Assert.AreEqual(answer1.Domain.Lons[i], guess1.Item1.Domain.Lons[i], TestConstants.DoublePrecision);
            for (int i = 0; i < answer15.Domain.Lats.Length; ++i) Assert.AreEqual(answer15.Domain.Lats[i], guess15.Item1.Domain.Lats[i], TestConstants.DoublePrecision);
            for (int i = 0; i < answer15.Domain.Lons.Length; ++i) Assert.AreEqual(answer15.Domain.Lons[i], guess15.Item1.Domain.Lons[i], TestConstants.DoublePrecision);
            for (int i = 0; i < answer16.Domain.Lats.Length; ++i) Assert.AreEqual(answer16.Domain.Lats[i], guess16.Item1.Domain.Lats[i], TestConstants.DoublePrecision);
            for (int i = 0; i < answer16.Domain.Lons.Length; ++i) Assert.AreEqual(answer16.Domain.Lons[i], guess16.Item1.Domain.Lons[i], TestConstants.DoublePrecision);
            for (int i = 0; i < answer31.Domain.Lats.Length; ++i) Assert.AreEqual(answer31.Domain.Lats[i], guess31.Item1.Domain.Lats[i], TestConstants.DoublePrecision);
            for (int i = 0; i < answer31.Domain.Lons.Length; ++i) Assert.AreEqual(answer31.Domain.Lons[i], guess31.Item1.Domain.Lons[i], TestConstants.DoublePrecision);

            Assert.AreEqual(0, guess1.Item2[0]);
            Assert.AreEqual(84, guess15.Item2[0]);
            Assert.AreEqual(90, guess16.Item2[0]);
            Assert.AreEqual(165, guess31.Item2[0]);

            var request2 = new FetchRequest(
                "pet",
                FetchDomain.CreateCellGrid(
                    Enumerable.Range(0, 150).Select(i => 5.0 + i * 0.1).ToArray(), // 5.0 .. 20.0
                    Enumerable.Range(0, 171).Select(i => 70.0 + i * 0.1).ToArray(),
                    tr),
                new DateTime(2013, 11, 17));
            var answer115 = new FetchRequest(
                "pet",
                FetchDomain.CreatePointGrid(
                    Enumerable.Range(0, 150).Select(i => 5.0 + i * 0.1).ToArray(), // 5.0 .. 20.0
                    Enumerable.Range(0, 7).Select(i => 78.4 + i * 0.1).ToArray(),
                    tr),
                new DateTime(2013, 11, 17));
            var answer116 = new FetchRequest(
                "pet",
                FetchDomain.CreatePointGrid(
                    Enumerable.Range(0, 150).Select(i => 5.0 + i * 0.1).ToArray(), // 5.0 .. 20.0
                    Enumerable.Range(0, 6).Select(i => 79.0 + i * 0.1).ToArray(),
                    tr),
                new DateTime(2013, 11, 17));

            var guess115 = JobManager.EvaluateSubrequestData(request2, 31, 14);
            var guess116 = JobManager.EvaluateSubrequestData(request2, 31, 15);

            for (int i = 0; i < answer115.Domain.Lats.Length; ++i) Assert.AreEqual(answer115.Domain.Lats[i], guess115.Item1.Domain.Lats[i], TestConstants.DoublePrecision);
            for (int i = 0; i < answer115.Domain.Lons.Length; ++i) Assert.AreEqual(answer115.Domain.Lons[i], guess115.Item1.Domain.Lons[i], TestConstants.DoublePrecision);
            for (int i = 0; i < answer116.Domain.Lats.Length; ++i) Assert.AreEqual(answer116.Domain.Lats[i], guess116.Item1.Domain.Lats[i], TestConstants.DoublePrecision);
            for (int i = 0; i < answer116.Domain.Lons.Length; ++i) Assert.AreEqual(answer116.Domain.Lons[i], guess116.Item1.Domain.Lons[i], TestConstants.DoublePrecision);

            var tr2 = new TimeRegion(1990, 2000, 1, -1, 0, 24).GetYearlyTimeseries(1990, 2000);
            var request3 = new FetchRequest(
                "airt",
                FetchDomain.CreateCells(
                    new double[] { 3.0 },
                    new double[] { 78.0 },
                    new double[] { 23.0 },
                    new double[] { 99.0 },
                    tr2));

            var guess117 = JobManager.EvaluateSubrequestData(request3, 1, 0);

            for (int i = 0; i < request3.Domain.Lats.Length; ++i) Assert.AreEqual(request3.Domain.Lats[i], guess117.Item1.Domain.Lats[i], TestConstants.DoublePrecision);
            for (int i = 0; i < request3.Domain.Lons.Length; ++i) Assert.AreEqual(request3.Domain.Lons[i], guess117.Item1.Domain.Lons[i], TestConstants.DoublePrecision);
            for (int i = 0; i < request3.Domain.Lats2.Length; ++i) Assert.AreEqual(request3.Domain.Lats2[i], guess117.Item1.Domain.Lats2[i], TestConstants.DoublePrecision);
            for (int i = 0; i < request3.Domain.Lons2.Length; ++i) Assert.AreEqual(request3.Domain.Lons2[i], guess117.Item1.Domain.Lons2[i], TestConstants.DoublePrecision);
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void SingleDimensionalRequestPartitioningTest()
        {
            var tr = new TimeRegion(firstYear: 1990, lastYear: 2001);
            var request = new FetchRequest(
                "pet",
                FetchDomain.CreatePoints(
                    Enumerable.Range(0, 1500).Select(i => 5.0 + i * 0.1).ToArray(), // 5.0 .. 20.0
                    Enumerable.Range(0, 1500).Select(i => 70.0 + i * 0.1).ToArray(),
                    tr));

            var guess0 = JobManager.EvaluateSubrequestData(request, 3, 0);
            var guess1 = JobManager.EvaluateSubrequestData(request, 3, 1);
            var guess2 = JobManager.EvaluateSubrequestData(request, 3, 2);
            Assert.AreEqual(512, guess0.Item1.Domain.Lats.Length);
            Assert.AreEqual(512, guess1.Item1.Domain.Lats.Length);
            Assert.AreEqual(476, guess2.Item1.Domain.Lats.Length);
            Assert.AreEqual(512, guess0.Item1.Domain.Lons.Length);
            Assert.AreEqual(512, guess1.Item1.Domain.Lons.Length);
            Assert.AreEqual(476, guess2.Item1.Domain.Lons.Length);
            for (int i = 0; i < 512; ++i) Assert.AreEqual(request.Domain.Lats[i], guess0.Item1.Domain.Lats[i], TestConstants.DoublePrecision);
            for (int i = 0; i < 512; ++i) Assert.AreEqual(request.Domain.Lats[i + 512], guess1.Item1.Domain.Lats[i], TestConstants.DoublePrecision);
            for (int i = 0; i < 476; ++i) Assert.AreEqual(request.Domain.Lats[i + 1024], guess2.Item1.Domain.Lats[i], TestConstants.DoublePrecision);
            for (int i = 0; i < 512; ++i) Assert.AreEqual(request.Domain.Lons[i], guess0.Item1.Domain.Lons[i], TestConstants.DoublePrecision);
            for (int i = 0; i < 512; ++i) Assert.AreEqual(request.Domain.Lons[i + 512], guess1.Item1.Domain.Lons[i], TestConstants.DoublePrecision);
            for (int i = 0; i < 476; ++i) Assert.AreEqual(request.Domain.Lons[i + 1024], guess2.Item1.Domain.Lons[i], TestConstants.DoublePrecision);

            var request2 = new FetchRequest(
                "pet",
                FetchDomain.CreateCells(
                    Enumerable.Range(0, 1500).Select(i => 5.0 + i * 0.1).ToArray(),
                    Enumerable.Range(0, 1500).Select(i => 70.0 + i * 0.1).ToArray(),
                    Enumerable.Range(0, 1500).Select(i => 6.0 + i * 0.1).ToArray(),
                    Enumerable.Range(0, 1500).Select(i => 71.0 + i * 0.1).ToArray(),
                    tr));

            var guess10 = JobManager.EvaluateSubrequestData(request, 3, 0);
            var guess11 = JobManager.EvaluateSubrequestData(request, 3, 1);
            var guess12 = JobManager.EvaluateSubrequestData(request, 3, 2);
            Assert.AreEqual(512, guess10.Item1.Domain.Lats.Length);
            Assert.AreEqual(512, guess11.Item1.Domain.Lats.Length);
            Assert.AreEqual(476, guess12.Item1.Domain.Lats.Length);
            Assert.AreEqual(512, guess10.Item1.Domain.Lons.Length);
            Assert.AreEqual(512, guess11.Item1.Domain.Lons.Length);
            Assert.AreEqual(476, guess12.Item1.Domain.Lons.Length);
            for (int i = 0; i < 512; ++i) Assert.AreEqual(request2.Domain.Lats[i], guess10.Item1.Domain.Lats[i], TestConstants.DoublePrecision);
            for (int i = 0; i < 512; ++i) Assert.AreEqual(request2.Domain.Lats[i + 512], guess11.Item1.Domain.Lats[i], TestConstants.DoublePrecision);
            for (int i = 0; i < 476; ++i) Assert.AreEqual(request2.Domain.Lats[i + 1024], guess12.Item1.Domain.Lats[i], TestConstants.DoublePrecision);
            for (int i = 0; i < 512; ++i) Assert.AreEqual(request2.Domain.Lons[i], guess10.Item1.Domain.Lons[i], TestConstants.DoublePrecision);
            for (int i = 0; i < 512; ++i) Assert.AreEqual(request2.Domain.Lons[i + 512], guess11.Item1.Domain.Lons[i], TestConstants.DoublePrecision);
            for (int i = 0; i < 476; ++i) Assert.AreEqual(request2.Domain.Lons[i + 1024], guess12.Item1.Domain.Lons[i], TestConstants.DoublePrecision);
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void PartsCountingInSingleDimensionalRequestTest()
        {
            var tr = new TimeRegion(firstYear: 1990, lastYear: 2001);
            var request = new FetchRequest(
                "pet",
                FetchDomain.CreatePoints(
                    Enumerable.Range(0, 1500).Select(i => 5.0 + i * 0.1).ToArray(), // 5.0 .. 20.0
                    Enumerable.Range(0, 1500).Select(i => 70.0 + i * 0.1).ToArray(),
                    tr));

            // 1500 pts in this request
            Assert.AreEqual(3,  JobManager.GetPartitionsCount(request, 0, 10000, 20));
            Assert.AreEqual(1, JobManager.GetPartitionsCount(request, 1000, 10000, 310));

            var request2 = new FetchRequest(
                 "pet",
                 FetchDomain.CreateCells(
                     Enumerable.Range(0, 15000).Select(i => 5.0 + i * 0.1).ToArray(),
                     Enumerable.Range(0, 15000).Select(i => 70.0 + i * 0.1).ToArray(),
                     Enumerable.Range(0, 15000).Select(i => 6.0 + i * 0.1).ToArray(),
                     Enumerable.Range(0, 15000).Select(i => 71.0 + i * 0.1).ToArray(),
                     tr));
            //15000 cells
            Assert.AreEqual(30, JobManager.GetPartitionsCount(request2, 0, 10000, 102));
            Assert.AreEqual(15, JobManager.GetPartitionsCount(request2, 0, 10000, 20));
            Assert.AreEqual(15, JobManager.GetPartitionsCount(request2, 1000, 10000, 102));
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void PartsCountingTest()
        {
            var tr = new TimeRegion(firstYear: 2000, lastYear: 2001).GetMonthlyTimeseries();
            var request2 = new FetchRequest(
                "pet",
                FetchDomain.CreateCellGrid(
                    Enumerable.Range(0, 151).Select(i => 5.0 + i * 0.1).ToArray(), // 5.0 .. 20.0
                    Enumerable.Range(0, 171).Select(i => 70.0 + i * 0.1).ToArray(),
                    tr),
                new DateTime(2013, 11, 17));

            // 306000 pts in this request
            Assert.AreEqual(31, JobManager.GetPartitionsCount(request2, 1000, 10000));
            Assert.AreEqual(170, JobManager.GetPartitionsCount(request2, 1000, 10000, 310));
            Assert.AreEqual(100, JobManager.GetPartitionsCount(request2, 1000, 10000, 100));

            var request3 = new FetchRequest(
                "pet",
                FetchDomain.CreatePointGrid(
                    Enumerable.Range(0, 50).Select(i => 5.0 + i * 0.1).ToArray(), // 5.0 .. 20.0
                    Enumerable.Range(0, 170).Select(i => 70.0 + i * 0.1).ToArray(),
                    tr),
                new DateTime(2013, 11, 17));
            //102000 pts
            Assert.AreEqual(102, JobManager.GetPartitionsCount(request3, 1000, 10000, 102));
        }
        /// <summary>
        /// Request yields latitude and longitude arrays of different length during extracting subpart.
        /// </summary>
        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void Bug1538()
        {
            var tr = new TimeRegion(1990, 2000, 1, 358).GetYearlyTimeseries(1990, 2000, 1, true).GetSeasonlyTimeseries(1, 358, 1, true);
            var r = new FetchRequest(
                "airt",
                FetchDomain.CreatePoints(
                    new double[] { 50, 52, 54 },
                    new double[] { 40, 42, 38 },
                    tr), new string[] { "CRU CL 2.0" });

            var request=JobManager.EvaluateSubrequestData(r, 3, 0);
            Assert.AreEqual(request.Item1.Domain.Lats.Length, request.Item1.Domain.Lons.Length);
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        [DeploymentItem(@"Data\423b28bf6a4357f14b64f2b16ab759cb6b5961db.csv")]
        public void MarksRequestTest()
        {
            double[] latmin = null, latmax, lonmin, lonmax = null;
            int[] startday, stopday, startyear, stopyear, starthour, stophour;

            FetchRequest fr = null;
            using (DataSet ds = DataSet.Open(@"423b28bf6a4357f14b64f2b16ab759cb6b5961db.csv?openMode=readOnly"))
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
                fr = new FetchRequest("prate", FetchDomain.CreatePoints(latmin, lonmax, tr), new string[] { "CRU CL 2.0" });
            }


            var request = JobManager.EvaluateSubrequestData(fr, 18, 16);            
        }
           
    }
}
