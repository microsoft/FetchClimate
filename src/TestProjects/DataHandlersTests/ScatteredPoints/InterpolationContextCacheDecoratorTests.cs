using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Research.Science.FetchClimate2;
using Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints.LinearCombination;
using Microsoft.Research.Science.FetchClimate2.Utils;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Tests.DataHandlers.ScatteredPoints
{
    [TestClass]
    public class InterpolationContextCacheDecoratorTests
    {
        class Stub1 : IAsyncMap<INodes,int>
        {
            static int counter = 0;

            public async Task<int> GetAsync(INodes nodes)
            {
                return ++counter;
            }
        }    

        class Stub3 : INodes
        {
            double lat, lon;

            public Stub3(double lat,double lon)
            {
                this.lat = lat;
                this.lon = lon;
            }

            public double[] Lats
            {
                get { return new double[] { lat }; }
            }

            public double[] Lons
            {
                get { return new double[] { lon }; }
            }
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public async Task  InterpolationContextCacheDecoratorTest()
        {
            var iccd = new AsyncMapCacheDecorator<INodes,int>(new HashBasedEquatibleINodesConverter(), new Stub1());

            var c1 = new Stub3(1.0,1.0);
            var c2 = new Stub3(1.0,1.0);
            var c3 = new Stub3(1.0,2.0);
            var c4 = new Stub3(3.0,3.0);

            var cont1 = await iccd.GetAsync(c1);
            var cont2 = await iccd.GetAsync(c2);
            var cont3 = await iccd.GetAsync(c3);
            var cont4 = await iccd.GetAsync(c4);

            Assert.AreEqual(cont1,cont2);
            Assert.AreNotEqual(cont1, cont3);
            Assert.AreNotEqual(cont1, cont4);
        }
    }
}

