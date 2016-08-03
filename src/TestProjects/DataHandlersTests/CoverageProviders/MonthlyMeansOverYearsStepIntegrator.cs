using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Research.Science.FetchClimate2;
using Microsoft.Research.Science.FetchClimate2.Integrators.Temporal;
using Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators;

namespace Microsoft.Research.Science.FetchClimate2.DataHandlersTests.CoverageProviders
{
    [TestClass]
    public class MonthlyMeansOverYearsStepIntegratorFacadeTest
    {
        [TestCategory("Local")]
        [TestCategory("BVT")]
        [TestMethod]
        public void LongTermMonthlyMeans()
        {
            ITimeAxisAvgProcessing a = new TimeAxisAvgProcessing.MonthlyMeansOverYearsStepIntegratorFacade(); //the provider only checks mongth alignment
            ITimeSegment t = new TimeRegionExtensions.TimeSegment(2000, 2001, 1, 31, 0, 24);
            var c = a.GetCoverage(t);

            Assert.AreEqual(DataCoverageResult.DataWithUncertainty, c);

            t = new TimeRegionExtensions.TimeSegment(2000, 2001, 1, 38, 0, 24);
            c = a.GetCoverage(t);

            Assert.AreEqual(DataCoverageResult.DataWithoutUncertainty, c);
        }

        [TestCategory("Local")]
        [TestCategory("BVT")]
        [TestMethod]
        public void LongTermMonthlyMeansWithExactYears()
        {
            ITimeAxisAvgProcessing a = new TimeAxisAvgProcessing.MonthlyMeansOverExactYearsStepIntegratorFacade(1961, 1990); //the provider checks mongth alignment AND exact years match
            ITimeSegment t = new TimeRegionExtensions.TimeSegment(2000, 2001, 1, 31, 0, 24); 
            var c = a.GetCoverage(t);
            Assert.AreEqual(DataCoverageResult.DataWithoutUncertainty, c); //years do not match

            t = new TimeRegionExtensions.TimeSegment(2000, 2001, 1, 38, 0, 24); //years do not match and time is not  aligned to month bounds
            c = a.GetCoverage(t);
            Assert.AreEqual(DataCoverageResult.DataWithoutUncertainty, c);

            t = new TimeRegionExtensions.TimeSegment(1961, 1990, 1, 38, 0, 24); //years match but time is not aligned to month bounds
            c = a.GetCoverage(t);
            Assert.AreEqual(DataCoverageResult.DataWithoutUncertainty, c);

            t = new TimeRegionExtensions.TimeSegment(1961, 1990, 1, 31, 0, 24); //years match and time is aligned to month bounds
            c = a.GetCoverage(t);
            Assert.AreEqual(DataCoverageResult.DataWithUncertainty, c);
        }

        [TestCategory("Local")]
        [TestCategory("BVT")]
        [TestMethod]
        public void LongTermMonthlyMeansWithYearsCount()
        {
            ITimeAxisAvgProcessing a = new TimeAxisAvgProcessing.MonthlyMeansOverEnoughYearsStepIntegratorFacade(10); //the provider checks mongth alignment AND the count of years in requested t
            ITimeSegment t = new TimeRegionExtensions.TimeSegment(2000, 2001, 1, 31, 0, 24);
            var c = a.GetCoverage(t);
            Assert.AreEqual(DataCoverageResult.DataWithoutUncertainty, c); //to few years

            t = new TimeRegionExtensions.TimeSegment(2000, 2001, 1, 38, 0, 24); //to few years and time is not  aligned to month bounds
            c = a.GetCoverage(t);
            Assert.AreEqual(DataCoverageResult.DataWithoutUncertainty, c);

            t = new TimeRegionExtensions.TimeSegment(1961, 1990, 1, 38, 0, 24); //enough years but time is not aligned to month bounds
            c = a.GetCoverage(t);
            Assert.AreEqual(DataCoverageResult.DataWithoutUncertainty, c);

            t = new TimeRegionExtensions.TimeSegment(1961, 1990, 1, 31, 0, 24); //enough years and time is aligned to month bounds
            c = a.GetCoverage(t);
            Assert.AreEqual(DataCoverageResult.DataWithUncertainty, c);
        }
    }
}
