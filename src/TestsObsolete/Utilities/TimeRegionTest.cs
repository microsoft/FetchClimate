using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Research.Science.FetchClimate2.Tests.Utilities
{
    [TestClass]
    public class TimeRegionTest
    {
        const int defaultFirstYear = 1961;
        const int defaultLastYear = 1990;

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void TimeRegionFactoryMonthlyTimeseries()
        {
            var region = new TimeRegion().GetMonthlyTimeseries(firstMonth: 4, lastMonth: 6);
            var segments = region.GetSegments().ToArray();
            int[] firstDays = new int[] { 91, 121, 152 };
            int[] lastDays = new int[] { 120, 151, 181 };
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(defaultFirstYear, segments[i].FirstYear);
                Assert.AreEqual(defaultLastYear, segments[i].LastYear);
                Assert.AreEqual(firstDays[i], segments[i].FirstDay);
                Assert.AreEqual(lastDays[i], segments[i].LastDay);
                Assert.AreEqual(0, segments[i].StartHour);
                Assert.AreEqual(24, segments[i].StopHour);
            }

            region = new TimeRegion(firstYear: 1944, lastYear: 1944).GetMonthlyTimeseries(firstMonth: 4, lastMonth: 6);
            segments = region.GetSegments().ToArray();
            firstDays = new int[] { 91, 121, 152 };
            lastDays = new int[] { 120, 151, 181 };
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(1944, segments[i].FirstYear);
                Assert.AreEqual(1944, segments[i].LastYear);
                Assert.AreEqual(firstDays[i] + 1, segments[i].FirstDay);
                Assert.AreEqual(lastDays[i] + 1, segments[i].LastDay);
                Assert.AreEqual(0, segments[i].StartHour);
                Assert.AreEqual(24, segments[i].StopHour);
            }

            region = new TimeRegion(firstYear: 1946, lastYear: 1946).GetMonthlyTimeseries(firstMonth: 4, lastMonth: 6);
            segments = region.GetSegments().ToArray();
            firstDays = new int[] { 91, 121, 152 };
            lastDays = new int[] { 120, 151, 181 };
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(1946, segments[i].FirstYear);
                Assert.AreEqual(1946, segments[i].LastYear);
                Assert.AreEqual(firstDays[i], segments[i].FirstDay);
                Assert.AreEqual(lastDays[i], segments[i].LastDay);
                Assert.AreEqual(0, segments[i].StartHour);
                Assert.AreEqual(24, segments[i].StopHour);
            }
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void TimeRegionFactorySeasonlyTimeseries()
        {
            var region = new TimeRegion().GetSeasonlyTimeseries(firstDay: 10, lastDay: 40, stepLen: 10, isIntervalTimeseries: false);
            var segments = region.GetSegments().ToArray();
            int[] firstDay = new int[] { 10, 20, 30, 40 };
            int[] lastDay = new int[] { 10, 20, 30, 40 };
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(defaultFirstYear, segments[i].FirstYear);
                Assert.AreEqual(defaultLastYear, segments[i].LastYear);
                Assert.AreEqual(firstDay[i], segments[i].FirstDay);
                Assert.AreEqual(lastDay[i], segments[i].LastDay);
                Assert.AreEqual(0, segments[i].StartHour);
                Assert.AreEqual(24, segments[i].StopHour);
            }

            region = new TimeRegion().GetSeasonlyTimeseries(firstDay: 11, lastDay: 40, stepLen: 10, isIntervalTimeseries: true);
            segments = region.GetSegments().ToArray();
            firstDay = new int[] { 11, 21, 31 };
            lastDay = new int[] { 20, 30, 40 };
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(defaultFirstYear, segments[i].FirstYear);
                Assert.AreEqual(defaultLastYear, segments[i].LastYear);
                Assert.AreEqual(firstDay[i], segments[i].FirstDay);
                Assert.AreEqual(lastDay[i], segments[i].LastDay);
                Assert.AreEqual(0, segments[i].StartHour);
                Assert.AreEqual(24, segments[i].StopHour);
            }
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void TimeRegionFactoryYearlyTimeseries()
        {
            var region = new TimeRegion(firstDay: 11, lastDay: 40, startHour: 2, stopHour: 5).GetYearlyTimeseries(firstYear: 2000, lastYear: 2002, isIntervalTimeseries: true);
            var segments = region.GetSegments().ToArray();
            int[] firstYear = new int[] { 2000, 2001, 2002 };
            int[] lastYear = new int[] { 2000, 2001, 2002 };
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(firstYear[i], segments[i].FirstYear);
                Assert.AreEqual(lastYear[i], segments[i].LastYear);
                Assert.AreEqual(11, segments[i].FirstDay);
                Assert.AreEqual(40, segments[i].LastDay);
                Assert.AreEqual(2, segments[i].StartHour);
                Assert.AreEqual(5, segments[i].StopHour);
            }

            region = new TimeRegion(firstDay: 11, lastDay: 40, startHour: 2, stopHour: 5).GetYearlyTimeseries(firstYear: 2000, lastYear: 2002, isIntervalTimeseries: true);
            segments = region.GetSegments().ToArray();
            firstYear = new int[] { 2000, 2001, 2002 };
            lastYear = new int[] { 2000, 2001, 2002 };
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(firstYear[i], segments[i].FirstYear);
                Assert.AreEqual(lastYear[i], segments[i].LastYear);
                Assert.AreEqual(11, segments[i].FirstDay);
                Assert.AreEqual(40, segments[i].LastDay);
                Assert.AreEqual(2, segments[i].StartHour);
                Assert.AreEqual(5, segments[i].StopHour);
            }

            segments = region.GetYearlyTimeseries(firstYear: 2000, lastYear: 2010, stepLen: 2, isIntervalTimeseries: false).GetSegments().ToArray();
            firstYear = new int[] { 2000, 2002, 2004, 2006, 2008, 2010 };
            lastYear = new int[] { 2000, 2002, 2004, 2006, 2008, 2010 };
            for (int i = 0; i < 6; i++)
            {
                Assert.AreEqual(firstYear[i], segments[i].FirstYear);
                Assert.AreEqual(lastYear[i], segments[i].LastYear);
                Assert.AreEqual(11, segments[i].FirstDay);
                Assert.AreEqual(40, segments[i].LastDay);
                Assert.AreEqual(2, segments[i].StartHour);
                Assert.AreEqual(5, segments[i].StopHour);
            }

            segments = region.GetYearlyTimeseries(firstYear: 2000, lastYear: 2005, stepLen: 2, isIntervalTimeseries: true).GetSegments().ToArray();
            firstYear = new int[] { 2000, 2002, 2004 };
            lastYear = new int[] { 2001, 2003, 2005 };
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(firstYear[i], segments[i].FirstYear);
                Assert.AreEqual(lastYear[i], segments[i].LastYear);
                Assert.AreEqual(11, segments[i].FirstDay);
                Assert.AreEqual(40, segments[i].LastDay);
                Assert.AreEqual(2, segments[i].StartHour);
                Assert.AreEqual(5, segments[i].StopHour);
            }

            segments = region.GetYearlyTimeseries(firstYear: 2000, lastYear: 2004, stepLen: 2, isIntervalTimeseries: false).GetSegments().ToArray();
            firstYear = new int[] { 2000, 2002, 2004 };
            lastYear = new int[] { 2000, 2002, 2004 };
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(firstYear[i], segments[i].FirstYear);
                Assert.AreEqual(lastYear[i], segments[i].LastYear);
                Assert.AreEqual(11, segments[i].FirstDay);
                Assert.AreEqual(40, segments[i].LastDay);
                Assert.AreEqual(2, segments[i].StartHour);
                Assert.AreEqual(5, segments[i].StopHour);
            }
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void TimeRegionFactoryHourlyTimeseries()
        {
            var region = new TimeRegion();
            var segments = region.GetHourlyTimeseries(startHour: 0, stopHour: 3, stepLen: 1, isIntervalTimeseries: true).GetSegments().ToArray();
            int[] startHours = new int[] { 0, 1, 2 };
            int[] stopHours = new int[] { 1, 2, 3 };
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(defaultFirstYear, segments[i].FirstYear);
                Assert.AreEqual(defaultLastYear, segments[i].LastYear);
                Assert.AreEqual(1, segments[i].FirstDay);
                Assert.AreEqual(365, segments[i].LastDay);
                Assert.AreEqual(startHours[i], segments[i].StartHour);
                Assert.AreEqual(stopHours[i], segments[i].StopHour);
            }

            region = new TimeRegion().GetHourlyTimeseries(startHour: 0, stopHour: 3, stepLen: 1, isIntervalTimeseries: false);

            segments = region.GetSegments().ToArray();
            startHours = new int[] { 0, 1, 2, 3 };
            stopHours = new int[] { 0, 1, 2, 3 };
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(defaultFirstYear, segments[i].FirstYear);
                Assert.AreEqual(defaultLastYear, segments[i].LastYear);
                Assert.AreEqual(1, segments[i].FirstDay);
                Assert.AreEqual(365, segments[i].LastDay);
                Assert.AreEqual(startHours[i], segments[i].StartHour);
                Assert.AreEqual(stopHours[i], segments[i].StopHour);
            }
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void TimeRegionLeapYearHandling()
        {
            var a = new TimeRegion(firstYear: 1948, lastYear: 1948);
            Assert.AreEqual(367, a.Days[a.Days.Length - 1]);
            a = new TimeRegion(firstYear: 1949, lastYear: 1949);
            Assert.AreEqual(366, a.Days[a.Days.Length - 1]);
            a = new TimeRegion(firstYear: 1946, lastYear: 1949);
            Assert.AreEqual(366, a.Days[a.Days.Length - 1]);
        }

        /// <summary>
        /// Yearly time series for WorldClim produce oscillations
        /// </summary>
        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void Bug1611() {
            TimeRegion tr = new TimeRegion(lastDay:-1,isIntervalsGridDays:true).GetYearlyTimeseries(stepLen:1);            
            foreach (var seg in tr.GetSegments())
            {
                int validLastDay = DateTime.IsLeapYear(seg.FirstYear) ? 366 : 365;
                Assert.AreEqual(validLastDay,seg.LastDay);
            }
        }
    }
}
