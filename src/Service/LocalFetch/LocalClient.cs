using Microsoft.Research.Science.Data;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    public class LocalFetch : IFetchClient
    {
        static SqlExtendedConfigurationProvider configProvider;        

        public Task<DataSet> FetchAsync(FetchRequest request, Action<FetchStatus> progressReport=null)        
        {            
            if (configProvider == null)
                configProvider = new SqlExtendedConfigurationProvider(SharedConstants.LocalConfigurationConnectionString);

            var cfg = configProvider.GetConfiguration(request.ReproducibilityTimestamp);
            var feType = Type.GetType(cfg.FetchEngineTypeName);
            if (feType == null)
                throw new InvalidOperationException("Cannot load fetch engine type " + feType);
            var fe = (IFetchEngine)feType.GetConstructor(new Type[1] { typeof(IExtendedConfigurationProvider) }).Invoke(new object[1] { new SqlExtendedConfigurationProvider(SharedConstants.LocalConfigurationConnectionString) });
            return fe.PerformRequestAsync(request).
                ContinueWith(t => RequestDataSetFormat.CreateCompletedRequestDataSet("msds:memory", request, t.Result.Values, t.Result.Provenance, t.Result.Uncertainty));
        }

        public FetchConfiguration GetConfiguration(DateTime utcTime)
        {
            FetchConfigurationProvider provider = new FetchConfigurationProvider(SharedConstants.LocalConfigurationConnectionString);
            return provider.GetConfiguration(utcTime);
        }
    }
}