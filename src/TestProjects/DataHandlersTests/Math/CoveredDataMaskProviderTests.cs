using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Research.Science.FetchClimate2.DataMaskProviders;
using Microsoft.Research.Science.FetchClimate2;

namespace DataHandlersTests.Math
{
    [TestClass]
    public class CoveredDataMaskProviderTests
    {
        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void GetBoundingBoxAscAxisTest()
        {
            var target = new CoveredPointsStatistics(new double[] { 0.0, 5.0, 10.0, 15.0 });
            var res = target.GetBoundingBox(1.1, 1.2);
            Assert.IsTrue(res.IsSingular); //requested region does not cover any data points

            res = target.GetBoundingBox(-1.2, -1.1);
            Assert.IsTrue(res.IsSingular); //requested region does not cover any data points

            res = target.GetBoundingBox(100, 120);
            Assert.IsTrue(res.IsSingular); //requested region does not cover any data points

            res = target.GetBoundingBox(1.1, 6.2);
            Assert.AreEqual(1,res.first);
            Assert.AreEqual(1, res.last);

            res = target.GetBoundingBox(1.1, 11.2);
            Assert.AreEqual(1, res.first);
            Assert.AreEqual(2, res.last);

            res = target.GetBoundingBox(1.1, 211.2);
            Assert.AreEqual(1, res.first);
            Assert.AreEqual(3, res.last);

            res = target.GetBoundingBox(-1.1, 11.2);
            Assert.AreEqual(0, res.first);
            Assert.AreEqual(2, res.last);

            res = target.GetBoundingBox(-1.1, 0.0);
            Assert.AreEqual(0, res.first);
            Assert.AreEqual(0, res.last);

            res = target.GetBoundingBox(15.0, 120.0);
            Assert.AreEqual(3, res.first);
            Assert.AreEqual(3, res.last);

            res = target.GetBoundingBox(5.0, 6.0);
            Assert.AreEqual(1, res.first);
            Assert.AreEqual(1, res.last);

            res = target.GetBoundingBox(4.0, 5.0);
            Assert.AreEqual(1, res.first);
            Assert.AreEqual(1, res.last);
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void GetBoundingBoxDescAxisTest()
        {
            var target = new CoveredPointsStatistics(new double[] { 15.0, 10.0, 5.0, 0.0 });            
            var res = target.GetBoundingBox(1.1, 1.2);
            Assert.IsTrue(res.IsSingular); //requested region does not cover any data points

            res = target.GetBoundingBox(-1.2, -1.1);
            Assert.IsTrue(res.IsSingular); //requested region does not cover any data points

            res = target.GetBoundingBox(100, 120);
            Assert.IsTrue(res.IsSingular); //requested region does not cover any data points

            res = target.GetBoundingBox(1.1, 6.2);
            Assert.AreEqual(2, res.first);
            Assert.AreEqual(2, res.last);

            res = target.GetBoundingBox(1.1, 11.2);
            Assert.AreEqual(1, res.first);
            Assert.AreEqual(2, res.last);

            res = target.GetBoundingBox(1.1, 211.2);
            Assert.AreEqual(0, res.first);
            Assert.AreEqual(2, res.last);

            res = target.GetBoundingBox(-1.1, 11.2);
            Assert.AreEqual(1, res.first);
            Assert.AreEqual(3, res.last);

            res = target.GetBoundingBox(-1.1, 0.0);
            Assert.AreEqual(3, res.first);
            Assert.AreEqual(3, res.last);

            res = target.GetBoundingBox(15.0, 120.0);
            Assert.AreEqual(0, res.first);
            Assert.AreEqual(0, res.last);

            res = target.GetBoundingBox(5.0, 6.0);
            Assert.AreEqual(2, res.first);
            Assert.AreEqual(2, res.last);

            res = target.GetBoundingBox(4.0, 5.0);
            Assert.AreEqual(2, res.first);
            Assert.AreEqual(2, res.last);
        }

        private void CompareArrays(int[] first, int[] second)
        {
            Assert.AreEqual(first.Length,second.Length);
            for (int i = 0; i < first.Length; i++)			
			    Assert.AreEqual(first[i],second[i]);			
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void GetDataIndicesAscAxisTest()
        {
            var target = new CoveredPointsStatistics(new double[] { 0.0, 5.0, 10.0, 15.0 });
            var res = target.GetDataIndices(1.1, 1.2);
            Assert.AreEqual(0,res.Length); //requested region does not cover any data points

            res = target.GetDataIndices(-1.2, -1.1);
            Assert.AreEqual(0, res.Length); //requested region does not cover any data points

            res = target.GetDataIndices(100, 120);
            Assert.AreEqual(0, res.Length); //requested region does not cover any data points

            res = target.GetDataIndices(1.1, 6.2);
            CompareArrays(new int[] { 1 }, res);

            res = target.GetDataIndices(1.1, 11.2);
            CompareArrays(new int[] { 1,2 }, res);

            res = target.GetDataIndices(1.1, 211.2);
            CompareArrays(new int[] { 1,2,3 }, res);

            res = target.GetDataIndices(-1.1, 11.2);
            CompareArrays(new int[] { 0,1,2 }, res);

            res = target.GetDataIndices(-1.1, 0.0);
            CompareArrays(new int[] { 0 }, res);

            res = target.GetDataIndices(15.0, 120.0);
            CompareArrays(new int[] { 3 }, res);

            res = target.GetDataIndices(5.0, 6.0);
            CompareArrays(new int[] { 1 }, res);

            res = target.GetDataIndices(4.0, 5.0);
            CompareArrays(new int[] { 1 }, res);
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void GetDataIndicesDescAxisTest()
        {
            var target = new CoveredPointsStatistics(new double[] { 15.0, 10.0, 5.0, 0.0 });
            var res = target.GetDataIndices(1.1, 1.2);
            Assert.AreEqual(0, res.Length); //requested region does not cover any data points

            res = target.GetDataIndices(-1.2, -1.1);
            Assert.AreEqual(0, res.Length); //requested region does not cover any data points

            res = target.GetDataIndices(100, 120);
            Assert.AreEqual(0, res.Length); //requested region does not cover any data points

            res = target.GetDataIndices(1.1, 6.2);
            CompareArrays(new int[] { 2 }, res);

            res = target.GetDataIndices(1.1, 11.2);
            CompareArrays(new int[] { 2,1 }, res);

            res = target.GetDataIndices(1.1, 211.2);
            CompareArrays(new int[] { 2,1,0 }, res);

            res = target.GetDataIndices(-1.1, 11.2);
            CompareArrays(new int[] { 3,2,1 }, res);

            res = target.GetDataIndices(-1.1, 0.0);
            CompareArrays(new int[] { 3 }, res);

            res = target.GetDataIndices(15.0, 120.0);
            CompareArrays(new int[] { 0 }, res);

            res = target.GetDataIndices(5.0, 6.0);
            CompareArrays(new int[] { 2 }, res);

            res = target.GetDataIndices(4.0, 5.0);
            CompareArrays(new int[] { 2 }, res);
        }
    }
}
