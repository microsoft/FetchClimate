using Microsoft.Research.Science.FetchClimate2;
using Microsoft.Research.Science.FetchClimate2.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlersTests.UncertatintyEvaluators
{
    [TestClass]
    public class ThinningTests
    {
        [TestMethod]
        [TestCategory("BVT")]
        [TestCategory("Local")]
        public void ThinByTwoTest_RightEven()
        {            
            PrivateType pt = new PrivateType(typeof(Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators.ThinningUtils));
            IPs ips,res;

            ips = new IPs() { Indices = new int[] { 0, 1 }, Weights = new double[] { 0.2, 0.8} };
            res = (IPs)pt.InvokeStatic("ThinByTwo", ips, false);

            Assert.AreEqual(2, res.Indices.Length);
            Assert.AreEqual(0, res.Indices[0]);
            Assert.AreEqual(1, res.Indices[1]);
            Assert.AreEqual(0.2, res.Weights[0]);
            Assert.AreEqual(0.8, res.Weights[1]);
            Assert.AreEqual(ips.Weights.Sum(), res.Weights.Sum());

            ips = new IPs() { Indices = new int[] { 0, 1,2,3 }, Weights = new double[] { 0.5, 0.8, 0.7, 0.3 } };
            res = (IPs)pt.InvokeStatic("ThinByTwo", ips, false);

            Assert.AreEqual(3, res.Indices.Length);
            Assert.AreEqual(0, res.Indices[0]);
            Assert.AreEqual(1, res.Indices[1]);
            Assert.AreEqual(3, res.Indices[2]);
            Assert.AreEqual(0.5, res.Weights[0], TestConstants.DoublePrecision);
            Assert.AreEqual(1.15, res.Weights[1], TestConstants.DoublePrecision);
            Assert.AreEqual(0.65, res.Weights[2],TestConstants.DoublePrecision);
            Assert.AreEqual(ips.Weights.Sum(), res.Weights.Sum(), TestConstants.DoublePrecision);
            
            ips = new IPs() { Indices = new int[] { 0, 1, 2, 3,4,5 }, Weights = new double[] { 0.5, 0.8, 0.7, 0.3,0.4,0.6 } };
            res = (IPs)pt.InvokeStatic("ThinByTwo", ips, false);

            Assert.AreEqual(4, res.Indices.Length);
            Assert.AreEqual(0, res.Indices[0]);
            Assert.AreEqual(1, res.Indices[1]);
            Assert.AreEqual(3, res.Indices[2]);
            Assert.AreEqual(5, res.Indices[3]);
            Assert.AreEqual(0.5, res.Weights[0], TestConstants.DoublePrecision);
            Assert.AreEqual(1.15, res.Weights[1], TestConstants.DoublePrecision);
            Assert.AreEqual(0.85, res.Weights[2], TestConstants.DoublePrecision);
            Assert.AreEqual(0.8, res.Weights[3], TestConstants.DoublePrecision);
            Assert.AreEqual(ips.Weights.Sum(), res.Weights.Sum(), TestConstants.DoublePrecision);
        }

        [TestMethod]
        [TestCategory("BVT")]
        [TestCategory("Local")]
        public void ThinByTwoTest_LeftEven()
        {
            PrivateType pt = new PrivateType(typeof(Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators.ThinningUtils));
            IPs ips, res;

            ips = new IPs() { Indices = new int[] { 0, 1 }, Weights = new double[] { 0.2, 0.8 } };
            res = (IPs)pt.InvokeStatic("ThinByTwo", ips, true);

            Assert.AreEqual(2, res.Indices.Length);
            Assert.AreEqual(0, res.Indices[0]);
            Assert.AreEqual(1, res.Indices[1]);
            Assert.AreEqual(0.2, res.Weights[0]);
            Assert.AreEqual(0.8, res.Weights[1]);
            Assert.AreEqual(ips.Weights.Sum(), res.Weights.Sum(), TestConstants.DoublePrecision);

            ips = new IPs() { Indices = new int[] { 0, 1, 2, 3 }, Weights = new double[] { 0.5, 0.8, 0.7, 0.3 } };
            res = (IPs)pt.InvokeStatic("ThinByTwo", ips, true);

            Assert.AreEqual(3, res.Indices.Length);
            Assert.AreEqual(0, res.Indices[0]);
            Assert.AreEqual(2, res.Indices[1]);
            Assert.AreEqual(3, res.Indices[2]);
            Assert.AreEqual(0.9, res.Weights[0], TestConstants.DoublePrecision);
            Assert.AreEqual(1.1, res.Weights[1], TestConstants.DoublePrecision);
            Assert.AreEqual(0.3, res.Weights[2], TestConstants.DoublePrecision);
            Assert.AreEqual(ips.Weights.Sum(), res.Weights.Sum(), TestConstants.DoublePrecision);

            ips = new IPs() { Indices = new int[] { 0, 1, 2, 3, 4, 5 }, Weights = new double[] { 0.5, 0.8, 0.7, 0.3, 0.4, 0.6 } };
            res = (IPs)pt.InvokeStatic("ThinByTwo", ips, true);

            Assert.AreEqual(4, res.Indices.Length);
            Assert.AreEqual(0, res.Indices[0]);
            Assert.AreEqual(2, res.Indices[1]);
            Assert.AreEqual(4, res.Indices[2]);
            Assert.AreEqual(5, res.Indices[3]);
            Assert.AreEqual(0.9, res.Weights[0], TestConstants.DoublePrecision);
            Assert.AreEqual(1.25, res.Weights[1], TestConstants.DoublePrecision);
            Assert.AreEqual(0.55, res.Weights[2], TestConstants.DoublePrecision);
            Assert.AreEqual(0.6, res.Weights[3], TestConstants.DoublePrecision);
            Assert.AreEqual(ips.Weights.Sum(), res.Weights.Sum(), TestConstants.DoublePrecision);
        }

        [TestMethod]
        [TestCategory("BVT")]
        [TestCategory("Local")]
        public void ThinByTwoTest_RightOdd()
        {
            PrivateType pt = new PrivateType(typeof(Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators.ThinningUtils));
            IPs ips, res;

            ips = new IPs() { Indices = new int[] { 0, 1 ,2}, Weights = new double[] { 0.2, 0.8, 0.3} };
            res = (IPs)pt.InvokeStatic("ThinByTwo", ips, false);

            Assert.AreEqual(2, res.Indices.Length);
            Assert.AreEqual(0, res.Indices[0]);
            Assert.AreEqual(2, res.Indices[1]);
            Assert.AreEqual(0.6, res.Weights[0], TestConstants.DoublePrecision);
            Assert.AreEqual(0.7, res.Weights[1], TestConstants.DoublePrecision);
            Assert.AreEqual(ips.Weights.Sum(), res.Weights.Sum(), TestConstants.DoublePrecision);

            ips = new IPs() { Indices = new int[] { 0, 1, 2, 3,4 }, Weights = new double[] { 0.5, 0.8, 0.7, 0.3 ,0.5} };
            res = (IPs)pt.InvokeStatic("ThinByTwo", ips, false);

            Assert.AreEqual(3, res.Indices.Length);
            Assert.AreEqual(0, res.Indices[0]);
            Assert.AreEqual(2, res.Indices[1]);
            Assert.AreEqual(4, res.Indices[2]);
            Assert.AreEqual(0.9, res.Weights[0], TestConstants.DoublePrecision);
            Assert.AreEqual(1.25, res.Weights[1], TestConstants.DoublePrecision);
            Assert.AreEqual(0.65, res.Weights[2], TestConstants.DoublePrecision);
            Assert.AreEqual(ips.Weights.Sum(), res.Weights.Sum(), TestConstants.DoublePrecision);            
        }

        [TestMethod]
        [TestCategory("BVT")]
        [TestCategory("Local")]
        public void ThinByTwoTest_LeftOdd()
        {
            PrivateType pt = new PrivateType(typeof(Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators.ThinningUtils));
            IPs ips, res;

            ips = new IPs() { Indices = new int[] { 0, 1, 2 }, Weights = new double[] { 0.2, 0.8, 0.3 } };
            res = (IPs)pt.InvokeStatic("ThinByTwo", ips, true);

            Assert.AreEqual(2, res.Indices.Length);
            Assert.AreEqual(0, res.Indices[0]);
            Assert.AreEqual(2, res.Indices[1]);
            Assert.AreEqual(0.6, res.Weights[0], TestConstants.DoublePrecision);
            Assert.AreEqual(0.7, res.Weights[1], TestConstants.DoublePrecision);
            Assert.AreEqual(ips.Weights.Sum(), res.Weights.Sum(), TestConstants.DoublePrecision);

            ips = new IPs() { Indices = new int[] { 0, 1, 2, 3, 4 }, Weights = new double[] { 0.5, 0.8, 0.7, 0.3, 0.5 } };
            res = (IPs)pt.InvokeStatic("ThinByTwo", ips, true);

            Assert.AreEqual(3, res.Indices.Length);
            Assert.AreEqual(0, res.Indices[0]);
            Assert.AreEqual(2, res.Indices[1]);
            Assert.AreEqual(4, res.Indices[2]);
            Assert.AreEqual(0.9, res.Weights[0], TestConstants.DoublePrecision);
            Assert.AreEqual(1.25, res.Weights[1], TestConstants.DoublePrecision);
            Assert.AreEqual(0.65, res.Weights[2], TestConstants.DoublePrecision);
            Assert.AreEqual(ips.Weights.Sum(), res.Weights.Sum(), TestConstants.DoublePrecision);
        }
    }
}



