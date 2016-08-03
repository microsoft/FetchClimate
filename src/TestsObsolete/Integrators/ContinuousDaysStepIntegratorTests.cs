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
    public class ContinuousDaysStepIntegratorTests
    {
        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void ContinuousDaysStepIntegratorGetTempIps()
        {
            double[] axis = new double[] {0,31,61,92};
            DaysAxisStepIntegrator mmsi = new DaysAxisStepIntegrator(axis, new DateTime(2001, 5, 1)); //121 - 1 may 

            IPs b = mmsi.GetTempIPs(new TimeSegment(2001,2001,152 /*1 june*/,212 /*31 jul*/,0,24));
            
            Assert.AreEqual(1, b.Indices[0]);
            Assert.AreEqual(2, b.Indices[1]);
            Assert.AreEqual(30.0/31.0,b.Weights[0]/b.Weights[1],TestConstants.DoublePrecision); // weights must be proportional to the lengths of the months

            b = mmsi.GetTempIPs(new TimeSegment(2001, 2001, 167 /*15 june*/, 196 /*15 jul*/, 0, 24));
            
            Assert.AreEqual(1, b.Indices[0]);
            Assert.AreEqual(2, b.Indices[1]);
            Assert.AreEqual(b.Weights[0],b.Weights[1], TestConstants.DoublePrecision); // weights must be the same, as time intervals are exactly 15 days long in each month
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void ContinuousDaysStepIntegratorGetBoundingBox()
        {
            double[] axis = new double[] { 0, 31, 61, 92 };
            DaysAxisStepIntegrator mmsi = new DaysAxisStepIntegrator(axis, new DateTime(2001, 5, 1)); //121 - 1 may 

            IndexBoundingBox bb = mmsi.GetBoundingBox(new TimeSegment(2001,2001,152 /*1 june*/,212 /*31 jul*/, 0,24 ));
            
            Assert.AreEqual(1, bb.first);
            Assert.AreEqual(2, bb.last);

            bb= mmsi.GetBoundingBox(new TimeSegment(2001,2001,135 /*15 may*/,166 /*15 june*/, 0, 24 ));
            
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(1, bb.last);
        }

    }
}
