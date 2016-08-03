using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Research.Science.FetchClimate2;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace DataHandlersTests.ValueAggregators
{
    [TestClass]
    public class MissingValueDictionaryTests
    {
        class DefinitionStub : IDataStorageDefinition
        {
            public System.Collections.ObjectModel.ReadOnlyDictionary<string, object> GlobalMetadata
            {
                get { throw new NotImplementedException(); }
            }

            public System.Collections.ObjectModel.ReadOnlyDictionary<string, System.Collections.ObjectModel.ReadOnlyDictionary<string, object>> VariablesMetadata
            {
                get {
                    var a = new Dictionary<string, object>();
                    a.Add("missing_value", -999);
                    var b = new ReadOnlyDictionary<string,object>(a);
                    var c = new Dictionary<string,ReadOnlyDictionary<string,object>>();
                    c.Add("a",b);
                    return new ReadOnlyDictionary<string,ReadOnlyDictionary<string,object>>(c);
                }
            }

            public System.Collections.ObjectModel.ReadOnlyDictionary<string, string[]> VariablesDimensions
            {
                get { throw new NotImplementedException(); }
            }

            public System.Collections.ObjectModel.ReadOnlyDictionary<string, Type> VariablesTypes
            {
                get {
                    var a = new Dictionary<string, Type>();
                    a.Add("a", typeof(double));
                    return new ReadOnlyDictionary<string, Type>(a);
                }
            }

            public System.Collections.ObjectModel.ReadOnlyDictionary<string, int> DimensionsLengths
            {
                get { throw new NotImplementedException(); }
            }
        }

        [TestCategory("BVT")]
        [TestCategory("Local")]
        [TestMethod]
        public void TestMV_DiffTypeInDefinition()
        {
            var mv = new MissingValuesDictionary(new DefinitionStub());
            Assert.AreEqual(-999.0,mv["a"]);
        }

        [TestCategory("BVT")]
        [TestCategory("Local")]
        [TestMethod]
        public void TestMV_DiffTypeInExplicitSet()
        {
            var mv = new MissingValuesDictionary(new DefinitionStub());
            mv["a"] = (short)-333;
            Assert.AreEqual(-333.0, mv["a"]);
        }
    }
}
