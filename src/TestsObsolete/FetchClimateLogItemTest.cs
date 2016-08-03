using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Research.Science.FetchClimate2.Diagnostics;

namespace Microsoft.Research.Science.FetchClimate2.Tests
{
    /// <summary>
    /// Summary description for FetchClimateLogItemTest
    /// </summary>
    [TestClass]
    public class FetchClimateLogItemTest
    {
        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void ParseItemWithHash()
        {
            WADRecord record = new WADRecord
            {
                EventTickCount = 634012319404982640L,
                Message = "1234567890abcdef987654321:1:20: sample message: hello",
                RoleInstance = "FetchWorker1_IN_3"
            };
            FetchClimateLogItem item = new FetchClimateLogItem(record);
            Assert.IsTrue(item.Hash == "1234567890abcdef987654321");
            Assert.IsTrue(item.PartCount == 20);
            Assert.IsTrue(item.PartNo == 1);
            Assert.IsTrue(item.Message == " sample message: hello");
            Assert.IsTrue(item.Instance == "FetchWorker1_IN_3");
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void ParseItemWithoutHash()
        {
            WADRecord record = new WADRecord
            {
                EventTickCount = 634012319404982640L,
                Message = "sample message: hello",
                RoleInstance = "FetchWorker1_IN_3"
            };
            FetchClimateLogItem item = new FetchClimateLogItem(record);
            Assert.IsTrue(item.Hash == null);
            Assert.IsTrue(item.PartCount == -1);
            Assert.IsTrue(item.PartNo == -1);
            Assert.IsTrue(item.Message == "sample message: hello");
            Assert.IsTrue(item.Instance == "FetchWorker1_IN_3");
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void ParseItemWithoutParts()
        {
            WADRecord record = new WADRecord
            {
                EventTickCount = 634012319404982640L,
                Message = "1234567890abcdef987654321: sample message: hello",
                RoleInstance = "FetchWorker1_IN_3"
            };
            FetchClimateLogItem item = new FetchClimateLogItem(record);
            Assert.IsTrue(item.Hash == "1234567890abcdef987654321");
            Assert.IsTrue(item.PartCount == -1);
            Assert.IsTrue(item.PartNo == -1);
            Assert.IsTrue(item.Message == " sample message: hello");
            Assert.IsTrue(item.Instance == "FetchWorker1_IN_3");
        }
    }
}
