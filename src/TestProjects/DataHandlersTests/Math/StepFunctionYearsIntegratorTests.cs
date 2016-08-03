using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Research.Science.FetchClimate2.Integrators.Temporal;
using Microsoft.Research.Science.FetchClimate2.Tests;

namespace Microsoft.Research.Science.FetchClimate2.DataHandlersTests.Math
{
    [TestClass]
    public class StepFunctionYearsIntegratorTests
    {
        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void TestProjections()
        {
            int[] data = new int[] {0,1,2,3,4,5,6,7};
            DateTime baseTime = new DateTime(2009,1,1);
            var handler = new TimeAxisAvgProcessing.TimeAxisAvgFacade(
                data,
                new TimeAxisProjections.ContinousYears(baseTime.Year),
                new WeightProviders.StepFunctionInterpolation(),
                new DataCoverageEvaluators.ContinousMeansCoverageEvaluator());
            var res1 = handler.GetTempIPs(new TimeSegment(2009,2009,1,365,0,24));
            Assert.AreEqual(1, res1.Weights.Length);
            Assert.AreEqual(1.0, res1.Weights[0]);
            Assert.AreEqual(0, res1.Indices[0]);

            res1 = handler.GetTempIPs(new TimeSegment(2015, 2015, 1, 365, 0, 24));
            Assert.AreEqual(1, res1.Weights.Length);
            Assert.AreEqual(1.0, res1.Weights[0]);
            Assert.AreEqual(6, res1.Indices[0]);

            res1 = handler.GetTempIPs(new TimeSegment(2016, 2016, 1, 365, 0, 24));
            Assert.AreEqual(0, res1.Weights.Length);            
        }
    }
}
