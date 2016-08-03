using Microsoft.Research.Science.FetchClimate2.Integrators;
using Microsoft.Research.Science.FetchClimate2.Integrators.Temporal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Tests.Integrators
{
    [TestClass]
    public class ContinuousDaysStepIntegrator360Tests
    {        
        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void ContinuousDaysStepIntegrator360GetTempIps()
        {
            double[] axis = new double[] { 14400, 14430, 14460, 14490,14520,14550,14580,14610 };
            ContinousDaysAxisIntegrator360<StepFunctionWeightsProvider, ContinousMeansCoverageEvaluator> mmsi = new ContinousDaysAxisIntegrator360<StepFunctionWeightsProvider, ContinousMeansCoverageEvaluator>(axis, 1960, 1);

            IPs b = mmsi.GetTempIPs(new TimeSegment(2000, 2000, 153 /*1 june*/, 213 /*31 jul*/, 0, 24));            
            Assert.AreEqual(5, b.Indices[0]);
            Assert.AreEqual(6, b.Indices[1]);
            Assert.AreEqual(b.Weights[0], b.Weights[1],TestConstants.DoublePrecision);            
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void ContinuousDaysStepIntegrator360GetBoundingBox()
        {
            double[] axis = new double[] { 14400, 14430, 14460, 14490, 14520, 14550, 14580, 14610 };
            ContinousDaysAxisIntegrator360<StepFunctionWeightsProvider, ContinousMeansCoverageEvaluator> mmsi = new ContinousDaysAxisIntegrator360<StepFunctionWeightsProvider, ContinousMeansCoverageEvaluator>(axis, 1960, 1);

            IndexBoundingBox bb = mmsi.GetBoundingBox(new TimeSegment(2000,2000,153 /*1 june*/,213 /*31 jul*/,0,24));
            
            Assert.AreEqual(5, bb.first);
            Assert.AreEqual(6, bb.last);

            bb = mmsi.GetBoundingBox(new TimeSegment(2000,2000,135 /*15 may*/,166 /*15 june*/,0,24));
            
            Assert.AreEqual(4, bb.first);
            Assert.AreEqual(5, bb.last);
        }

    }
}
