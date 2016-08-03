using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Research.Science.FetchClimate2;
using Microsoft.Research.Science.FetchClimate2.Integrators.Temporal;

namespace DataHandlersTests.CoverageProviders
{
    [TestClass]
    public class MonthlyMeansOverYearsStepIntegratorFacadeTest
    {
        [TestCategory("Local")]
        [TestCategory("BVT")]
        [TestMethod]
        public void TestYearsRange()
        {
            var a = new MonthlyMeansOverYearsStepIntegratorFacade(2000, 2005);
            var t = new TimeRegionExtensions.TimeSegment(2000,2005,1,31,0,24);
            var c = a.GetCoverage(t);

            Assert.AreEqual(DataCoverageResult.DataWithUncertainty, c);
        }
    }
}
