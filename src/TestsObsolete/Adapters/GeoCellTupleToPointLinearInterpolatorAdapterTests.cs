using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Research.Science.FetchClimate2;
using Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints.LinearCombination;

namespace Microsoft.Research.Science.FetchClimate2.Tests.Adapters
{
    [TestClass]
    public class GeoCellTupleToPointLinearInterpolatorAdapterTests
    {
        class Stub : I2PhaseScatteredPointsLinearInterpolatorOnSphere
        {

            public object GetInterpolationContext(INodes nodes)
            {
                return Tuple.Create(nodes.Lats[0], nodes.Lons[0]);
            }

            public LinearWeight[] GetLinearWeigths(double lat, double lon, object interpolationContext)
            {
                Tuple<double, double> t = (Tuple<double, double>)interpolationContext;
                double l1 = lat - t.Item1, l2 = lon - t.Item2, sum = l1+ l2;
                return new LinearWeight[] { new LinearWeight(0, l1/sum), new LinearWeight(1, l2/sum) };
            }
        }

        class NodesStub : INodes
        {

            public double[] Lats
            {
                get { return new double[] { 2.0 }; }
            }

            public double[] Lons
            {
                get { return new double[] { 3.0 }; }
            }
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void GeoCellTupleToPointLinearInterpolatorAdapterTest()
        {
            var adapter = new GeoCellTupleToPointLinearInterpolatorAdapter(new Stub());
            var context = adapter.GetInterpolationContext(new NodesStub());

            var cell = new GeoCellTuple(){ LatMin = 5.0 , LatMax = 7.0, LonMin = 11.0, LonMax = 13.0};

            var result = adapter.GetLinearWeigths(cell, context);

            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(0, result[0].DataIndex);
            Assert.AreEqual(1, result[1].DataIndex);
            Assert.AreEqual(2620.0/8580.0, result[0].Weight, TestConstants.DoublePrecision); //manual camputation
            Assert.AreEqual(5960.0/8580.0, result[1].Weight, TestConstants.DoublePrecision);
        }
    }
}
