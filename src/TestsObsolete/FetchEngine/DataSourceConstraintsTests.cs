using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Research.Science.FetchClimate2.Tests.FetchEngine
{
    class DsConstraintsTestConfigurationProvider : IExtendedConfigurationProvider
    {
        public DateTime GetExactTimestamp(DateTime utcTimestamp)
        {
            return new DateTime();
        }

        public ExtendedConfiguration GetConfiguration(DateTime utcTime)
        {
            VariableDefinition bDef = new VariableDefinition("b", "bUnits", "b");
            var map = new System.Collections.Generic.Dictionary<string,string>();
            map.Add("a","b");

            return new ExtendedConfiguration(new DateTime(0, DateTimeKind.Utc),
                new ExtendedDataSourceDefinition[]
                {
                new ExtendedDataSourceDefinition(0,"20_40","20_40","","msds:memory",typeof(Static_20_40_Handler).AssemblyQualifiedName,new string[]{"a"},"(local data source)",map,null,0),
                new ExtendedDataSourceDefinition(1,"25_45","25_45","","msds:memory",typeof(Static_25_45_Handler).AssemblyQualifiedName,new string[]{"a"},"(local data source)",map,null,0),
                new ExtendedDataSourceDefinition(2,"30_50","30_50","","msds:memory",typeof(Static_30_50_Handler).AssemblyQualifiedName,new string[]{"a"},"(local data source)",map,null,0),
                }
                , new VariableDefinition[]
                {
                    bDef
                });
        }
    }

  
    

    [TestClass]
    public class DataSourceConstraintsTests
    {
        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public async Task SelectLowestUncertatintyAmongPermitedTest()
        {
            DsConstraintsTestConfigurationProvider provider = new DsConstraintsTestConfigurationProvider();
            FetchClimate2.FetchEngine fetchEngine = new FetchClimate2.FetchEngine(provider);

            FetchDomain fd = FetchDomain.CreatePoints(new double[] {5.0},new double[] {35.0}, new TimeRegion());

            FetchRequest unconstraintRequest = new FetchRequest("a",fd);
            FetchRequest constraintRequest = new FetchRequest("a",fd,new string[] {"25_45","30_50"});
            
            Assert.AreEqual(-50.0, (await fetchEngine.PerformRequestAsync(unconstraintRequest)).Values.GetValue(0));
            Assert.AreEqual(-60.0, (await fetchEngine.PerformRequestAsync(constraintRequest)).Values.GetValue(0));
        }
    }
}
