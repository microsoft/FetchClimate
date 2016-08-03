using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Research.Science.Data;
using System.Linq;
using Microsoft.Research.Science.FetchClimate2.Integrators.Spatial;

namespace Microsoft.Research.Science.FetchClimate2.Tests
{
    [TestClass]
    public class LinearCycledLonsGridAggregatorTests
    {        
        private Array Build360Axis()
        {
            return Enumerable.Range(0, 360).Select(a => (double)a).ToArray();            
        }

        private Array Build180Axis()
        {
            return Enumerable.Range(-180,360).Select(a => (double)a).ToArray();            
        }

        private Array Build360AxisBoundariesRepeated()
        {
            return Enumerable.Range(0, 361).Select(a => (double)a).ToArray();
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void TestGetBoundingBoxCoord360()
        {            
            var lclga = new LinearCycledLonsAvgProcessing(Build360Axis(),false);
            
            var bb = lclga.GetBoundingBox(0.0);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(0, bb.last);
            
            bb = lclga.GetBoundingBox(0.3);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(1, bb.last);

            bb = lclga.GetBoundingBox(-0.3);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(359, bb.last);

            bb = lclga.GetBoundingBox(4.5);
            Assert.AreEqual(4, bb.first);
            Assert.AreEqual(5, bb.last);

            bb = lclga.GetBoundingBox(359);
            Assert.AreEqual(359, bb.first);
            Assert.AreEqual(359, bb.last);

            bb = lclga.GetBoundingBox(359.2);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(359, bb.last);

            bb = lclga.GetBoundingBox(-180);
            Assert.AreEqual(180, bb.first);
            Assert.AreEqual(180, bb.last);

            bb = lclga.GetBoundingBox(-178.3);
            Assert.AreEqual(181, bb.first);
            Assert.AreEqual(182, bb.last);
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void TestGetBoundingBoxInterval360()
        {            
            var lclga = new LinearCycledLonsAvgProcessing(Build360Axis(),false);

            var bb = lclga.GetBoundingBox(0.0,0.3);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(1, bb.last);

            bb = lclga.GetBoundingBox(0.3,1.0);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(1, bb.last);

            bb = lclga.GetBoundingBox(-0.3,0.0);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(359, bb.last);

            bb = lclga.GetBoundingBox(4.5,6.5);
            Assert.AreEqual(4, bb.first);
            Assert.AreEqual(7, bb.last);

            bb = lclga.GetBoundingBox(350,359);
            Assert.AreEqual(350, bb.first);
            Assert.AreEqual(359, bb.last);

            bb = lclga.GetBoundingBox(350.0,359.2);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(359, bb.last);

            bb = lclga.GetBoundingBox(-180,-170);
            Assert.AreEqual(180, bb.first);
            Assert.AreEqual(190, bb.last);

            bb = lclga.GetBoundingBox(-178.3,-178.0);
            Assert.AreEqual(181, bb.first);
            Assert.AreEqual(182, bb.last);

            bb = lclga.GetBoundingBox(179, -179.0);
            Assert.AreEqual(179, bb.first);
            Assert.AreEqual(181, bb.last);

            bb = lclga.GetBoundingBox(5, -5.0);
            Assert.AreEqual(5, bb.first);
            Assert.AreEqual(355, bb.last);
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void TestGetBoundingBoxCoord180()
        {
            var lclga = new LinearCycledLonsAvgProcessing(Build180Axis(),false);

            var bb = lclga.GetBoundingBox(-180.0);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(0, bb.last);

            bb = lclga.GetBoundingBox(-179.3);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(1, bb.last);

            bb = lclga.GetBoundingBox(179.5);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(359, bb.last);

            bb = lclga.GetBoundingBox(4.5);
            Assert.AreEqual(184, bb.first);
            Assert.AreEqual(185, bb.last);

            bb = lclga.GetBoundingBox(179);
            Assert.AreEqual(359, bb.first);
            Assert.AreEqual(359, bb.last);

            bb = lclga.GetBoundingBox(179.2);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(359, bb.last);

            bb = lclga.GetBoundingBox(360);
            Assert.AreEqual(180, bb.first);
            Assert.AreEqual(180, bb.last);

            bb = lclga.GetBoundingBox(183.3);
            Assert.AreEqual(3, bb.first);
            Assert.AreEqual(4, bb.last);            
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void TestGetBoundingBoxInterval180()
        {            
            var lclga = new LinearCycledLonsAvgProcessing(Build180Axis(),false);

            var bb = lclga.GetBoundingBox(-180.0, -179.4);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(1, bb.last);

            bb = lclga.GetBoundingBox(-179.4,-179.0);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(1, bb.last);

            bb = lclga.GetBoundingBox(179.3, 180.0);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(359, bb.last);

            bb = lclga.GetBoundingBox(4.5, 6.5);
            Assert.AreEqual(184, bb.first);
            Assert.AreEqual(187, bb.last);

            bb = lclga.GetBoundingBox(270, 275.4);
            Assert.AreEqual(90, bb.first);
            Assert.AreEqual(96, bb.last);

            bb = lclga.GetBoundingBox(350.0, 358.2);
            Assert.AreEqual(170, bb.first);
            Assert.AreEqual(179, bb.last);

            bb = lclga.GetBoundingBox(358.2, 358.0);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(359, bb.last);

            bb = lclga.GetBoundingBox(-180, -170);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(10, bb.last);

            bb = lclga.GetBoundingBox(179, 180);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(359, bb.last);

            bb = lclga.GetBoundingBox(179, -179.0);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(359, bb.last);

            bb = lclga.GetBoundingBox(5, -5.0);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(359, bb.last);
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void TestGetIPsForCell360()
        {
            var a = new LinearCycledLonsAvgProcessing(Build360Axis(),false);

            var ips = a.GetIPsForCell(4.0, 6.0);
            Assert.AreEqual(3,ips.Indices.Length);
            Assert.AreEqual(4, ips.Indices[0]);
            Assert.AreEqual(5, ips.Indices[1]);
            Assert.AreEqual(6, ips.Indices[2]);

            ips = a.GetIPsForCell(358.0, 360.0);
            Assert.AreEqual(3, ips.Indices.Length);
            Assert.AreEqual(358, ips.Indices[0]);
            Assert.AreEqual(359, ips.Indices[1]);
            Assert.AreEqual(0, ips.Indices[2]);

            ips = a.GetIPsForCell(-120.4, -120.2);
            Assert.AreEqual(2, ips.Indices.Length);
            Assert.AreEqual(239, ips.Indices[0]);
            Assert.AreEqual(240, ips.Indices[1]);

            ips = a.GetIPsForCell(10, 5);
            Assert.AreEqual(356, ips.Indices.Length);

            ips = a.GetIPsForCell(-0.2, -0.4);
            Assert.AreEqual(362, ips.Indices.Length);            
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void TestIndecesForRepeatedNodesAtBoundares()
        {
            var a = new LinearCycledLonsAvgProcessing(Build360AxisBoundariesRepeated(),true);
            var ips = a.GetIPsForPoint(3.5);
            Assert.AreEqual(2, ips.Indices.Length);
            Assert.AreEqual(3, ips.Indices[0]);
            Assert.AreEqual(4, ips.Indices[1]);

            ips = a.GetIPsForPoint(-0.5);
            Assert.AreEqual(2, ips.Indices.Length);
            Assert.AreEqual(359, ips.Indices[0]);
            Assert.IsTrue(0 == ips.Indices[1] || 360 == ips.Indices[1]);

            ips = a.GetIPsForPoint(-1.5);
            Assert.AreEqual(2, ips.Indices.Length);
            Assert.AreEqual(358, ips.Indices[0]);
            Assert.AreEqual(359, ips.Indices[1]);

            ips = a.GetIPsForCell(-0.5,0.5);
            Assert.AreEqual(3, ips.Indices.Length);
            Assert.AreEqual(359, ips.Indices[0]);
            Assert.IsTrue(0 == ips.Indices[1] || 360 == ips.Indices[1]);
            Assert.AreEqual(1, ips.Indices[2]);
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void TestGetIPsForCell180()
        {
            var a = new LinearCycledLonsAvgProcessing(Build180Axis(),false);

            var ips = a.GetIPsForCell(4.0, 6.0);
            Assert.AreEqual(3, ips.Indices.Length);
            Assert.AreEqual(184, ips.Indices[0]);
            Assert.AreEqual(185, ips.Indices[1]);
            Assert.AreEqual(186, ips.Indices[2]);

            ips = a.GetIPsForCell(178.0, 180.0);
            Assert.AreEqual(3, ips.Indices.Length);
            Assert.AreEqual(358, ips.Indices[0]);
            Assert.AreEqual(359, ips.Indices[1]);
            Assert.AreEqual(0, ips.Indices[2]);

            ips = a.GetIPsForCell(270.2, 270.6);
            Assert.AreEqual(2, ips.Indices.Length);
            Assert.AreEqual(90, ips.Indices[0]);
            Assert.AreEqual(91, ips.Indices[1]);           
        }
    }
}
