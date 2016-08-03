using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Research.Science.FetchClimate2;
using System.Collections.Generic;
using Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints.LinearCombination;
using Microsoft.Research.Science.FetchClimate2.Utils;

namespace Microsoft.Research.Science.FetchClimate2.Tests.DataHandlers.ScatteredPoints
{
    [TestClass]
    public class HashBasedEquatibleRealValueNodesTests
    {
        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void HashBasedEquatibleRealValueNodesEqualsTest()
        {
            var c = new HashBasedEquatibleRealValueNodesConverter();

            IEquatable<RealValueNodes> n1 = c.Covert(new RealValueNodes(new double[] { 1.0, 1.0 }, new double[] { 1.0, 2.0 }, new double[] { 1.0, 5.0 }));
            IEquatable<RealValueNodes> n2 = c.Covert(new RealValueNodes(new double[] { 1.0, 1.0 }, new double[] { 1.0, 2.0 }, new double[] { 1.0, 5.0 }));
            IEquatable<RealValueNodes> n3 = c.Covert(new RealValueNodes(new double[] { 1.0, 1.0 }, new double[] { 1.0, 2.0 }, new double[] { 1.0, 4.0 }));
            IEquatable<RealValueNodes> n4 = c.Covert(new RealValueNodes(new double[] { 1.0, 3.0 }, new double[] { 1.0, 2.0 }, new double[] { 1.0, 5.0 }));

            Assert.IsTrue(n1.Equals(n2));
            Assert.IsTrue(n1.Equals(new RealValueNodes(new double[] { 1.0, 1.0 }, new double[] { 1.0, 2.0 }, new double[] { 1.0, 5.0 })));
            Assert.IsFalse(n1.Equals(n3));
            Assert.IsFalse(n1.Equals(n4));
            Assert.IsFalse(n4.Equals(n3));
            Assert.IsFalse(n2.Equals(n4));


        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        [ExpectedException(typeof(ArgumentException), "An item with the same key has already been added")]
        public void HashBasedEquatibleRealValueNodesDictionaryTest()
        {
            var c = new HashBasedEquatibleRealValueNodesConverter();

            IEquatable<RealValueNodes> n1 = c.Covert(new RealValueNodes(new double[] { 1.0, 1.0 }, new double[] { 1.0, 2.0 }, new double[] { 1.0, 5.0 }));
            IEquatable<RealValueNodes> n2 = c.Covert(new RealValueNodes(new double[] { 1.0, 1.0 }, new double[] { 1.0, 2.0 }, new double[] { 1.0, 5.0 }));

            Dictionary<IEquatable<RealValueNodes>, int> d = new Dictionary<IEquatable<RealValueNodes>, int>();
            d.Add(n1, 1);
            d.Add(n2, 1);            
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void HashBasedEquatibleRealValueNodesHashcodeTest()
        {
            var c = new HashBasedEquatibleRealValueNodesConverter();

            IEquatable<RealValueNodes> n1 = c.Covert(new RealValueNodes(new double[] { 1.0, 1.0 }, new double[] { 1.0, 2.0 }, new double[] { 1.0, 5.0 }));
            IEquatable<RealValueNodes> n2 = c.Covert(new RealValueNodes(new double[] { 1.0, 1.0 }, new double[] { 1.0, 2.0 }, new double[] { 1.0, 5.0 }));
            IEquatable<RealValueNodes> n3 = c.Covert(new RealValueNodes(new double[] { 1.0, 1.0 }, new double[] { 1.0, 2.0 }, new double[] { 1.0, 4.0 }));
            IEquatable<RealValueNodes> n4 = c.Covert(new RealValueNodes(new double[] { 1.0, 3.0 }, new double[] { 1.0, 2.0 }, new double[] { 1.0, 5.0 }));

            int h1 = n1.GetHashCode(), h2 = n2.GetHashCode(), h3 = n3.GetHashCode(), h4 = n4.GetHashCode();
            Assert.IsTrue(h1.Equals(h2));
            Assert.IsTrue(h1.Equals(c.Covert(new RealValueNodes(new double[] { 1.0, 1.0 }, new double[] { 1.0, 2.0 }, new double[] { 1.0, 5.0 })).GetHashCode()));
            Assert.IsFalse(h1.Equals(h3));
            Assert.IsFalse(h1.Equals(h4));
            Assert.IsFalse(n4.Equals(h3));
            Assert.IsFalse(n2.Equals(h4));


        }
    }
}
