using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Microsoft.Research.Science.FetchClimate2.Tests
{
    [TestClass]
    public class FetchRequestHashTests
    {
        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void HashTest()
        {
            TimeRegion tr = new TimeRegion(firstYear: 2000, lastYear: 2001).GetMonthlyTimeseries();
            FetchRequest request = new FetchRequest(
                "airt" ,
                FetchDomain.CreatePointGrid(
                    Enumerable.Range(0, 150).Select(i => 50.0 + i * 0.1).ToArray(), // 5.0 .. 20.0
                    Enumerable.Range(0, 170).Select(i => 30.0 + i * 0.1).ToArray(),
                    tr),
                new DateTime(2012, 11, 13), new string[] {"WorldClim 1.4"});

            FetchRequest request2 = new FetchRequest(
                "temp",
                FetchDomain.CreatePointGrid(
                    Enumerable.Range(0, 150).Select(i => 50.0 + i * 0.1).ToArray(), // 5.0 .. 20.0
                    Enumerable.Range(0, 170).Select(i => 30.0 + i * 0.1).ToArray(),
                    tr),
                new DateTime(2012, 11, 13), new string[] {"WorldClim 1.4"});

            FetchRequest request3 = new FetchRequest(
                "temp",
                FetchDomain.CreateCellGrid(
                    Enumerable.Range(0, 150).Select(i => 50.0 + i * 0.1).ToArray(), // 5.0 .. 20.0
                    Enumerable.Range(0, 170).Select(i => 30.0 + i * 0.1).ToArray(),
                    tr),
                new DateTime(2012, 11, 13), new string[] {"WorldClim 1.4"});

            Assert.IsTrue(request.GetSHAHash() != request2.GetSHAHash());
            Assert.IsTrue(request2.GetSHAHash() != request3.GetSHAHash());
        }
    }
}