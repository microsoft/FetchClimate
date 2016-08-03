using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Research.Science.FetchClimate2.DataHandlers;
using Microsoft.Research.Science.FetchClimate2;
using Microsoft.Research.Science.FetchClimate2.Tests;
using Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints.LinearCombination;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataHandlersTests.ScatteredPoints
{
    [TestClass]
    public class LinearCombinationAggregatorTests
    {        
        [TestMethod]
        [TestCategory("BVT")]
        [TestCategory("Local")]
        public async Task LinearCombinationAggregatorTest()
        {
            LinearCombinationAggregator lca = new LinearCombinationAggregator();
                                    
            var nodes = new RealValueNodes(new double[3],new double[3], new double[] {2.0,3.0,5.0});
            var weights = new LinearWeight[] {new LinearWeight(1,7.0),new LinearWeight(0,11.0), new LinearWeight(2,13.0)};
            LinearCombinationContext lcc = new LinearCombinationContext(new Tuple<ICellRequest, RealValueNodes, IEnumerable<LinearWeight>>[] { new Tuple<ICellRequest, RealValueNodes, IEnumerable<LinearWeight>>(new RequestStubs(), nodes, (IEnumerable<LinearWeight>)weights) });

            double[] result = await lca.AggregateCellsBatchAsync(lcc, new ICellRequest[] { new RequestStubs() });

            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(108, result[0]);
        }
    }
}
