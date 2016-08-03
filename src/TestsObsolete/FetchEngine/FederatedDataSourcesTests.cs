using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Research.Science.FetchClimate2;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Research.Science.Data;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Tests.FetchEngine
{   
    [TestClass]
    public class FederatedDataSourcesTests
    {
        /// <summary>
        /// Emulates requests to remote fetch climates. Serves only pointset requests without timeseries
        /// </summary>
        class FederatedDsTestingFetchEngine : FetchClimate2.FetchEngine
        {
            Dictionary<string, StaticValuesForRectDataHandler[]> remoteDataSources;
            public FederatedDsTestingFetchEngine(IExtendedConfigurationProvider confProvider, Dictionary<string, StaticValuesForRectDataHandler[]> remoteDataSources)
                : base(confProvider)
            {
                this.remoteDataSources = remoteDataSources;
            }

            public override async System.Threading.Tasks.Task<FetchResponseWithProvenance> PerformRemoteRequestAsync(string remoteServiceURI, FetchRequest request)
            {
                int pointsCount = request.Domain.Lats.Length;

                string[] requestedDsNames =null;
                if(request.ParticularDataSource!=null)
                    requestedDsNames = request.ParticularDataSource.Select(name => name.Substring(0,name.Length-"_remote".Length)).ToArray();

                StaticValuesForRectDataHandler[] handlers = remoteDataSources[remoteServiceURI];
                if (requestedDsNames != null)
                    handlers = handlers.Where(ds => requestedDsNames.Any(name => ds.GetType().Name.Contains(name))).ToArray();

                StandaloneRequestContext[] contextes = new StandaloneRequestContext[handlers.Length];
                for (ushort i = 0; i < contextes.Length; i++)
                    contextes[i] = new StandaloneRequestContext(request, this, null, DataSet.Open("msds:memory"), i);

                Array[] vals = new Array[contextes.Length];
                Array[] uncs = new Array[contextes.Length];
                for (int i = 0; i < contextes.Length; i++)
                {
                    vals[i] = await handlers[i].ProcessRequestAsync(contextes[i]);
                    uncs[i] = contextes[i].ReportUncertainty();                    
                }

                
                Array res = Array.CreateInstance(typeof(double), pointsCount );
                Array unc = Array.CreateInstance(typeof(double), pointsCount );
                Array prov = Array.CreateInstance(typeof(ushort), pointsCount);
                for (int i = 0; i < pointsCount; i++)
                {
                    double minUnc = (double)(uncs[0].GetValue(i));
                    for (ushort j = 0; j < contextes.Length; j++)
                    {
                        if (((double)uncs[j].GetValue(i)) <= minUnc)
                        {
                            res.SetValue(vals[j].GetValue(i), i);
                            unc.SetValue(uncs[j].GetValue(i), i);
                            prov.SetValue(j, i);
                        }
                    }
                    
                }

                return new FetchResponseWithProvenance(request, res, unc, prov);
            }
        }

        /// <summary>
        /// serves predefined configuration with 2 remote service (hosting 2 and 1 data sources respectivle) and 1 local data source
        /// </summary>
        class FederationTestConfigurationProvider : IExtendedConfigurationProvider
        {
            public DateTime GetExactTimestamp(DateTime utcTimestamp)
            {
                return new DateTime();
            }

            public ExtendedConfiguration GetConfiguration(DateTime utcTime)
            {
                VariableDefinition bDef = new VariableDefinition("b", "bUnits", "b");
                var map = new System.Collections.Generic.Dictionary<string, string>();
                map.Add("a", "b");

                return new ExtendedConfiguration(new DateTime(0, DateTimeKind.Utc),
                    new ExtendedDataSourceDefinition[]
                {
                new ExtendedDataSourceDefinition(0,"20_40","20_40","","msds:memory",typeof(Static_20_40_Handler).AssemblyQualifiedName,new string[]{"a"},"(local data source)",map,null,0),
                new ExtendedDataSourceDefinition(1,"25_45","25_45","","http://1/",null,new string[]{"a"},"1",map,"25_45_remote",0),
                new ExtendedDataSourceDefinition(2,"30_50","30_50","","http://1/",null,new string[]{"a"},"1",map,"30_50_remote",1),
                new ExtendedDataSourceDefinition(3,"35_55","35_55","","http://2/",null,new string[]{"a"},"2",map,"35_55_remote",0),
                }
                    , new VariableDefinition[]
                {
                    bDef
                });
            }
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public async Task FederatedFetchingTest()
        {
            FederationTestConfigurationProvider provider = new FederationTestConfigurationProvider();
            Dictionary<string,StaticValuesForRectDataHandler[]> federatedDsDict = new Dictionary<string,StaticValuesForRectDataHandler[]>();
            federatedDsDict["http://1/"] = new StaticValuesForRectDataHandler[] {new Static_25_45_Handler(),new Static_30_50_Handler()};
            federatedDsDict["http://2/"] = new StaticValuesForRectDataHandler[] {new Static_35_55_Handler()};
            FederatedDsTestingFetchEngine fetchEngine = new FederatedDsTestingFetchEngine(provider, federatedDsDict);

            FetchDomain fd = FetchDomain.CreatePoints(new double[] { 5.0, 5.0, 5.0, 5.0 }, new double[] { 37.0,42.0,47.0,52.0}, new TimeRegion());

            FetchRequest request = new FetchRequest("a", fd);            

            FetchResponseWithProvenance result =  await fetchEngine.PerformRequestAsync(request);

            Assert.AreEqual(-50.0, result.Values.GetValue(0));
            Assert.AreEqual(50.0, result.Uncertainty.GetValue(0));
            Assert.AreEqual((ushort)0, result.Provenance.GetValue(0));

            Assert.AreEqual(-60.0, result.Values.GetValue(1)); //serve
            Assert.AreEqual(60.0, result.Uncertainty.GetValue(1));
            Assert.AreEqual((ushort)1, result.Provenance.GetValue(1));

            Assert.AreEqual(-70.0, result.Values.GetValue(2));
            Assert.AreEqual(70.0, result.Uncertainty.GetValue(2));
            Assert.AreEqual((ushort)2, result.Provenance.GetValue(2));

            Assert.AreEqual(-80.0, result.Values.GetValue(3));
            Assert.AreEqual(80.0, result.Uncertainty.GetValue(3));
            Assert.AreEqual((ushort)3, result.Provenance.GetValue(3));
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public async Task FederatedFetchingNanHandlingTest()
        {
            FederationTestConfigurationProvider provider = new FederationTestConfigurationProvider();
            Dictionary<string, StaticValuesForRectDataHandler[]> federatedDsDict = new Dictionary<string, StaticValuesForRectDataHandler[]>();
            federatedDsDict["http://1/"] = new StaticValuesForRectDataHandler[] { new Static_25_45_Handler(), new StaticNanYielding_30_50_Handler() };
            federatedDsDict["http://2/"] = new StaticValuesForRectDataHandler[] { new Static_35_55_Handler() };
            FederatedDsTestingFetchEngine fetchEngine = new FederatedDsTestingFetchEngine(provider, federatedDsDict);

            FetchDomain fd = FetchDomain.CreatePoints(new double[] { 5.0, 5.0, 5.0, 5.0 }, new double[] { 37.0, 42.0, 47.0, 52.0 }, new TimeRegion());

            FetchRequest request = new FetchRequest("a", fd);

            FetchResponseWithProvenance result = await fetchEngine.PerformRequestAsync(request);

            Assert.AreEqual(-50.0, result.Values.GetValue(0));
            Assert.AreEqual(50.0, result.Uncertainty.GetValue(0));
            Assert.AreEqual((ushort)0, result.Provenance.GetValue(0));

            Assert.AreEqual(-60.0, result.Values.GetValue(1)); //serve
            Assert.AreEqual(60.0, result.Uncertainty.GetValue(1));
            Assert.AreEqual((ushort)1, result.Provenance.GetValue(1));

            Assert.AreEqual(-80.0, result.Values.GetValue(2)); //here instead of StaticNanYielding_30_50_Handler, Static_35_55_Handler must provide results
            Assert.AreEqual(80.0, result.Uncertainty.GetValue(2));
            Assert.AreEqual((ushort)3, result.Provenance.GetValue(2));

            Assert.AreEqual(-80.0, result.Values.GetValue(3));
            Assert.AreEqual(80.0, result.Uncertainty.GetValue(3));
            Assert.AreEqual((ushort)3, result.Provenance.GetValue(3));
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public async Task FeOneLocalOneFederatedTest()
        {
            FederationTestConfigurationProvider provider = new FederationTestConfigurationProvider();
            Dictionary<string, StaticValuesForRectDataHandler[]> federatedDsDict = new Dictionary<string, StaticValuesForRectDataHandler[]>();
            federatedDsDict["http://1/"] = new StaticValuesForRectDataHandler[] { new Static_25_45_Handler(), new Static_30_50_Handler() };
            federatedDsDict["http://2/"] = new StaticValuesForRectDataHandler[] { new Static_35_55_Handler() };
            FederatedDsTestingFetchEngine fetchEngine = new FederatedDsTestingFetchEngine(provider, federatedDsDict);

            FetchDomain fd = FetchDomain.CreatePoints(new double[] { 5.0, 5.0, 5.0, 5.0 }, new double[] { 37.0, 42.0, 47.0, 52.0 }, new TimeRegion());

            FetchRequest request = new FetchRequest("a", fd, new string[] {"35_55","20_40"});

            FetchResponseWithProvenance result = await fetchEngine.PerformRequestAsync(request);

            Assert.AreEqual(-50.0, result.Values.GetValue(0));
            Assert.AreEqual(50.0, result.Uncertainty.GetValue(0));
            Assert.AreEqual((ushort)0, result.Provenance.GetValue(0));

            Assert.AreEqual(-80.0, result.Values.GetValue(1));
            Assert.AreEqual(80.0, result.Uncertainty.GetValue(1));
            Assert.AreEqual((ushort)3, result.Provenance.GetValue(1));

            Assert.AreEqual(-80.0, result.Values.GetValue(2));
            Assert.AreEqual(80.0, result.Uncertainty.GetValue(2));
            Assert.AreEqual((ushort)3, result.Provenance.GetValue(2));

            Assert.AreEqual(-80.0, result.Values.GetValue(3));
            Assert.AreEqual(80.0, result.Uncertainty.GetValue(3));
            Assert.AreEqual((ushort)3, result.Provenance.GetValue(3));
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public async Task FetchingSingleDataSourceTest()
        {
            FederationTestConfigurationProvider provider = new FederationTestConfigurationProvider();
            Dictionary<string, StaticValuesForRectDataHandler[]> federatedDsDict = new Dictionary<string, StaticValuesForRectDataHandler[]>();
            federatedDsDict["http://1/"] = new StaticValuesForRectDataHandler[] { new Static_25_45_Handler(), new Static_30_50_Handler() };
            federatedDsDict["http://2/"] = new StaticValuesForRectDataHandler[] { new Static_35_55_Handler() };
            FederatedDsTestingFetchEngine fetchEngine = new FederatedDsTestingFetchEngine(provider, federatedDsDict);

            FetchDomain fd = FetchDomain.CreatePoints(new double[] { 5.0}, new double[] { 37.0}, new TimeRegion());

            FetchRequest request = new FetchRequest("a", fd, new string[] {"20_40"}); //local

            FetchResponseWithProvenance result = await fetchEngine.PerformRequestAsync(request);

            Assert.AreEqual(-50.0, result.Values.GetValue(0));
            Assert.AreEqual(50.0, result.Uncertainty.GetValue(0));
            Assert.AreEqual((ushort)0, result.Provenance.GetValue(0));


            request = new FetchRequest("a", fd, new string[] { "35_55" }); //federated

            result = await fetchEngine.PerformRequestAsync(request);

            Assert.AreEqual(-80.0, result.Values.GetValue(0));
            Assert.AreEqual(80.0, result.Uncertainty.GetValue(0));
            Assert.AreEqual((ushort)3, result.Provenance.GetValue(0));
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task FetchingUnregisteredDataSourceTest()
        {
            FederationTestConfigurationProvider provider = new FederationTestConfigurationProvider();
            Dictionary<string, StaticValuesForRectDataHandler[]> federatedDsDict = new Dictionary<string, StaticValuesForRectDataHandler[]>();
            federatedDsDict["http://1/"] = new StaticValuesForRectDataHandler[] { new Static_25_45_Handler(), new Static_30_50_Handler() };
            federatedDsDict["http://2/"] = new StaticValuesForRectDataHandler[] { new Static_35_55_Handler() };
            FederatedDsTestingFetchEngine fetchEngine = new FederatedDsTestingFetchEngine(provider, federatedDsDict);

            FetchDomain fd = FetchDomain.CreatePoints(new double[] { 5.0 }, new double[] { 37.0 }, new TimeRegion());

            FetchRequest request = new FetchRequest("a", fd, new string[] { "20_40_unregistered" });

            await fetchEngine.PerformRequestAsync(request);            
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public async Task TwoFederatedDsOnSingleRemoteServiceTest()
        {
            FederationTestConfigurationProvider provider = new FederationTestConfigurationProvider();
            Dictionary<string, StaticValuesForRectDataHandler[]> federatedDsDict = new Dictionary<string, StaticValuesForRectDataHandler[]>();
            federatedDsDict["http://1/"] = new StaticValuesForRectDataHandler[] { new Static_25_45_Handler(), new Static_30_50_Handler() };
            federatedDsDict["http://2/"] = new StaticValuesForRectDataHandler[] { new Static_35_55_Handler() };
            FederatedDsTestingFetchEngine fetchEngine = new FederatedDsTestingFetchEngine(provider, federatedDsDict);

            FetchDomain fd = FetchDomain.CreatePoints(new double[] { 5.0, 5.0, 5.0, 5.0 }, new double[] { 37.0, 42.0, 47.0, 52.0 }, new TimeRegion());

            FetchRequest request = new FetchRequest("a", fd, new string[] { "25_45","30_50" });

            FetchResponseWithProvenance result = await fetchEngine.PerformRequestAsync(request);

            Assert.AreEqual(-60.0, result.Values.GetValue(0));
            Assert.AreEqual(60.0, result.Uncertainty.GetValue(0));
            Assert.AreEqual((ushort)1, result.Provenance.GetValue(0));

            Assert.AreEqual(-60.0, result.Values.GetValue(1));
            Assert.AreEqual(60.0, result.Uncertainty.GetValue(1));
            Assert.AreEqual((ushort)1, result.Provenance.GetValue(1));

            Assert.AreEqual(-70.0, result.Values.GetValue(2));
            Assert.AreEqual(70.0, result.Uncertainty.GetValue(2));
            Assert.AreEqual((ushort)2, result.Provenance.GetValue(2));

            Assert.AreEqual(-160.0, result.Values.GetValue(3));
            Assert.AreEqual(160.0, result.Uncertainty.GetValue(3));
            Assert.AreEqual((ushort)1, result.Provenance.GetValue(3));

        }
    }
}
