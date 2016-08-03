using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Microsoft.Research.Science.FetchClimate2.Tests
{
    [TestClass]
    public class LinearWeightsProviderTests
    {
        [TestMethod]
        [TestCategory("BVT")]
        [TestCategory("Local")]
        public void GetWeightsTestLinearInterpolator()
        {
            double tolerance = 1e-8;

            WeightProviders.LinearInterpolation lwp = new WeightProviders.LinearInterpolation();
            double[] grid = System.Linq.Enumerable.Range(0, 10).Select(v => (double)v).ToArray();

            int start, stop;

            var w = lwp.GetWeights(grid, 0.5, 0.5, out start, out stop);
            Assert.AreEqual(2, w.Length);
            Assert.AreEqual(0.5, w[0]);
            Assert.AreEqual(0.5, w[1]);
            Assert.AreEqual(0, start);
            Assert.AreEqual(1, stop);

            w = lwp.GetWeights(grid, 0.0, 0.0, out start, out stop);
            Assert.AreEqual(1, w.Length);
            Assert.AreEqual(1.0, w[0]);
            Assert.AreEqual(0, start);
            Assert.AreEqual(0, stop);

            w = lwp.GetWeights(grid, 0.0, 0.5, out start, out stop);
            Assert.AreEqual(2, w.Length);
            Assert.AreEqual(0.75, w[0]);
            Assert.AreEqual(0.25, w[1]);
            Assert.AreEqual(0, start);
            Assert.AreEqual(1, stop);

            w = lwp.GetWeights(grid, 0.5, 0.75, out start, out stop);
            Assert.AreEqual(2, w.Length);
            Assert.AreEqual(3.0 / 8.0, w[0]);
            Assert.AreEqual(5.0 / 8.0, w[1]);
            Assert.AreEqual(0, start);
            Assert.AreEqual(1, stop);

            w = lwp.GetWeights(grid, 0.5, 1.0, out start, out stop);
            Assert.AreEqual(2, w.Length);
            Assert.AreEqual(0.25, w[0]);
            Assert.AreEqual(0.75, w[1]);
            Assert.AreEqual(0, start);
            Assert.AreEqual(1, stop);

            w = lwp.GetWeights(grid, 0.5, 1.25, out start, out stop);
            Assert.AreEqual(3, w.Length);
            Assert.AreEqual(0.125 / 0.75, w[0]);
            Assert.AreEqual(0.59375 / 0.75, w[1]);
            Assert.AreEqual(1 / 32.0 / 0.75, w[2]);
            Assert.AreEqual(0, start);
            Assert.AreEqual(2, stop);

            w = lwp.GetWeights(grid, 8.5, 8.5, out start, out stop);
            Assert.AreEqual(2, w.Length);
            Assert.AreEqual(0.5, w[0]);
            Assert.AreEqual(0.5, w[1]);
            Assert.AreEqual(8, start);
            Assert.AreEqual(9, stop);

            w = lwp.GetWeights(grid, 7.75, 8.5, out start, out stop);
            Assert.AreEqual(3, w.Length);
            Assert.AreEqual(1 / 32.0 / 0.75, w[0]);
            Assert.AreEqual(0.59375 / 0.75, w[1]);
            Assert.AreEqual(0.125 / 0.75, w[2]);
            Assert.AreEqual(7, start);
            Assert.AreEqual(9, stop);

            w = lwp.GetWeights(grid, 8.5, 9.0, out start, out stop);
            Assert.AreEqual(2, w.Length);
            Assert.AreEqual(0.25, w[0]);
            Assert.AreEqual(0.75, w[1]);
            Assert.AreEqual(8, start);
            Assert.AreEqual(9, stop);

            w = lwp.GetWeights(grid, 9.0, 9.0, out start, out stop);
            Assert.AreEqual(1, w.Length);
            Assert.AreEqual(1.0, w[0]);
            Assert.AreEqual(9, start);
            Assert.AreEqual(9, stop);


            grid = new double[] { 0.0, 1.0, 3.0, 4.0, 5.0 };
            w = lwp.GetWeights(grid, 0.5, 4.25, out start, out stop);
            Assert.AreEqual(5, w.Length);
            Assert.AreEqual(1.0 / 8.0 / 3.75, w[0], tolerance);
            Assert.AreEqual(11.0 / 8.0 / 3.75, w[1], tolerance);
            Assert.AreEqual(3.0 / 2.0 / 3.75, w[2], tolerance);
            Assert.AreEqual(23.0 / 32.0 / 3.75, w[3], tolerance);
            Assert.AreEqual(1.0 / 32.0 / 3.75, w[4], tolerance);
            Assert.AreEqual(0, start);
            Assert.AreEqual(4, stop);


            //out of orange
            w = lwp.GetWeights(grid, 11.0, 11.0, out start, out stop);
            Assert.AreEqual(0, w.Length);

            w = lwp.GetWeights(grid, 11.0, 12.0, out start, out stop);
            Assert.AreEqual(0, w.Length);

            w = lwp.GetWeights(grid, -2.0, -1.0, out start, out stop);
            Assert.AreEqual(0, w.Length);

            w = lwp.GetWeights(grid, 5.5, 12.0, out start, out stop);
            Assert.AreEqual(0, w.Length);
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void BoundingBoxTestLinearInterpolator()
        {
            WeightProviders.LinearInterpolation lwp = new WeightProviders.LinearInterpolation();
            double[] grid = System.Linq.Enumerable.Range(0, 10).Select(v => (double)v).ToArray();
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


