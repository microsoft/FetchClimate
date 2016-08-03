using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Research.Science.FetchClimate2.Tests.DataHandlers.VirtualVariables
{
    [TestClass]
    public class VirtualVariablesTests
    {
        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void VirtualVariablesInfrastructureTest()
        {
            FetchRequest requestA = new FetchRequest("A", FetchDomain.CreatePointGrid(
                new double[] { 1.0, 2.0 },
                new double[] { 2.0, 3.0 },
                new TimeRegion()));

            VirtVarRequestContextStub contextStubA = new VirtVarRequestContextStub(requestA);

            var handler = new VirtVarTests.FunctionClass();
            var result = handler.ProcessRequestAsync(contextStubA).Result;

            // x1+ 2*x2 + 3*x3
            // args: 3,5,7,11,13,17
            double expected = 3 + 2 * 5 + 3 * 7;
            foreach (var item in result)
                Assert.AreEqual(expected, (double)item, TestConstants.DoublePrecision);





            FetchRequest requestE = new FetchRequest("E", FetchDomain.CreatePointGrid(
                new double[] { 1.0, 2.0 },
                new double[] { 2.0, 3.0 },
                new TimeRegion()));

            VirtVarRequestContextStub contextStubE = new VirtVarRequestContextStub(requestE);

            handler = new VirtVarTests.FunctionClass();
            result = handler.ProcessRequestAsync(contextStubE).Result;

            // 4*x1 + x2 + 2*x3 + 3*x4
            // args: 3,5,7,11,13,17
            expected = 4 * 3 + 5 + 2 * 7 + 3 * 11;
            foreach (var item in result)
                Assert.AreEqual(expected, (double)item, TestConstants.DoublePrecision);




            FetchRequest requestX = new FetchRequest("X", FetchDomain.CreatePointGrid(
                new double[] { 1.0, 2.0 },
                new double[] { 2.0, 3.0 },
                new TimeRegion()));

            VirtVarRequestContextStub contextStubX = new VirtVarRequestContextStub(requestX);

            handler = new VirtVarTests.FunctionClass();
            result = handler.ProcessRequestAsync(contextStubX).Result;

            // x1 - 2 * x2
            // unc: x1 + 2 * x2
            // args: 3,5,7,11,13,17
            expected = 3 - 2 * 5;
            foreach (var item in result)
                Assert.AreEqual(expected, (double)item, TestConstants.DoublePrecision);
        }
    }
}
