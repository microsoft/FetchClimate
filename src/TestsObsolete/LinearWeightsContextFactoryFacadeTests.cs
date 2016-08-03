using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Research.Science.FetchClimate2.Utils;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints.LinearCombination;
using Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints;

namespace Microsoft.Research.Science.FetchClimate2.Tests
{
    [TestClass]
    public class LinearWeightsContextFactoryFacadeTests
    {
        class RealValueNodesStub : RealValueNodes
        {

            public RealValueNodesStub()
                :base(
                 new double[] { 2.0,3.0},
                new double[] { 5.0, 7.0 },
                new double[] { 23.0, 31.0 })
            { }            
        }

        class Stub1 : IScatteredPointsLinearInterpolatorOnSphere
        {

            public LinearWeight[] GetLinearWeigths(GeoCellTuple cell, Nodes nodes)
            {
                int N = nodes.Lons.Length;
                LinearWeight[] weights = new LinearWeight[N];

                for (int i = 0; i < N; i++)
                {
                    weights[i] = new LinearWeight(N -i - 1, cell.LatMax*(i+1));
                }

                return weights;
            }
        }


        class Stub2 : ITimeSeriesAverager<RealValueNodesStub>
        {
            public async Task<RealValueNodesStub> GetAveragedTimeSeriesAsync(IRequestContext requestContext, IGeoCellTuple cell)
            {
                return new RealValueNodesStub();
            }
        }

        class Stub3 : ITimeSeriesAveragerFactory<RealValueNodesStub>
        {
            public async Task<ITimeSeriesAverager<RealValueNodesStub>> CreateAsync(IStorageContext context)
            {
                return new Stub2();
            }
        }

        class Stub4 : IScatteredPointsLinearInterpolatorOnSphereFactory
        {
            public async Task<IScatteredPointsLinearInterpolatorOnSphere> CreateAsync()
            {
                return new Stub1();
            }
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public async Task LinearWeightsContextFactoryFacadeTest()
        {
            var lwcff = new LinearWeightsContextFactoryFacade<RealValueNodesStub>(new Stub4(), new Stub3());                        

            var cells = new GeoCellTuple[] { new GeoCellTuple() { LatMax = 11.0, LonMin = 13.0, LatMin = 17.0, LonMax = 19.0} };

            var request = new FetchRequest("dummy",FetchDomain.CreateCells(new double[] { 17.0}, new double[] {13.0}, new double[] {11.0}, new double[] {19.0}, new TimeRegion()));

            var storage = TestDataStorageFactory.GetStorage("msds:memory");
            var requestContext = RequestContextStub.GetStub(storage,request);


            var lwc = await lwcff.CreateAsync(requestContext,cells);

            var combs = lwc.Combinations.ToArray();

            Assert.AreEqual(1, combs.Length);
            Assert.AreEqual(cells[0],combs[0].Item1);
            
            RealValueNodes nodes = combs[0].Item2;
            IEnumerable<LinearWeight> weights = combs[0].Item3;

            var result = weights.Sum(w => w.Weight * nodes.Values[w.DataIndex]);
            Assert.AreEqual(847.0, result);
        }
    }
}
