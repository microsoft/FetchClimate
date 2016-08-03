using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Tests.DataHandlers.ValueAggregators
{
    [TestClass]
    public class DecoratorsTests
    {
        class ValAggStub : IBatchValueAggregator
        {            
            public async System.Threading.Tasks.Task<double[]> AggregateCellsBatchAsync(IRequestContext context, System.Collections.Generic.IEnumerable<GeoCellTuple> cells)
            {
                return cells.Select(c => c.LatMax).ToArray();
            }
        }
        
        [TestMethod]
        [TestCategory("BVT")]
        [TestCategory("Local")]
        public async Task TestLinearTransformDecorator()
        {
            var component = new ValAggStub();

            var storage = TestDataStorageFactory.GetStorage("msds:memory");

            storage.StorageDefinition.VariablesMetadata.Add("a", new MetaDataDictionary());
            storage.StorageDefinition.VariablesMetadata["a"]["scale_factor"] = 13.0;
            storage.StorageDefinition.VariablesMetadata["a"]["add_offset"] = 17.0;

            var dec = new Microsoft.Research.Science.FetchClimate2.ValueAggregators.LinearTransformDecorator(storage, component);
            dec.SetAdditionalTranform("a", b => b * 3 + 7.0);

            FetchRequest fr = new FetchRequest("a", FetchDomain.CreatePoints(new double[] { 5.0 }, new double[] { -11.0 }, new TimeRegion()));

            IRequestContext rcs = RequestContextStub.GetStub(storage, fr);
            
            var res = await dec.AggregateCellsBatchAsync(rcs, new GeoCellTuple[] { new GeoCellTuple() { LatMax = 5.0, LatMin = 5.0, LonMax = -11.0, LonMin = -11.0, Time = new TimeSegment() } });

            Assert.AreEqual(253.0, res[0]);
        }
    }
}
