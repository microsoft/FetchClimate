using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Microsoft.Research.Science.FetchClimate2.Tests
{
    [TestClass]
    public class StepFunctionTests
    {
        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void GetWeightsTestStepInterpolator()
        {
            double[] axis = Enumerable.Range(0,24).Select(a => (double)a).ToArray();
            WeightProviders.StepFunctionInterpolation weightsProvider = new WeightProviders.StepFunctionInterpolation();
            DoubleEpsComparer dec = new DoubleEpsComparer(0.1);

            int start,stop;
            double[] weights = weightsProvider.GetWeights(axis, 0.0, 1.0, out start, out stop, dec);
            Assert.AreEqual(1, weights.Length);
            Assert.AreEqual(0, start);
            Assert.AreEqual(0, stop);

            weights = weightsProvider.GetWeights(axis, 0.0, 2.0, out start, out stop, dec);
            Assert.AreEqual(2, weights.Length);
            Assert.AreEqual(weights[0],weights[1]);
            Assert.AreEqual(0, start);
            Assert.AreEqual(1, stop);

            weights = weightsProvider.GetWeights(axis, 3.5, 4.5, out start, out stop, dec);
            Assert.AreEqual(2, weights.Length);
            Assert.AreEqual(weights[0], weights[1]);
            Assert.AreEqual(3, start);
            Assert.AreEqual(4, stop);

            weights = weightsProvider.GetWeights(axis, 3.5, 5.5, out start, out stop, dec);
            Assert.AreEqual(3, weights.Length);
            Assert.AreEqual(weights[0], weights[2]);
            Assert.AreEqual(weights[1], 2.0* weights[2]);
            Assert.AreEqual(3, start);
            Assert.AreEqual(5, stop);

            weights = weightsProvider.GetWeights(axis, 0.7, 0.75, out start, out stop, dec);
            Assert.AreEqual(1, weights.Length);
            Assert.AreEqual(0, start);
            Assert.AreEqual(0, stop);

            weights = weightsProvider.GetWeights(axis, 5.75, 8.75, out start, out stop, dec);
            Assert.AreEqual(4, weights.Length);
            Assert.AreEqual(weights[1], weights[2]);
            Assert.AreEqual(weights[0], 0.25 * weights[1]);
            Assert.AreEqual(weights[3], 0.75 * weights[2]);
            Assert.AreEqual(5, start);
            Assert.AreEqual(8, stop);


            //out of range
            weights = weightsProvider.GetWeights(axis, 30.0, 31.1, out start, out stop, dec);
            Assert.AreEqual(0, weights.Length);

            weights = weightsProvider.GetWeights(axis, -2.0, -1.0, out start, out stop, dec);
            Assert.AreEqual(0, weights.Length);

            weights = weightsProvider.GetWeights(axis, 10.0, 30.0, out start, out stop, dec);
            Assert.AreEqual(0, weights.Length);      
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void BoundingBoxTestStepInterpolator()
        {
            WeightProviders.StepFunctionInterpolation lwp = new WeightProviders.StepFunctionInterpolation();
            double[] grid = System.Linq.Enumerable.Range(0, 10).Select(v => (double)v).ToArray();
            var bb = lwp.GetBoundingBox(grid, -1.0, -0.3);
            Assert.IsTrue(bb.IsSingular);

            bb = lwp.GetBoundingBox(grid, -1.0, 0.5);
            Assert.IsTrue(bb.IsSingular);

            bb = lwp.GetBoundingBox(grid, -1.0, 0.0);
            Assert.IsTrue(bb.IsSingular);

            bb = lwp.GetBoundingBox(grid, 0.5, 0.5);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(0, bb.last);

            bb = lwp.GetBoundingBox(grid, 0.0, 0.0);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(0, bb.last);

            bb = lwp.GetBoundingBox(grid, 0.0, 0.6);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(0, bb.last);

            bb = lwp.GetBoundingBox(grid, 0.5, 0.6);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(0, bb.last);

            bb = lwp.GetBoundingBox(grid, 0.5, 1.0);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(0, bb.last);

            bb = lwp.GetBoundingBox(grid, 0.5, 1.1);
            Assert.AreEqual(0, bb.first);
            Assert.AreEqual(1, bb.last);

            bb = lwp.GetBoundingBox(grid, 8.5, 8.5);
            Assert.AreEqual(8, bb.first);
            Assert.AreEqual(8, bb.last);

            bb = lwp.GetBoundingBox(grid, 7.9, 8.5);
            Assert.AreEqual(7, bb.first);
            Assert.AreEqual(8, bb.last);

            bb = lwp.GetBoundingBox(grid, 8.5, 9.0);
            Assert.AreEqual(8, bb.first);
            Assert.AreEqual(8, bb.last);

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
