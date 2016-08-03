using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Research.Science.FetchClimate2.Tests.Utilities
{
    [TestClass]
    public class FetchRequestTest
    {
        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void TestValidRequest()
        {            
            TimeRegion tr = new TimeRegion();
            FetchDomain fd = FetchDomain.CreatePointGrid(new double[] { 0, 1, 2 }, new double[] { 3, 4, 5 }, tr);            
            string error;
            Assert.IsTrue(fd.IsContentValid(out error));
            Assert.IsTrue(string.IsNullOrEmpty(error));

            
            fd = FetchDomain.CreatePoints(new double[] { 0, 1, -2 }, new double[] { 3, 4, 5 }, tr); //points allow mixed axis            

            Assert.IsTrue(fd.IsContentValid(out error));
            Assert.IsTrue(string.IsNullOrEmpty(error));
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void TestInValidAxisRequest()
        {
            TimeRegion tr = new TimeRegion();
            FetchDomain fd = FetchDomain.CreatePointGrid(new double[] { 0, 1, -2 }, new double[] { 3, 4, 5 }, tr);                        
            string error;
            Assert.IsFalse(fd.IsContentValid(out error));
            Assert.IsFalse(string.IsNullOrEmpty(error));
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void TestInValidAxisRangeRequest()
        {
            TimeRegion tr = new TimeRegion();
            FetchDomain fd = FetchDomain.CreatePointGrid(new double[] { 0, 1, 2678 }, new double[] { 3, 4, 5 }, tr);            
            string error;
            Assert.IsFalse(fd.IsContentValid(out error));
            Assert.IsFalse(string.IsNullOrEmpty(error));
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void TestInValidtimeRequest()
        {
            TimeRegion tr = new TimeRegion(lastDay:567).GetYearlyTimeseries();
            FetchDomain fd = FetchDomain.CreatePointGrid(new double[] { 0, 1, 2 }, new double[] { 3, 4, 5 }, tr);            
            string error;
            Assert.IsFalse(fd.IsContentValid(out error));
            Assert.IsFalse(string.IsNullOrEmpty(error));
        }
    }
}
