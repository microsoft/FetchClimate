using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Research.Science.Data.Factory;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Tests.FetchEngine
{    

    [TestClass]
    public class DataSourceHandlerCacheTest
    {
        class HandlerStubSyncroNoContext : DataHandlerFacade
        {
            public HandlerStubSyncroNoContext()
                :base(null,null,null)
            { }
        }

        class HandlerStubSyncroWithContext : DataHandlerFacade
        {
            public HandlerStubSyncroWithContext(IStorageContext context)
                : base(context, null, null)
            { }
        }

        class HandlerStubAsyncWithContext : DataHandlerFacade
        {
            public static async Task<HandlerStubAsyncWithContext> CreateAsync(IStorageContext dataContext)
            {
                return new HandlerStubAsyncWithContext(dataContext);
            }

            private HandlerStubAsyncWithContext(IStorageContext dataContext)
                : base(dataContext, null, null)
            {

            }
        }

        class HandlerStubAsyncNoContext : DataHandlerFacade
        {
            public static async Task<HandlerStubAsyncNoContext> CreateAsync()
            {
                return new HandlerStubAsyncNoContext();
            }

            private HandlerStubAsyncNoContext()
                : base(null, null, null)
            {

            }
        }

        [ClassInitialize]
        public static void ClassInitialize(TestContext dummy)
        {
            DataSetFactory.Register(typeof(Microsoft.Research.Science.Data.Azure.AzureDataSet));
        }
        
        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        [DeploymentItem("GHCNv2_part.nc")]
        public async Task DataSourceHandlerCacheCloneToMemoryTest()
        {
            string GhcnFullName = typeof(HandlerStubSyncroNoContext).AssemblyQualifiedName;
            var ds = await DataSourceHandlerCache.GetInstanceAsync(GhcnFullName, "msds:nc?file=GHCNv2_part.nc&openMode=readOnly");
            Assert.IsTrue(ds.Storage.GetType().ToString().ToLower().Contains("netcdf"));
            var ds2 = await DataSourceHandlerCache.GetInstanceAsync(GhcnFullName, "msds:nc?file=GHCNv2_part.nc&openMode=readOnly&cloneToMemory=true");
            Assert.IsTrue(ds2.Storage.GetType().ToString().ToLower().Contains("memory"));
        }

        // Checks that DataSourceHandlerCache can serve thousands of requests in considerable time without out of memory
        [TestMethod]
        [TestCategory("Local")]
        public void DataSourceHandlerCacheMemoryLoadTest()
        {
            string WorldClimFullName = typeof(Microsoft.Research.Science.FetchClimate2.WorldClim14DataSource).AssemblyQualifiedName;
            string CruFullName = typeof(Microsoft.Research.Science.FetchClimate2.DataSources.CruCl20DataHandler).AssemblyQualifiedName;
            Trace.WriteLine("Total memory before test: " + GC.GetTotalMemory(true));
            for (int i = 0; i < 10000; i++)
            {
                DataSourceHandlerCache.GetInstanceAsync(
                    WorldClimFullName,
                    "msds:az?name=WorldClimCurrent&DefaultEndpointsProtocol=http&AccountName=fc2chunkedstorage&AccountKey=dnPQl1Zjwpzm2qLPW/J9MFrhPWYocz3h/2zzuQ+RxCTE+ClFfKIriu4aCwJpPt+P6sU8hJfiWfQaBYc4nDSY/Q==").Wait();
                DataSourceHandlerCache.GetInstanceAsync(
                    CruFullName,
                    "msds:az?name=CRU_CL_2_0&DefaultEndpointsProtocol=http&AccountName=fc2chunkedstorage&AccountKey=dnPQl1Zjwpzm2qLPW/J9MFrhPWYocz3h/2zzuQ+RxCTE+ClFfKIriu4aCwJpPt+P6sU8hJfiWfQaBYc4nDSY/Q==").Wait();
                if(i % 100 == 0)
                    Trace.WriteLine("Iteration " + i.ToString() + ": " + GC.GetTotalMemory(true));
            }
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]        
        public async Task DataSourceHandlerCacheLoadHandlerOldAPI()
        {
            string StubFullName = typeof(HandlerStubSyncroNoContext).AssemblyQualifiedName;
            string StubFullName2 = typeof(HandlerStubSyncroWithContext).AssemblyQualifiedName;

            var dh = await DataSourceHandlerCache.GetInstanceAsync(StubFullName, "msds:memory");
            Assert.IsNotNull(dh);

            dh = await DataSourceHandlerCache.GetInstanceAsync(StubFullName2, "msds:memory");
            Assert.IsNotNull(dh);
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public async Task DataSourceHandlerCacheLoadHandlerAsyncAPI()
        {
            string StubFullName = typeof(HandlerStubAsyncWithContext).AssemblyQualifiedName;
            string StubFullName2 = typeof(HandlerStubAsyncNoContext).AssemblyQualifiedName;

            var dh = await DataSourceHandlerCache.GetInstanceAsync(StubFullName, "msds:memory");
            Assert.IsNotNull(dh);

            dh = await DataSourceHandlerCache.GetInstanceAsync(StubFullName2, "msds:memory");
            Assert.IsNotNull(dh);
        }
    }
}
