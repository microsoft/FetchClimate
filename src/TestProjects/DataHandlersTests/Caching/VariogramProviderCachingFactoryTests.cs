using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints;
using System.Threading.Tasks;
using Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints.LinearCombination;
using Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators;

namespace DataHandlersTests.Caching
{
    [TestClass]
    public class VariogramCaching
    {
        class VariogramStub : VariogramModule.IVariogram
        {
            public static int counter = 1;
            int current;

            public VariogramStub()
            {
                current = counter++;
            }

            
            public double GetGamma(double value)
            {
                return current;
            }

            public double Nugget
            {
                get { return current; }
            }

            public double Range
            {
                get { return current; }
            }

            public double Sill
            {
                get { return current; }
            }
        }

        class VarpProvStub : IVariogramProvider
        {

            public async Task<VariogramModule.IVariogram> GetSpatialVariogramAsync(RealValueNodes nodes)
            {
                return new VariogramStub();
            }
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public async Task VariogramCachingTest()
        {
            var component = new VarpProvStub();
            var factory = new VariogramProviderCachingFactory(component);
            var variogramfitter = await factory.ConstructAsync();

            double[] lats = new double[10], lons = new double[10], vals = new double[10];

            Random r = new Random(1);

            for (int i = 0; i < 10; i++)
			{
                lats[i] = r.NextDouble();
                lons[i] = r.NextDouble();
                vals[i] = 3*lons[i]+2*lats[i];
			}


            RealValueNodes rvn1 = new RealValueNodes(lats, lons, vals);
            RealValueNodes rvn2 = new RealValueNodes(lats, lons, vals);
            RealValueNodes rvn3 = new RealValueNodes(lats, lons, lons);

            var v1 = await variogramfitter.GetSpatialVariogramAsync(rvn1);
            var v2 = await variogramfitter.GetSpatialVariogramAsync(rvn2);
            var v3 = await variogramfitter.GetSpatialVariogramAsync(rvn3);

            Assert.AreEqual(1.0, v1.Nugget);
            Assert.AreEqual(1.0, v2.Nugget);
            Assert.AreEqual(2.0, v3.Nugget);
        }
    }
}
