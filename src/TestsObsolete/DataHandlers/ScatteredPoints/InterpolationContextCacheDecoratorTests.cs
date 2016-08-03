using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Research.Science.FetchClimate2;
using Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints.LinearCombination;

namespace Microsoft.Research.Science.FetchClimate2.Tests.DataHandlers.ScatteredPoints
{
    [TestClass]
    public class InterpolationContextCacheDecoratorTests
    {
        class Stub1 : I2PhaseGeoCellScatteredPointsLinearInterpolatorOnSphere
        {
            static int counter = 0;

            public object GetInterpolationContext(INodes nodes)
            {
                return ++counter;
            }

            public LinearWeight[] GetLinearWeigths(GeoCellTuple cell, object interpolationContext)
            {
                throw new NotImplementedException();
            }
        }

        class Stub2 : IEquatibleNodesConverter
        {
            class Stub3 : INodesEquatible
            {
                double[] lat, lon;

                public Stub3(double[] lat, double[] lon)
                {
                    this.lat = lat;
                    this.lon = lon;
                }

                public double[] Lats
                {
                    get { return lat; }
                }

                public double[] Lons
                {
                    get { return lon; }
                }

                public bool Equals(INodes other)
                {
                    return Lats[0] == other.Lats[0];
                }

                public override int GetHashCode()
                {
                    int c =  lat[0].GetHashCode();
                    return c;
                }

                public override bool Equals(object obj)
                {
                    INodes i = obj as INodes;
                    if (i != null)
                        return this.Equals(i);
                    else
                        return base.Equals(obj);
                }
            }

            public INodesEquatible Covert(INodes nodes)
            {
                return new Stub3(nodes.Lats, nodes.Lons);
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
        public void InterpolationContextCacheDecoratorTest()
        {
            var iccd = new InterpolationContextCacheDecorator(new Stub1(), new Stub2());

            var c1 = new Stub3(1.0,1.0);
            var c2 = new Stub3(1.0,2.0);
            var c3 = new Stub3(2.0,2.0);
            var c4 = new Stub3(3.0,3.0);

            var cont1 = iccd.GetInterpolationContext(c1);
            var cont2 = iccd.GetInterpolationContext(c2);
            var cont3 = iccd.GetInterpolationContext(c3);
            var cont4 = iccd.GetInterpolationContext(c4);

            Assert.AreEqual((int)cont1,(int)cont2);
            Assert.AreNotEqual((int)cont1, (int)cont3);
            Assert.AreNotEqual((int)cont1, (int)cont4);
        }
    }
}

