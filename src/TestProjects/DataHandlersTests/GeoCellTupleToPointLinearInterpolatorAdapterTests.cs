using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Research.Science.FetchClimate2;
using Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints.LinearCombination;
using DataHandlersTests;
using Microsoft.Research.Science.FetchClimate2.Utils;
using System.Threading.Tasks;
using Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints;

namespace Microsoft.Research.Science.FetchClimate2.Tests.Adapters
{
    [TestClass]
    public class GeoCellTupleToPointLinearInterpolatorAdapterTests
    {
        class Stub : IScatteredPointContextBasedLinearWeightProviderOnSphere<Tuple<double, double>>
        {
            public async Task<LinearWeight[]> GetLinearWeigthsAsync(double lat, double lon, Tuple<double, double> interpolationContext)
            {
                double l1 = lat - interpolationContext.Item1, l2 = lon - interpolationContext.Item2, sum = l1 + l2;
                return new LinearWeight[] { new LinearWeight(0, l1 / sum), new LinearWeight(1, l2 / sum) };
            }
        }

        class Stub2: IAsyncMap<INodes,Tuple<double,double>>
        {
            public async Task<Tuple<double, double>> GetAsync(INodes nodes)
            {
                return Tuple.Create(nodes.Lats[0], nodes.Lons[0]);
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
        public async Task GeoCellTupleToPointLinearInterpolatorAdapterTest()
        {
            var adapter = new CellRequestToPointsAdapter<Tuple<double, double>>(new Stub());
            var context = await (new Stub2()).GetAsync(new NodesStub());

            var cell = new RequestStubs() { LatMin = 5.0, LatMax = 7.0, LonMin = 11.0, LonMax = 13.0 };

            var result = await adapter.GetLinearWeigthsAsync(cell, context);

            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(0, result[0].DataIndex);
            Assert.AreEqual(1, result[1].DataIndex);
            Assert.AreEqual(2620.0/8580.0, result[0].Weight, TestConstants.DoublePrecision); //manual camputation
            Assert.AreEqual(5960.0/8580.0, result[1].Weight, TestConstants.DoublePrecision);
        }
    }
}
