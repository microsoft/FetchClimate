using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Research.Science.FetchClimate2.Integrators;
using Microsoft.Research.Science.FetchClimate2.DataCoverageEvaluators;

namespace DataHandlersTests.UncertatintyEvaluators
{
    [TestClass]
    public class CoverageEvaluators
    {
        [TestMethod]
        //[TestCategory("BVT")]
        [TestCategory("Local")]
        public void ContinousMeansCoverageEvaluatorTest()
        {
            ContinousMeansCoverageEvaluator cmce = new ContinousMeansCoverageEvaluator();
            double[] grid = new double[]{0.0,1.0,2.0,3.0};
            Assert.AreEqual(Microsoft.Research.Science.FetchClimate2.DataCoverageResult.DataWithUncertainty, cmce.EvaluateInterval(grid, 2.0, 3.0));
            Assert.AreEqual(Microsoft.Research.Science.FetchClimate2.DataCoverageResult.DataWithoutUncertainty, cmce.EvaluateInterval(grid, 2.5, 3.0));
            Assert.AreEqual(Microsoft.Research.Science.FetchClimate2.DataCoverageResult.OutOfData, cmce.EvaluateInterval(grid, 2.5, 6.0));
            Assert.AreEqual(Microsoft.Research.Science.FetchClimate2.DataCoverageResult.OutOfData, cmce.EvaluateInterval(grid, 5, 6.0));
            Assert.AreEqual(Microsoft.Research.Science.FetchClimate2.DataCoverageResult.OutOfData, cmce.EvaluateInterval(grid, -2.0, 1.0));
            Assert.AreEqual(Microsoft.Research.Science.FetchClimate2.DataCoverageResult.OutOfData, cmce.EvaluateInterval(grid, -5.0, -1.0));
        }
    }
}
