using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    /// <summary>
    /// An interface to be used by FetchEngine
    /// </summary>
    public interface IExtendedConfigurationProvider
    {
        /// <summary>Gets exact timestamp from configuration database for given user supplied timestamp</summary>
        /// <param name="utcTimestamp">User supplied timestamp</param>
        /// <returns>Exact timestamp present in configuration database or DateTime.MinValue if
        /// utcTimestamp is too small</returns>
        DateTime GetExactTimestamp(DateTime utcTimestamp);

        /// <summary>Returns the ExtendedConfiguration for the time specifed or null if time is before first timestamp in configuration database</summary>
        /// <param name="utcTime">UTC time to fetch the configuration for or DateTime.MaxValue for latest timestamp </param>
        /// <returns>ExtendedConfiguration object or null</returns>
        ExtendedConfiguration GetConfiguration(DateTime utcTime);
    }

    /// <summary>
    /// A factory that can produce FetchConfiguration object for the timestamp using LINQ-to-SQL
    /// </summary>
    public class FetchConfigurationProvider
    {
        private string connectionString;

        /// <summary>
        /// Creates the provider
        /// </summary>
        /// <param name="dataBaseConnectionString">The ADO.net connection string to the SQL</param>
        public FetchConfigurationProvider(string dataBaseConnectionString)
        {
            connectionString = dataBaseConnectionString;
        }
       
        /// <summary>
        /// Produces the configuration for the time specified
        /// </summary>
        /// <param name="utcTime">A time to produce the configuration for</param>
        /// <returns></returns>
        public IFetchConfiguration GetConfiguration(DateTime utcTime)
        {
            using (FetchConfigurationDataClassesDataContext db = new FetchConfigurationDataClassesDataContext(connectionString))
            {
                if (utcTime == DateTime.MaxValue)
                    utcTime = (DateTime)db.GetLatestTimeStamp().First().TimeStamp;
                else if (utcTime < db.GetFirstTimeStamp().First().TimeStamp)
                    throw new ArgumentException("No configuration exists for given timestamp");

                List<IDataSourceDefinition> dataSourcesList = new List<IDataSourceDefinition>();

                Dictionary<string, IVariableDefinition> supportedVars = new Dictionary<string, IVariableDefinition>();
                var dataSources = db.GetDataSources(utcTime);
                var dsVariables = db.GetEnvVariables().ToArray();
                foreach (var ds in dataSources)
                {
                    var mappings = db.GetMapping(utcTime, ds.Name);
                    var providedVars = mappings.Where(mp => mp.IsOutbound != null && (bool)mp.IsOutbound).Select(mp => mp.FetchVariableName).ToArray();
                    
                    if (providedVars.Length > 0) //otherwise there are no mappings for the data source
                    {
                        IDataSourceDefinition dsd = new DataSourceDefinition((ushort)ds.ID, ds.Name, ds.Description, ds.Copyright,
                            (ds.RemoteName != null) ? ds.Uri : string.Empty,
                            providedVars);
                        dataSourcesList.Add(dsd);
                        foreach (var evn in providedVars)
                            if (!supportedVars.ContainsKey(evn))
                            {
                                var v = dsVariables.Where(dsv => dsv.DisplayName == evn).First();
                                supportedVars[v.DisplayName] = new VariableDefinition(v.DisplayName, v.Units, v.Description);
                            }
                    }
                }

                return new FetchConfiguration(utcTime, dataSourcesList.ToArray(), supportedVars.Values.ToArray());
            }

        }
    }

    /// <summary>
    /// A configuration that contains variable name mapping (betwwing fetch varaibles and data varaibles,data parameters)
    /// </summary>
    public class ExtendedConfiguration
    {
        /// <summary>
        /// Constructs the extended configuration
        /// </summary>
        /// <param name="timeStamp">A time stamp that the configurtaion coresponds to</param>
        /// <param name="dataSources">Enabled data sources</param>
        /// <param name="variables">Enabled fetch varaibles</param>
        public ExtendedConfiguration(DateTime timeStamp, ExtendedDataSourceDefinition[] dataSources, IVariableDefinition[] variables)
        {
            this.TimeStamp = timeStamp;
            this.DataSources = dataSources;
            this.EnvironmentalVariables = variables;
        }

        /// <summary>
        /// A UTC timestamp for which the configuration corresponds
        /// </summary>
        public DateTime TimeStamp { get; internal set; }
        
        /// <summary>
        /// Enabled data sources
        /// </summary>
        public ExtendedDataSourceDefinition[] DataSources { get; internal set; }

        /// <summary>
        /// Enabled fetch varaibles. Each can be produced at least by one data source
        /// </summary>
        public IVariableDefinition[] EnvironmentalVariables { get; internal set; }

        /// <summary>
        /// Full type name of FetchEngine implementation
        /// </summary>
        public string FetchEngineTypeName { get; internal set; }
    }

    /// <summary>
    /// Describes the datasource including mapping
    /// </summary>
    public class ExtendedDataSourceDefinition : DataSourceDefinition
    {
        readonly Dictionary<string, string> envToDsMapping = new Dictionary<string, string>();
        readonly Dictionary<string, string> dsToEnvMapping = new Dictionary<string, string>();



        public Dictionary<string, string> EnvToDsMapping
        {
            get
            {
                return envToDsMapping;
            }
        }

        public Dictionary<string, string> DsToEnvMapping
        {
            get
            {
                return dsToEnvMapping;
            }
        }

        public ExtendedDataSourceDefinition(ushort id, string name, string description, string copyright, string uri, string handlerTypeName,string[] providedEvnVariables,string serviceURI, Dictionary<string, string> envToDsVarNameMapping,string remoteName,ushort remoteId)
            : base(id, name, description, copyright,serviceURI, providedEvnVariables)
        {
            Uri = uri;            
            HandlerTypeName = handlerTypeName;
            this.RemoteDataSourceID = remoteId;
            this.RemoteDataSourceName = remoteName;

            foreach (var item in envToDsVarNameMapping)
            {
                envToDsMapping[item.Key] = item.Value;
                dsToEnvMapping[item.Value] = item.Key;
            }            
        }
        
        /// <summary>
        /// The URI of Dmitrov Data set to access the data (for non-federated data source) or URI of remote service (for federated data source)
        /// </summary>
        public string Uri { get; internal set; }

        /// <summary>
        /// Indicates whether the data source is federated to the another instance of FetchClimate or not
        /// </summary>
        public bool IsFederated
        {
            get
            {
                Debug.Assert((string.IsNullOrEmpty(RemoteDataSourceName) ^ string.IsNullOrEmpty(HandlerTypeName)));
                return !string.IsNullOrEmpty(RemoteDataSourceName);
            }
        }

        /// <summary>
        /// The name of the class (full class name, including assembly name) that must be invoked to process the requests with the data source (no null only for non-federated data sources)
        /// </summary>
        public string HandlerTypeName { get; internal set; }
        
        /// <summary>
        /// The name of the datasource at the remote fetch climate instance (not null for the federated data sources)
        /// </summary>
        public string RemoteDataSourceName { get; internal set; }
        
        /// <summary>
        /// The ID of the datasource at the remote fetch climate instance (undefined for non-federated data sources)
        /// </summary>
        public ushort RemoteDataSourceID { get; internal set; }
    }

    /// <summary>
    /// The factory of ExtendedConfiguration using LINQ-to-SQL
    /// </summary>
    public class SqlExtendedConfigurationProvider : IExtendedConfigurationProvider
    {
        private string connectionString;

        public SqlExtendedConfigurationProvider(string dataBaseConnectionString)
        {
            connectionString = dataBaseConnectionString;
        }

        /// <summary>Gets exact timestamp from configuration database for given user supplied timestamp</summary>
        /// <param name="utcTimestamp">User supplied timestamp</param>
        /// <returns>Exact timestamp present in configuration database or DateTime.MinValue if
        /// utcTimestamp is too small or service configuration is empty</returns>
        public DateTime GetExactTimestamp(DateTime utcTimestamp)
        {
            using (FetchConfigurationDataClassesDataContext db = new FetchConfigurationDataClassesDataContext(connectionString))
            {
                if (utcTimestamp == DateTime.MaxValue) 
                {
                    var result = db.GetLatestTimeStamp().FirstOrDefault();
                    if (result != null)
                        return (DateTime)result.TimeStamp;
                    else
                        return DateTime.MinValue;
                }
                else
                {
                    var result = db.GetExactTimeStamp(utcTimestamp).FirstOrDefault();
                    if (result == null)
                        return DateTime.MinValue;
                    else
                        return (DateTime)result.TimeStamp;
                }
            }
        }

        /// <summary>Returns the ExtendedConfiguration for the time specifed or null if time is before first timestamp in configuration database</summary>
        /// <param name="utcTime">UTC time to fetch the configuration for or DateTime.MaxValue for latest timestamp </param>
        /// <returns>ExtendedConfiguration object or null</returns>
        public ExtendedConfiguration GetConfiguration(DateTime utcTime)
        {
            using (FetchConfigurationDataClassesDataContext db = new FetchConfigurationDataClassesDataContext(connectionString))
            {
                if (utcTime == DateTime.MaxValue)
                    utcTime = (DateTime)db.GetLatestTimeStamp().First().TimeStamp;
                else if (utcTime < db.GetFirstTimeStamp().First().TimeStamp)
                    return null; 

                List<ExtendedDataSourceDefinition> dataSourcesList = new List<ExtendedDataSourceDefinition>();

                Dictionary<string, VariableDefinition> supportedVars = new Dictionary<string, VariableDefinition>();

                var dataSources = db.GetDataSources(utcTime);
                var dsVariables = db.GetEnvVariables().ToArray();

                foreach (var ds in dataSources)
                {
                    var mapping = db.GetMapping(utcTime, ds.Name).ToArray();
                    var providedVars = mapping.Where(mp => mp.IsOutbound != null && (bool)mp.IsOutbound).Select(mp => mp.FetchVariableName).ToArray();
                    
                    ExtendedDataSourceDefinition dsd = new ExtendedDataSourceDefinition((ushort)ds.ID, ds.Name, ds.Description, ds.Copyright, ds.Uri, ds.FullClrTypeName, providedVars,
                        (ds.RemoteName != null) ? ds.Uri : string.Empty,
                        mapping.ToDictionary(map => map.FetchVariableName,map => map.DataVariableName),
                        ds.RemoteName,
                        (ushort)(ds.RemoteID == null ? -1 : ds.RemoteID));
                    
                    dataSourcesList.Add(dsd);                    
                    foreach (var envVar in providedVars)
                        if (!supportedVars.ContainsKey(envVar))
                        {
                            var v = dsVariables.Where(dsv => dsv.DisplayName == envVar).First();
                            supportedVars[envVar] = new VariableDefinition(v.DisplayName, v.Units, v.Description);
                        }
                }

                return new ExtendedConfiguration(utcTime, dataSourcesList.ToArray(), supportedVars.Values.ToArray())
                {
                    FetchEngineTypeName = db.GetFetchEngine(utcTime).First().FullClrTypeName
                };
            }
        }
    }
}
