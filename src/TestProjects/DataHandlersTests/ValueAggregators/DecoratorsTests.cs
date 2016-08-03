using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using DataHandlersTests;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Research.Science.FetchClimate2.Tests.DataHandlers.ValueAggregators
{
    [TestClass]
    public class DecoratorsTests
    {
        class ValAggStub : IBatchValueAggregator
        {            
            public async System.Threading.Tasks.Task<double[]> AggregateCellsBatchAsync(System.Collections.Generic.IEnumerable<ICellRequest> cells)
            {
                return cells.Select(c => c.LatMax).ToArray();
            }
        }

        class Stub : IDataStorageDefinition
        {

            public System.Collections.ObjectModel.ReadOnlyDictionary<string, object> GlobalMetadata
            {
                get { throw new NotImplementedException(); }
            }

            public System.Collections.ObjectModel.ReadOnlyDictionary<string, System.Collections.ObjectModel.ReadOnlyDictionary<string, object>> VariablesMetadata
            {
                get {
                    Dictionary<string, ReadOnlyDictionary<string, object>> dict = new Dictionary<string, ReadOnlyDictionary<string, object>>();
                    Dictionary<string, object> dict2 = new Dictionary<string, object>();
                    dict.Add("a", new ReadOnlyDictionary<string, object>(dict2));
                    dict2["scale_factor"] = 13.0;
                    dict2["add_offset"] = 17.0;
                    return new ReadOnlyDictionary<string, ReadOnlyDictionary<string, object>>(dict);
                }
            }

            public System.Collections.ObjectModel.ReadOnlyDictionary<string, string[]> VariablesDimensions
            {
                get { throw new NotImplementedException(); }
            }

            public System.Collections.ObjectModel.ReadOnlyDictionary<string, Type> VariablesTypes
            {
                get { throw new NotImplementedException(); }
            }

            public System.Collections.ObjectModel.ReadOnlyDictionary<string, int> DimensionsLengths
            {
                get { throw new NotImplementedException(); }
            }
        }

        class Stub2 : IStorageContext {

            public IDataStorageDefinition StorageDefinition
            {
                get { return new Stub(); }
            }

            public Task<IStorageResponse[]> GetDataAsync(params IStorageRequest[] requests)
            {
                throw new NotImplementedException();
            }

            public Task<Array> GetDataAsync(string variableName, int[] origin = null, int[] stride = null, int[] shape = null)
            {
                throw new NotImplementedException();
            }
        }
        
        [TestMethod]
        [TestCategory("BVT")]
        [TestCategory("Local")]
        public async Task TestLinearTransformDecorator()
        {            

            var component = new ValAggStub();

            var storage = new Stub2();

            var dec = new Microsoft.Research.Science.FetchClimate2.ValueAggregators.LinearTransformDecorator(storage, component);
            dec.SetAdditionalTranform("a", b => b * 3 + 7.0);

            FetchRequest fr = new FetchRequest("a", FetchDomain.CreatePoints(new double[] { 5.0 }, new double[] { -11.0 }, new TimeRegion()));

            IRequestContext rcs = RequestContextStub.GetStub(storage, fr);

            var res = await dec.AggregateCellsBatchAsync(new RequestStubs[] { new RequestStubs() { LatMax = 5.0, LatMin = 5.0, LonMax = -11.0, LonMin = -11.0, Time = new TimeSegment(), VariableName="a" } });

            Assert.AreEqual(253.0, res[0]);
        }
    }
}
