using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Collections.Generic;
using DataHandlersTests;

namespace Microsoft.Research.Science.FetchClimate2.Tests.DataHandlers.UncertatintyEvaluators
{
    [TestClass]
    public class DecoratorsTests
    {
        class UncEvalStub : IBatchUncertaintyEvaluator
        {
            public async System.Threading.Tasks.Task<double[]> EvaluateCellsBatchAsync(IEnumerable<ICellRequest> cells)
            {
                return cells.Select(c => c.LatMax).ToArray();
            }
        }

        [TestMethod]
        [TestCategory("BVT")]
        [TestCategory("Local")]
        public async Task TestLinearTransformDecorator()
        {
            var component = new UncEvalStub();

            var dec = new Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators.LinearTransformDecorator(component);
            dec.SetTranform("a", b => b * 3 + 7.0);

            var storage = TestDataStorageFactory.GetStorageContext("msds:memory");

            FetchRequest fr = new FetchRequest("a", FetchDomain.CreatePoints(new double[] {5.0}, new double[] {-11.0},new TimeRegion()));

            IRequestContext rcs = RequestContextStub.GetStub(storage, fr);

            var res = await dec.EvaluateCellsBatchAsync(new RequestStubs[] { new RequestStubs() { LatMax = 5.0, LatMin = 5.0, LonMax = -11.0, LonMin = -11.0, Time = new TimeSegment(), VariableName = "a" } });

            Assert.AreEqual(22.0, res[0]);
        }
    }
}
