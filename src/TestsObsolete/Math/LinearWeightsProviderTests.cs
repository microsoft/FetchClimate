using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Microsoft.Research.Science.FetchClimate2.Tests
{
    [TestClass]
    public class LinearWeightsProviderTests
    {
        [TestMethod]
        [TestCategory("Local")]        
        [TestCategory("BVT")]
        public void LinearWeightsProviderBoundingBoxTest()
        {
            LinearWeightsProvider lwp = new LinearWeightsProvider();
            double[] grid = System.Linq.Enumerable.Range(0,10).Select(v => (double)v).ToArray();
            var bb = lwp.GetBoundingBox(grid, -1.0, -0.3);            
            Assert.IsTrue(bb.IsSingular);

            bb = lwp.GetBoundingBox(grid, -1.0, 0.5);
            Assert.IsTrue(bb.IsSingular);

            bb = lwp.GetBoundingBox(grid, -1.0, 0.0);
            Assert.IsTrue(bb.IsSingular);

            bb = lwp.GetBoundingBox(grid, 0.5, 0.5);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(1, bb.last);

            bb = lwp.GetBoundingBox(grid, 0.0, 0.0);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(0, bb.last);

            bb = lwp.GetBoundingBox(grid, 0.0, 0.6);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(1, bb.last);

            bb = lwp.GetBoundingBox(grid, 0.5, 0.6);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(1, bb.last);

            bb = lwp.GetBoundingBox(grid, 0.5, 1.0);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(1, bb.last);

            bb = lwp.GetBoundingBox(grid, 0.5, 1.1);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(2, bb.last);

            bb = lwp.GetBoundingBox(grid, 8.5, 8.5);
            Assert.AreEqual(8, bb.first);
            Assert.AreEqual(9, bb.last);

            bb = lwp.GetBoundingBox(grid, 7.9, 8.5);
            Assert.AreEqual(7, bb.first);
            Assert.AreEqual(9, bb.last);

            bb = lwp.GetBoundingBox(grid, 8.5, 9.0);
            Assert.AreEqual(8, bb.first);
            Assert.AreEqual(9, bb.last);

            bb = lwp.GetBoundingBox(grid, 9.0, 9.0);
            Assert.AreEqual(9, bb.first);
            Assert.AreEqual(9, bb.last);

            bb = lwp.GetBoundingBox(grid, 9.0, 10.0);
            Assert.IsTrue(bb.IsSingular);

            bb = lwp.GetBoundingBox(grid, 8.5, 10.0);
            Assert.IsTrue(bb.IsSingular);

            bb = lwp.GetBoundingBox(grid, 9.1, 10.0);
            Assert.IsTrue(bb.IsSingular);
        }
    }
}

