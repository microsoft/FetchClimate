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

            public async Task<LinearWeight[]> GetLinearWeigthsAsync(INodes nodes, ICellRequest cell)
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


        class Stub2 : ICellRequestMap<RealValueNodesStub>
        {
            public async Task<RealValueNodesStub> GetAsync(ICellRequest cell)
            {
                return new RealValueNodesStub();
            }
        }

        class Stub3 : ICellRequestMapFactory<RealValueNodesStub>
        {
            public async Task<ICellRequestMap<RealValueNodesStub>> CreateAsync()
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

        class Stub5 : ICellRequest
        {

            public string VariableName
            {
                get { throw new NotImplementedException(); }
            }

            public double LatMin
            {
                get { return 17.0; }
            }

            public double LonMin
            {
                get { return 13.0; }
            }

            public double LatMax
            {
                get { return 11.0; }
            }

            public double LonMax
            {
                get { return 19.0; }
            }

            public ITimeSegment Time
            {
                get { throw new NotImplementedException(); }
            }
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public async Task LinearWeightsContextFactoryFacadeTest()
        {
            var lwcff = new LinearWeightsContextFactoryFacade<RealValueNodesStub>(new Stub4(), new Stub3());                        

            var cells = new ICellRequest[] { new Stub5() };

            var lwc = await lwcff.CreateAsync(cells);

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
