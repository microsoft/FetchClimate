using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Research.Science.FetchClimate2;
using Microsoft.Research.Science.FetchClimate2.Integrators.Spatial;

namespace DataHandlersTests
{
    [TestClass]
    public class LinearGridIntegratorTests
    {
        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void AxisValuesTest()
        {
            double[] doubleAxis = new double[] { 0.0,1.0,2.0};
            double[] doubleAxisDesc = new double[] { 2.0, 1.0, 0.0 };
            float[] floatAxis = new float[] { 0.0f, 1.0f, 2.0f};
            float[] floatAxisDesc = new float[] { 2.0f,1.0f, 0.0f};

            double[] axis = new LinearGridIntegrator(doubleAxis).AxisValues;
            Assert.AreEqual(0.0, axis[0]);
            Assert.AreEqual(1.0, axis[1]);
            Assert.AreEqual(2.0, axis[2]);

            axis = new LinearGridIntegrator(doubleAxisDesc).AxisValues;
            Assert.AreEqual(0.0, axis[2]);
            Assert.AreEqual(1.0, axis[1]);
            Assert.AreEqual(2.0, axis[0]);

            axis = new LinearGridIntegrator(floatAxis).AxisValues;
            Assert.AreEqual(0.0, axis[0]);
            Assert.AreEqual(1.0, axis[1]);
            Assert.AreEqual(2.0, axis[2]);

            axis = new LinearGridIntegrator(floatAxisDesc).AxisValues;
            Assert.AreEqual(0.0, axis[2]);
            Assert.AreEqual(1.0, axis[1]);
            Assert.AreEqual(2.0, axis[0]);
        }

    }
}
