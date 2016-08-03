using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    /// <summary>
    /// An implementation of IFetchConfigProvider with a single DataSource and a single Variable. 
    /// </summary>
    /// <remarks>this class is useful to instantiate simple working configurations of Fetch Engine</remarks>
    public class SimpleFetchConfigurationProvider<Engine, Handler> : IExtendedConfigurationProvider
        where Engine: IFetchEngine
        where Handler: DataSourceHandler
    {
        private ExtendedConfiguration _value;
        /// <summary>
        /// Instantiates a very simple FetchClimate configuration.
        /// </summary>
        /// 
        /// <param name="uri">DataSet uri.</param>
        /// <param name="envMapping">The DataSet variable name that will be matched to an 'env' environmental variable.</param>
        public SimpleFetchConfigurationProvider(string uri, string envMapping)
        {
            _value = new ExtendedConfiguration(
                DateTime.UtcNow,
                new ExtendedDataSourceDefinition[]{
                    new ExtendedDataSourceDefinition(
                        1, // DataSource unique key
                        typeof(Handler).Name, // Display name
                        "Directly referenced data handler", // Description
                        "", // Copyright
                        uri, // A reference to actual data, e.g. DataSet URI
                        typeof(Handler).AssemblyQualifiedName, // Data handler type name
                        new string[] {"env"}, // provided environmental variables
                        null, // federated FetchClimate service URI
                        new Dictionary<string, string>() {{"env", envMapping}}, // EnvToDsVarMapping: FetchClimate "env" mapped to dataset envMapping
                        null, // remote name in the federated FetchClimate service
                        0 // remote ID
                        )
                },
                new VariableDefinition[]{
                    new VariableDefinition("env", "", "Universally provided variable")
                }) { 
                FetchEngineTypeName = typeof(Engine).AssemblyQualifiedName
            };
        }
        public DateTime GetExactTimestamp(DateTime utcTimestamp)
        {
            return _value.TimeStamp;
        }

        public ExtendedConfiguration GetConfiguration(DateTime utcTime)
        {
            return _value;
        }
    }
}
