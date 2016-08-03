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
        public void TestGetWeights()
        {
            double[] axis = Enumerable.Range(0,24).Select(a => (double)a).ToArray();
            StepFunctionWeightsProvider weightsProvider = new StepFunctionWeightsProvider();
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

            weights = weightsProvider.GetWeights(axis, 30.0, 30.4, out start, out stop, dec);
            Assert.AreEqual(0, weights.Length);            

            weights = weightsProvider.GetWeights(axis, -30.7, -30.4, out start, out stop, dec);
            Assert.AreEqual(0, weights.Length);            
        }
    }
}
