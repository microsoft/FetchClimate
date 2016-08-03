using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Research.Science.FetchClimate2.Tests
{
    [TestClass]
    public class SphereMathTests
    {
        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void GreatCircleDistanceTest()            
        {
            Assert.AreEqual(System.Math.PI*0.5,SphereMath.GetDistance(0.0,90.0,90.0,23.4,1.0),1e-14);

            Assert.AreEqual(System.Math.PI * 0.5, SphereMath.GetDistance(0.0, 0.0,0.0, 90.0, 1.0), 1e-14);
            Assert.AreEqual(System.Math.PI * 0.5, SphereMath.GetDistance(0.0, 0.0, 0.0, -90.0, 1.0), 1e-14);
            Assert.AreEqual(System.Math.PI * 0.5, SphereMath.GetDistance(0.0, 180.0, 0.0, 90.0, 1.0), 1e-14);
            Assert.AreEqual(System.Math.PI * 0.5, SphereMath.GetDistance(0.0, 180.0, 0.0, -90.0, 1.0), 1e-14);
            Assert.AreEqual(System.Math.PI * 0.5, SphereMath.GetDistance(0.0, 360.0, 0.0, 90.0, 1.0), 1e-14);
            Assert.AreEqual(System.Math.PI * 0.5, SphereMath.GetDistance(0.0, 360.0, 0.0, -90.0, 1.0), 1e-14);
        }
    }
}
