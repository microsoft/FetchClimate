using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Research.Science.FetchClimate2;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Research.Science.FetchClimate2.DataHandlersTests
{
    [TestClass]
    public class StepFunctionDataMaskProviderTests
    {
        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void GetBoundingBoxAscAxisTest()
        {
            var target = new DataMaskProviders.StepFunctionDataMaskProvider();
            var grid = new double[] { 0.0, 5.0, 10.0, 15.0 };
            var res = target.GetBoundingBox(grid,1.1, 1.2);
            Assert.AreEqual(0, res.first);
            Assert.AreEqual(0, res.last);

            res = target.GetBoundingBox(grid, -1.2, -1.1);
            Assert.IsTrue(res.IsSingular); //requested region does not cover any data points

            res = target.GetBoundingBox(grid, 100, 120);
            Assert.IsTrue(res.IsSingular); //requested region does not cover any data points

            res = target.GetBoundingBox(grid, 1.1, 6.2);
            Assert.AreEqual(0, res.first);
            Assert.AreEqual(1, res.last);

            res = target.GetBoundingBox(grid, 1.1, 11.2);
            Assert.AreEqual(0, res.first);
            Assert.AreEqual(2, res.last);

            res = target.GetBoundingBox(grid, 1.1, 211.2);
            Assert.AreEqual(0, res.first);
            Assert.AreEqual(3, res.last);

            res = target.GetBoundingBox(grid, -1.1, 11.2);
            Assert.AreEqual(0, res.first);
            Assert.AreEqual(2, res.last);

            res = target.GetBoundingBox(grid, -1.1, 0.0);
            Assert.IsTrue(res.IsSingular);

            res = target.GetBoundingBox(grid, 15.0, 120.0);
            Assert.AreEqual(3, res.first);
            Assert.AreEqual(3, res.last);

            res = target.GetBoundingBox(grid, 5.0, 6.0);
            Assert.AreEqual(1, res.first);
            Assert.AreEqual(1, res.last);

            res = target.GetBoundingBox(grid, 4.0, 5.0);
            Assert.AreEqual(0, res.first);
            Assert.AreEqual(0, res.last);
        }       

        private void CompareArrays(int[] first, int[] second)
        {
            Assert.AreEqual(first.Length, second.Length);
            for (int i = 0; i < first.Length; i++)
                Assert.AreEqual(first[i], second[i]);
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void GetDataIndicesAscAxisTest()
        {
            var target = new DataMaskProviders.StepFunctionDataMaskProvider();
            var grid = new double[] { 0.0, 5.0, 10.0, 15.0 };
            var res = target.GetIndices(grid,1.1, 1.2);
            CompareArrays(new int[] { 0 }, res);

            res = target.GetIndices(grid, -1.2, -1.1);
            Assert.AreEqual(0, res.Length); //requested region does not cover any data points

            res = target.GetIndices(grid, 100, 120);
            Assert.AreEqual(0, res.Length); //requested region does not cover any data points

            res = target.GetIndices(grid, 1.1, 6.2);
            CompareArrays(new int[] { 0,1 }, res);

            res = target.GetIndices(grid, 1.1, 11.2);
            CompareArrays(new int[] { 0,1, 2 }, res);

            res = target.GetIndices(grid,1.1, 211.2);
            CompareArrays(new int[] { 0,1, 2, 3 }, res);

            res = target.GetIndices(grid,-1.1, 11.2);
            CompareArrays(new int[] { 0, 1, 2 }, res);

            res = target.GetIndices(grid,-1.1, 0.0);
            Assert.AreEqual(0, res.Length);

            res = target.GetIndices(grid,15.0, 120.0);
            CompareArrays(new int[] { 3 }, res);

            res = target.GetIndices(grid,5.0, 6.0);
            CompareArrays(new int[] { 1 }, res);

            res = target.GetIndices(grid,4.0, 5.0);
            CompareArrays(new int[] { 0 }, res);
        }

    }
}
