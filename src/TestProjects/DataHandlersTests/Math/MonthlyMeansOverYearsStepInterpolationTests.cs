using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Microsoft.Research.Science.FetchClimate2.Integrators.Temporal;

namespace Microsoft.Research.Science.FetchClimate2.Tests
{
    [TestClass]
    public class MonthlyMeansOverYearsStepInterpolationTests
    {
        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void TestProjectFirstDay()
        {
            Assert.AreEqual(0.0, DaysOfYearConversions.ProjectFirstDay(1, true));//1 Jenuary
            Assert.AreEqual(0.0, DaysOfYearConversions.ProjectFirstDay(1, false));
            Assert.AreEqual(1.0, DaysOfYearConversions.ProjectFirstDay(32, true));//1 February
            Assert.AreEqual(1.0, DaysOfYearConversions.ProjectFirstDay(32, false));
            Assert.AreEqual(3.5, DaysOfYearConversions.ProjectFirstDay(107, true));//16 april (exact start of the second half of april)
            Assert.AreEqual(3.5, DaysOfYearConversions.ProjectFirstDay(106, false));
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void TestProjectLastDay()
        {
            Assert.AreEqual(1.0, DaysOfYearConversions.ProjectLastDay(31, true));//31 Jenuary
            Assert.AreEqual(1.0, DaysOfYearConversions.ProjectLastDay(31, false));

            Assert.AreEqual(2.0, DaysOfYearConversions.ProjectLastDay(60, true));//29 February
            Assert.AreEqual(2.0, DaysOfYearConversions.ProjectLastDay(59, false));//28 February

            Assert.AreEqual(3.5, DaysOfYearConversions.ProjectLastDay(106, true));//15 april (exact end of the first half of april)
            Assert.AreEqual(3.5, DaysOfYearConversions.ProjectLastDay(105, false));

            Assert.AreEqual(12.0, DaysOfYearConversions.ProjectLastDay(365, false));
            Assert.AreEqual(12.0, DaysOfYearConversions.ProjectLastDay(366, true));

            Assert.AreEqual(15.5, DaysOfYearConversions.ProjectLastDay(470, false)); //overlapped 15 april
            Assert.AreEqual(23.0, DaysOfYearConversions.ProjectLastDay(699, false)); //overlapped 30 november
        }        
    }


}
