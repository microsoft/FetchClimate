using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Research.Science.FetchClimate2;
using System.Collections.Generic;
using Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints.LinearCombination;

namespace Microsoft.Research.Science.FetchClimate2.Tests.DataHandlers.ScatteredPoints
{
    [TestClass]
    public class HashBasedEquatibleNodesTests
    {
        class NodesStub : INodes
        {
            double[] lat,lon;

            public NodesStub(double[] lats, double[] lons)
            {
                lat = lats;
                lon = lons;
            }

            public double[] Lats
            {
                get { return lat; }
            }

            public double[] Lons
            {
                get { return lon; }
            }
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void HashBasedEquatibleNodesTest1()
        {
            var c = new HashBasedEquatibleNodesConverter();

            INodesEquatible n1 = c.Covert(new NodesStub(new double[] { 1.0, 1.0 }, new double[] { 1.0, 2.0 }));
            INodesEquatible n2 = c.Covert(new NodesStub(new double[] { 1.0, 1.0 }, new double[] { 1.0, 2.0 }));
            INodesEquatible n3 = c.Covert(new NodesStub(new double[] { 1.0, 1.0, 3.0 }, new double[] { 1.0, 2.0,3.0 }));
            INodesEquatible n4 = c.Covert(new NodesStub(new double[] { 1.0, 3.0 }, new double[] { 1.0, 2.0 }));

            Assert.IsTrue(n1.Equals(n2));
            Assert.IsTrue(n1.Equals(new NodesStub(new double[] { 1.0, 1.0 }, new double[] { 1.0, 2.0 })));
            Assert.IsFalse(n1.Equals(n3));
            Assert.IsFalse(n1.Equals(n4));
            Assert.IsFalse(n4.Equals(n3));
            Assert.IsFalse(n2.Equals(n4));


        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        [ExpectedException(typeof(ArgumentException), "An item with the same key has already been added")]
        public void HashBasedEquatibleNodesTest2()
        {
            var c = new HashBasedEquatibleNodesConverter();

            INodesEquatible n1 = c.Covert(new NodesStub(new double[] { 1.0, 1.0 }, new double[] { 1.0, 2.0 }));
            INodesEquatible n2 = c.Covert(new NodesStub(new double[] { 1.0, 1.0 }, new double[] { 1.0, 2.0 }));            

            Dictionary<INodesEquatible, int> d = new Dictionary<INodesEquatible, int>();
            d.Add(n1, 1);
            d.Add(n2, 1);            
        }
    }
}
