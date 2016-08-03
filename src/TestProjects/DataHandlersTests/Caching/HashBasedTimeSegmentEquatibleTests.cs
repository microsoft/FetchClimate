using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Research.Science.FetchClimate2.Utils;
using DataHandlersTests.Stubs;
using Microsoft.Research.Science.FetchClimate2;
using System.Collections.Generic;
using Microsoft.Research.Science.FetchClimate2.Tests;

namespace DataHandlersTests.Caching
{
    [TestClass]
    public class HashBasedTimeSegmentEquatibleTests
    {
        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        [ExpectedException(typeof(ArgumentException))]
        public void HashBasedTimeSegmentOnlyDuplicateTest()
        {
            var converter = new HashBasedTimeSegmentOnlyConverter();

            ITimeSegment ts1 = new TimeSegment(1800,1890,1,365,0,24);
            ITimeSegment ts2 = new TimeSegment(1800,1895,1,365,0,24);

            var c1 = new GeoCellStub() { LatMax = 1.0, LatMin =-1.0, LonMin = -2.0, LonMax =2.0, Time = ts1};
            var c2 = new GeoCellStub() { LatMax = 1.0, LatMin =-1.0, LonMin = -2.0, LonMax =2.0, Time = ts2};
            var c3 = new GeoCellStub() { LatMax = 1.0, LatMin =-1.0, LonMin = -3.0, LonMax =2.0, Time = ts1};
            var c4 = new GeoCellStub() { LatMax = 1.0, LatMin =-1.0, LonMin = -3.0, LonMax =2.0, Time = ts2};

            IGeoCell ec1 = converter.Covert(c1);
            IGeoCell ec2 = converter.Covert(c2);
            IGeoCell ec3 = converter.Covert(c3);
            IGeoCell ec4 = converter.Covert(c4);

            Dictionary<IGeoCell, int> d = new Dictionary<IGeoCell, int>();

            d.Add(ec1,1);
            d.Add(ec3, 2);

        }

    }
}
