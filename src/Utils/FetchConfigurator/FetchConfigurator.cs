using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.Globalization;
using Microsoft.Research.Science.Data;


namespace Microsoft.Research.Science.FetchClimate2
{
    public enum DataSourceMappingType
    {
        DataVariable,
        DataParameter,
        Remove
    };

    public class DataSourceMapping
    {
        public readonly DataSourceMappingType Type;
        public readonly string DataVariable;
        public readonly string EnvVariable;

        public DataSourceMapping(DataSourceMappingType type, string dataVariable, string envVariable)
        {
            Type = type;
            DataVariable = dataVariable;
            EnvVariable = envVariable;
        }

    };

    public class FetchConfigurator
    {
        private const string ConfigurationContainerName = "fc2-config";
        private const string SqlConnectionStringBlobName = "configuration-db-connection-string";

        /// <summary>
        /// identifies whether the configurator works with cloud deployment of FetchClimate or with "in-process" deployment
        /// </summary>
        private bool isConnectedToCloud;
        private string storageConnectionString = null;
        private AssemblyStore astore;
        private string sqlConnStringIncludingPassword = string.Empty;
        private FetchConfigurationDataClassesDataContext db;
        public FetchConfigurationDataClassesDataContext Db
        {
            get
            {
                return db;
            }
        }

        /// <summary>
        /// Can the connection to the SQL server be established
        /// </summary>
        public bool IsSqlServerAvailable
        {
            get
            {
                return SqlHelper.IsSqlServerAvailable(db.Connection.ConnectionString);
            }
        }

        /// <summary>
        /// True is the sql server is available and the database with the name specified in the connection string exists
        /// </summary>
        public bool DoesDataBaseExist
        {
            get
            {
                return SqlHelper.DoesDataBaseExist(db.Connection.ConnectionString);
            }
        }

        /// <summary>
        /// true if the sql server is available, database with specified name exists and its schema is not empty
        /// </summary>
        public bool IsDataBaseDeployed
        {
            get
            {
                return SqlHelper.IsDataBaseDeployed(db.Connection.ConnectionString);
            }
        }

        /// <summary>
        /// creates an empty database with the name specified in the connection string
        /// </summary>
        public void CreateDatabase()
        {
            SqlHelper.CreateDatabase(db.Connection.ConnectionString);
        }

        /// <summary>
        /// Connection string to work with Azure storage
        /// </summary>
        public string StorageConnectionString
        {
            get
            {
                return storageConnectionString;
            }
        }

        #region Contructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="localSqlConnString">SQL connection string to configuration data base (null for automatic extraction from blob)</param>
        /// <param name="storageConnStr">An azure storage to work with (null for in-process operation)</param>
        /// <param name="isWorkingWithCloud">identifies whether the configurator works with cloud deployment of FetchClimate or with "in-process" deployment</param>
        /// <remarks>This constructor is invoked by "use command".
        /// use cloud accountkey=... accountname=... sqlconnstr=... invokes FetchConfigurator(storageconnstr, sqlconnstr, true),
        /// use local sqlconnstr=... invokes FetchConfigurator(null, sqlconnstr, false)</remarks>
        public FetchConfigurator(string storageConnStr, string sqlConnectionStr, bool isWorkingWithCloud)
        {
            this.storageConnectionString = storageConnStr;
            this.isConnectedToCloud = isWorkingWithCloud;

            if (!string.IsNullOrEmpty(storageConnectionString) && sqlConnectionStr == null) //extracting sql connection string from the azure storage            
                sqlConnectionStr = ExtractSqlConnectionString(sqlConnectionStr);
            sqlConnStringIncludingPassword = sqlConnectionStr;
            db = new FetchConfigurationDataClassesDataContext(sqlConnectionStr);

            //caching sql conn string to blob storage
            if (!string.IsNullOrEmpty(storageConnStr))
            {
                var csa = CloudStorageAccount.Parse(storageConnectionString);
                var client = csa.CreateCloudBlobClient();
                var contatiner = client.GetContainerReference(ConfigurationContainerName);
                contatiner.CreateIfNotExist();
                var blob = contatiner.GetBlobReference(SqlConnectionStringBlobName);
                blob.UploadText(sqlConnStringIncludingPassword);
                astore = new AssemblyStore(storageConnectionString, true);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scriptText">A script without SQLCMD operators (but with GO operators) to execute for the database specified in the connection string.</param>        
        public void ExecuteSqlScript(string scriptText)
        {
            SqlHelper.ExecuteSqlScript(db.Connection.ConnectionString, scriptText);
        }

        /// <summary>
        /// Extracts SQL connection string of FetchClimate configuration DB from the supplied Azure storage account
        /// </summary>
        /// <param name="storageConnStr">An azure storage to check</param>        
        /// <exception cref="">StorageException if blob with configuration can't be found</exception>
        /// <returns></returns>
        public static string ExtractSqlConnectionString(string storageConnectionString)
        {
            string sqlConnectionStr;
            var csa = CloudStorageAccount.Parse(storageConnectionString);
            var client = csa.CreateCloudBlobClient();
            var contatiner = client.GetContainerReference(ConfigurationContainerName);
            contatiner.CreateIfNotExist();

            var blob = contatiner.GetBlobReference(SqlConnectionStringBlobName);

            try
            {
                sqlConnectionStr = blob.DownloadText();
            }
            catch (StorageException)
            {
                throw new InvalidOperationException("BLOB with configuration DB connection string can't be found in the Azure Blob Storage");
            }
            return sqlConnectionStr;
        }
        #endregion

        #region assembly

        [CmdAttr("Usage: assembly upload file=\"<dll file name>\"\nUploads provided assembly to the cloud GAC.", "assembly:asm", "upload:up")]
        public void AssemblyUpload(string file)
        {
            Assembly toLoad;
            try
            {
                toLoad = System.Reflection.Assembly.LoadFrom(file);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to load assenbly from " + file + "\n Exception message: " + ex.Message);
            }
            if (isConnectedToCloud)
                astore.Install(toLoad);
        }

        [CmdAttr(" * Upload (up) - adds a new datasource.\n",
            "assembly:asm")]
        public void Assembly()
        {

        }

        #endregion

        #region DataSource

        [CmdAttr("Usage: datasource add Name=\"<Name>\" [(Handler=\"<dll file name or full CLR type name>\" | RemoteName=\"<name of the data source on the remote service>\")] [Copyright=\"<copyright string>\"] [Description=\"<description>\"] [Uri=\"<uri of the dataset if handler is specified or uri of the remote service if the remote name is specified>\"] [IsHidden=\"true|false\"] [[var1>var2] | [var1<var2] | [!var]]\nAdds a new datasource with given attributes. Adds provided mappings to it. It is possible that data source be added but some of the mappings would fail to. It may be safely assumed that everything, about which an error message was not shown, succeeded.",
            "datasource:ds", "add:a")]
        public void DataSourceAdd(string name, string description, string handler = null, string remotename = null, string copyright = "",
            string uri = "", List<DataSourceMapping> addMapping = null, List<String> removeMapping = null)
        {

            ushort? remoteID = null;
            string resolvedHandlerAssemblyQualifiedName = null;

            //if (string.IsNullOrEmpty(remotename) && !String.IsNullOrEmpty(handler))
            //    ExtractHandlerAssemblyAndTypeName(handler, out toLoad, out handlerType);

            if (!string.IsNullOrEmpty(remotename) && String.IsNullOrEmpty(handler)) //federated
            {
                IFetchConfiguration remoteConfig;
                try
                {
                    RemoteFetchClient client = new RemoteFetchClient(new Uri(uri));
                    remoteConfig = client.GetConfiguration(DateTime.MaxValue);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException("Failed to retrieve configuration of the remote service.\n Exception message: " + ex.Message);
                }
                if (!remoteConfig.DataSources.Any(ds => ds.Name == remotename))
                    throw new ArgumentException("Data source with given name does not exist on the remote service.");
                remoteID = remoteConfig.DataSources.Where(ds => ds.Name == remotename).FirstOrDefault().ID;
            }
            else if (string.IsNullOrEmpty(remotename) && !String.IsNullOrEmpty(handler))//local
            {
                AssemblyStore gac = null;
                if (isConnectedToCloud)
                    gac = astore;
                resolvedHandlerAssemblyQualifiedName = ExtractHandlerAssemblyAndTypeName(handler, gac);
            }
            else if (!string.IsNullOrEmpty(remotename) && !String.IsNullOrEmpty(handler))
                throw new ArgumentException("Handler and remote name can not be specified simultaneously.");

            var localDs = db.GetDataSources(DateTime.MaxValue).ToArray();
            if (localDs.All(x => x.Name != name))
                db.AddDataSource(name, description, copyright, resolvedHandlerAssemblyQualifiedName, ParserHelper.AppendDataSetUriWithDimensions(uri), remoteID, remotename);
            else
                throw new ArgumentException("Data source with given name already exists.");
            SetMappings(name, addMapping, null);

        }

        /// <summary>
        /// extracts the assembly and type for the data hanfler from supplied string which can be either a type name or a dll name
        /// </summary>
        /// <param name="handler">a type name or a dll name</param>
        /// <param name="toLoad"></param>
        /// <param name="handlerType"></param>
        private static string ExtractHandlerAssemblyAndTypeName(string handler, AssemblyStore gac)
        {
            if (handler.EndsWith("dll", true, CultureInfo.InvariantCulture))
            {
                try
                {
                    var toLoad = System.Reflection.Assembly.LoadFrom(handler);
                    var types = toLoad.GetExportedTypes().Where(t => t.IsSubclassOf(typeof(Microsoft.Research.Science.FetchClimate2.DataSourceHandler))).ToArray();
                    if (types.Length == 0)
                        throw new Exception("Specifed dll doesn't contain classes inheried from Microsoft.Research.Science.FetchClimate2.DataSourceHandler");
                    else if (types.Length > 1)
                        throw new Exception("Specifed dll contains more than one class specification inheried from Microsoft.Research.Science.FetchClimate2.DataSourceHandler. You can specify FullTypeName of the handler instead");
                    else return types[0].AssemblyQualifiedName;
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to load data handler from " + handler + "\n Exception message: " + ex.Message);
                }
            }
            else
            {
                if (gac != null)
                {
                    var result = gac.TryLoadType(handler);
                    if (result.Item1) return result.Item2;
                    else throw new Exception(result.Item2);
                }
                else
                    return Type.GetType(handler).AssemblyQualifiedName;

            }
        }

        [CmdAttr("Usage: datasource set Name=\"<Name>\" [(Handler=\"<dll file name or full CLR type name>\" | RemoteName=\"<name of the datasource on the remote service>\")] [Copyright=\"<copyright string>\"] [Description=\"<description>\"] [Uri=\"<uri of the dataset if handler is specified or uri of the remote service if the remote name is specified>\"] [IsHidden=\"true|false\"] [[var1>var2] | [var1<var2] | [!var]]\nUpdates attributes of the datasource with given name, adds, updates, or removes mappings to it",
            "datasource:ds", "set:s")]
        public void DataSourceSet(string name, string handler = null, string remotename = null, string uri = null, string copyright = null, string description = null,
            List<DataSourceMapping> addMapping = null, List<String> removeMapping = null)
        {
            var localDs = db.GetDataSources(DateTime.MaxValue).ToArray();
            if (localDs.All(x => x.Name != name))
                throw new ArgumentException("Specified data source does not exist.");

            ushort? remoteID = null;
            if (!String.IsNullOrEmpty(handler) && String.IsNullOrEmpty(remotename))
            {
                AssemblyStore gac = null;
                if (isConnectedToCloud)
                    gac = new AssemblyStore(storageConnectionString);
                var resolvedHandlerAssemblyQualifiedName = ExtractHandlerAssemblyAndTypeName(handler, gac);
                db.SetDataSourceProcessor(name, resolvedHandlerAssemblyQualifiedName, null, null);
            }
            else if (String.IsNullOrEmpty(handler) && !String.IsNullOrEmpty(remotename))
            {
                IFetchConfiguration remoteConfig;
                try
                {
                    RemoteFetchClient client = new RemoteFetchClient(new Uri(uri));
                    remoteConfig = client.GetConfiguration(DateTime.MaxValue);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException("Failed to retrieve configuration of the remote service.\n Exception message: " + ex.Message);
                }
                if (!remoteConfig.DataSources.Any(ds => ds.Name == remotename))
                    throw new ArgumentException("Data source with given name does not exist on the remote service.");
                remoteID = remoteConfig.DataSources.Where(ds => ds.Name == remotename).FirstOrDefault().ID;
                db.SetDataSourceProcessor(name, null, remoteID, remotename);
            }
            if (!string.IsNullOrEmpty(remotename) && !String.IsNullOrEmpty(handler))
                throw new ArgumentException("Handler and remote name can not be specified simultaneously.");

            if (!String.IsNullOrEmpty(uri))
                db.SetDataSourceUri(name, ParserHelper.AppendDataSetUriWithDimensions(uri));

            //if (isHidden != null)
            //    db.SetDataSourceHidden(name, isHidden);

            if (copyright != null)
                db.SetDataSourceCopyright(name, copyright);

            if (description != null)
                db.SetDataSourceDescription(name, description);

            SetMappings(name, addMapping, removeMapping);

        }

        private void SetMappings(string name, List<DataSourceMapping> addMapping, List<String> removeMapping)
        {
            var vars = db.GetEnvVariables().Select(x => x.DisplayName).ToList();
            List<string> errs = new List<string>();

            if (addMapping != null && addMapping.Count > 0)
            {
                foreach (var m in addMapping)
                {
                    if (vars.Contains(m.EnvVariable))
                    {
                        try
                        {
                            if (m.Type == DataSourceMappingType.DataVariable)
                                db.SetMapping(name, m.EnvVariable, m.DataVariable, true, true);
                            else if (m.Type == DataSourceMappingType.DataParameter)
                                db.SetMapping(name, m.EnvVariable, m.DataVariable, false, true);
                        }
                        catch (Exception ex)
                        {
                            errs.Add("Failed to map " + m.EnvVariable + " to " + m.DataVariable + ". Reason:" + ex.Message);
                        }
                    }
                    else
                        errs.Add("Variable \"" + m.EnvVariable + "\" is not defined.");
                }
            }
            if (removeMapping != null && removeMapping.Count > 0)
            {
                for (int i = 0; i < removeMapping.Count; i++)
                {
                    if (vars.Contains(removeMapping[i]))
                    {
                        try
                        {
                            db.SetMapping(name, removeMapping[i], "", false, false);
                        }
                        catch (Exception ex)
                        {
                            errs.Add("Failed to unmap " + removeMapping[i] + ". Reason:" + ex.Message);
                        }
                    }
                    else
                        errs.Add("Variable \"" + removeMapping[i] + "\" is not defined.");
                }
            }
            if (errs.Count > 0)
                throw new ArgumentException(errs.Aggregate((x, y) => x + "\n" + y));
        }

        [CmdAttr("Usage: datasource list [timestamp=\"dd-MM-yyyy HH:mm:ss\"] [verbose=<yes|no>]\nShows the list of datasources relevant for a given timestamp or for current time if no timestamp provided.",
            "datasource:ds", "list:ls")]
        public List<string> DataSourceList(string verbose = "no", DateTime? timestamp = null, string name = null)
        {
            if (timestamp == null)
                timestamp = DateTime.UtcNow;

            bool needVerbose = false;
            if (verbose.ToLower() == "yes")
                needVerbose = true;
            else if (verbose.ToLower() != "no")
                throw new Exception("Verbose must be either \"yes\" or \"no\".");

            List<GetDataSourcesResult> DataSources;
            if (!string.IsNullOrEmpty(name))
            {
                DataSources = db.GetDataSources(timestamp).Where(d => d.Name == name).ToList();
            }
            else
            {
                DataSources = db.GetDataSources(timestamp).ToList();
            }
            if (needVerbose)
                return DataSources.Select(ds => ds.ToLongString()).ToList();
            return DataSources.Select(ds => ds.ToString()).ToList();
        }

        [CmdAttr("Usage: datasource info name=\"data source name\" [timestamp=\"time stamp\"]", "datasource:ds", "info:i")]
        public IEnumerable<string> DataSourceInfo(string name, DateTime? timestamp = null)
        {
            if (timestamp == null)
                timestamp = DateTime.UtcNow;

            var ds = db.GetDataSources(timestamp).Where(d => d.Name == name).FirstOrDefault();
            if (ds == null)
                throw new Exception("Data source with specified name is not found");

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("[{0}] {1}\n", ds.ID, ds.Name);
            sb.AppendFormat("Description: {0}\n", ds.Description);
            sb.AppendFormat("Copyright: {0}\n", ds.Copyright);
            if (String.IsNullOrEmpty(ds.FullClrTypeName))
            {
                sb.AppendFormat("Remote service uri: {0}\n", ds.Uri);
                sb.AppendFormat("Remove data source id: {0}\n", ds.Uri);
            }
            else
            {
                sb.AppendFormat("Handler: {0}\n", ds.FullClrTypeName);
                sb.AppendFormat("Dataset uri: {0}\n", ds.Uri);
            }
            var mappings = db.GetMapping(timestamp, name);
            sb.AppendFormat("Mappings: " + String.Join(", ", mappings.Select(
                m => String.Concat(
                    m.DataVariableName,
                    (bool)m.IsOutbound ? ">" : "<",
                    m.FetchVariableName))) + "\n");

            return new string[] { sb.ToString() };
        }

        [CmdAttr("Usage: datasource update Name=\"<Name>\"\nUpdates dimension lengths used by the datasource with given name to those of the associated dataset if they differ for local data sources, generates new timestamp for federated data sources, does nothing for local virtual data sources.",
            "datasource:ds", "update:u")]
        public void DataSourceUpdate(string name)
        {
            var localDs = db.GetDataSources(DateTime.MaxValue).ToArray();
            if (localDs.All(x => x.Name != name))
                throw new ArgumentException("Specified data source does not exist.");

            var datasource = localDs.Single(x => x.Name == name);

            if (datasource.RemoteID != null)
            {
                //federated data source
                db.SetDataSourceProcessor(name, null, datasource.RemoteID, datasource.RemoteName);
            }
            else if (!String.IsNullOrEmpty(datasource.Uri))
            {
                //local datasource
                var dsuri = new DataSetUri(datasource.Uri);

                Dictionary<string, int> oldDims = new Dictionary<string, int>();
                if (dsuri.ContainsParameter("dimensions"))
                {
                    string dimstr = dsuri.GetParameterValue("dimensions");
                    var dimpairs = dimstr.Split(',');
                    foreach (var p in dimpairs)
                    {
                        var pair = p.Split(':');
                        oldDims.Add(pair[0], int.Parse(pair[1]));
                    }
                }

                DataSet ds;
                try
                {
                    ds = DataSet.Open(dsuri);
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to open associated dataset: " + ex.Message);
                }
                var newDims = ds.Dimensions;
                if (oldDims.All(d => newDims.Contains(d.Key) && newDims[d.Key].Length == d.Value) && newDims.All(d => oldDims.ContainsKey(d.Name) && oldDims[d.Name] == d.Length))
                    throw new Exception("Associated data set has not grown.");
                string newDimString = String.Join(",", newDims.Select(d => d.Name + ":" + d.Length.ToString()));
                ds.Dispose();
                dsuri.SetParameterValue("dimensions", newDimString);
                string newUri = dsuri.ToString();
                db.SetDataSourceUri(name, newUri);
            }
        }

        [CmdAttr(" * Add (a) - adds a new datasource.\n" +
                 " * Set (s) - changes attributes of a datasource.\n" +
                 " * Update (u) - updates data source.\n" +
                 " * List (ls) - show all relevant datasources.\n" +
                 " * Info (i) - full datasource information.",
            "datasource:ds")]
        public void DataSource()
        {

        }
        #endregion

        #region Variable
        [CmdAttr("Usage: variable add Name=\"<name>\" Units=\"<units>\" Description=\"<description>\"\nAdds a new variable with given name, units and description.",
            "variable:var", "add:a")]
        public void VariableAdd(string name, string description, string units)
        {
            var vars = db.GetEnvVariables().Select(x => x.DisplayName).ToList();
            if (vars.Contains(name))
                throw new ArgumentException("Variable with given name is already defined");
            db.AddVariable(name, description, units);
        }

        [CmdAttr("Usage: variable list [name=\"<VariableName>\"]\nProvides detailed info about the variable with given name or shows the list of all defined variables if no such name is specified."/* [timestamp=\"dd-MM-yyyy HH:mm:ss\"]"*/,
            "variable:var", "list:ls")]
        public IEnumerable<VariableDetails> VariableList(/*DateTime? timestamp = null,*/ string name = null)
        {
            //var ts = timestamp.HasValue ? timestamp.Value : DateTime.MaxValue;
            IEnumerable<GetEnvVariablesResult> Variables = db.GetEnvVariables(/*ts*/);
            if (!string.IsNullOrEmpty(name))
            {
                Variables = Variables.Where(d => d.DisplayName == name).ToList();
                if (Variables.Count() <= 0)
                    throw new ArgumentException("Variable with given name does not exists.");
            }
            return Variables.Select(v => new VariableDetails(v, db.GetDataSourcesForVariable(DateTime.UtcNow, v.DisplayName)));
        }

        [CmdAttr("Usage: variable set Name=\"<name>\" [Description=\"<new description>\"] [Units=\"<new units>\"]\nSets new name or new description (or both) for a variable with given name.",
            "variable:var", "set:s")]
        public void VariableSet(string name, string description = null, string units = null)
        {
            var vars = db.GetEnvVariables().Select(x => x.DisplayName).ToList();
            if (vars.Contains(name))
            {
                var errs = new List<string>();
                try
                {
                    if (units != null)
                        db.SetEnvVariableUnits(name, units);
                }
                catch (Exception ex)
                {
                    errs.Add("Failed to alter units of a variable. Reason:\n" + ex.Message);
                }

                try
                {
                    if (description != null)
                        db.SetEnvVariableDescription(name, description);
                }
                catch (Exception ex)
                {
                    errs.Add("Failed to alter description of a variable. Reason:\n" + ex.Message);
                }

                if (errs.Count > 0)
                    throw new Exception(errs.Aggregate((x, y) => x + "\n" + y));
            }
            else
                throw new ArgumentException("Variable with given name does not exists.");
        }

        [CmdAttr(" * Add (a) - adds a new environmental variable.\n" +
                 " * Set (s) - changes attributes of an environmental variable.\n" +
                 " * List (ls) - lists all defined variables.",
            "variable:var")]
        public void Variable()
        {

        }
        #endregion

        private IEnumerable<Tuple<Assembly, byte[]>> GetBaseAssembliesForAzureGac(string path)
        {
            return from f in Directory.GetFiles(path, "*.dll")
                   let fp = Path.Combine(path, f)
                   select Tuple.Create(System.Reflection.Assembly.LoadFrom(fp), File.ReadAllBytes(fp));
        }

        #region Reset
        [CmdAttr(@"Usage: reset [azureGAC=<path to assemplies forlder, ./azureGAC by default>]
Erases current configuration (if one exists) and sets up fresh empty configuration.
Populates Azure GAC with assemblies from AzureGAC subdirectory.
Clears request cache.",
            "reset")]
        public void Reset(string azureGAC = null)
        {
            // check that engine is among the assemblies
            if (azureGAC == null) azureGAC = Path.Combine(Directory.GetCurrentDirectory(), "azureGAC");
            if (!Directory.Exists(azureGAC)) Console.WriteLine("Error: no such directory: {0}.", azureGAC);
            else
            {
                try
                {
                    Tuple<Assembly, byte[]>[] toLoadtoGAC = GetBaseAssembliesForAzureGac(azureGAC).ToArray();
                    var ee = from a in toLoadtoGAC
                             from t in a.Item1.GetTypes()
                             where t.GetInterfaces().Any(i => i.Name == "IFetchEngine")
                             select t.AssemblyQualifiedName;
                    var engines = ee.ToArray();
                    if (engines.Length < 1) Console.WriteLine("Error: {0} assemblies to load in AzureGAC and none of them contains IFetchEngine.", toLoadtoGAC.Length);
                    else if (engines.Length > 1) Console.WriteLine("Error: multiple engines found:\n{0}", string.Join("\n", engines));
                    else
                    {
                        Console.WriteLine("Engine found: {0}", engines[0]);
                        db.TruncateTables();
                        if (isConnectedToCloud)
                        {
                            astore.Reset();

                            foreach (Tuple<Assembly, byte[]> toLoad in toLoadtoGAC)
                                astore.Install(toLoad.Item1);

                            //clearing requests container
                            var csa = CloudStorageAccount.Parse(storageConnectionString);
                            var client = csa.CreateCloudBlobClient();
                            var requestsContatiner = client.GetContainerReference("requests");
                            requestsContatiner.CreateIfNotExist();
                            Parallel.ForEach(requestsContatiner.ListBlobs(), x =>
                                {
                                    try
                                    {
                                        ((CloudBlob)x).Delete();
                                    }
                                    catch (StorageClientException sce)
                                    {
                                        if (sce.ErrorCode != StorageErrorCode.BlobNotFound)
                                            return;
                                    }
                                });

                        }
                        db.SetFetchEngine(string.Format(engines[0]));
                    }
                }
                catch (System.Reflection.ReflectionTypeLoadException e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(string.Join<Exception>("\n", e.LoaderExceptions));
                }
            }
        }

        #endregion

        #region mirror

        [CmdAttr("Usage: mirror Source=\"<Remote FetchClimate address>\"\nFederates all the data sources of the remote fetchclimate service, copies variables and mappings from it. \nIf the name of the remote data source coincides with the name of the one of already added data sources, it and all mappings referring it will be omitted.\nIf the name of the one of variables on the remote fetchclimate service coincides with the name of the one of already added varibles, but descriptions or units of those variable differ, variable from the remote service and all mappings referring it will be omitted.\nIf a variable on the remote service has same name, description and units as an already added varible, new mappings will be added to an existing variable.", "mirror:m")]
        public List<string> Mirror(string source)
        {
            IFetchConfiguration remoteConfig;
            try
            {
                RemoteFetchClient client = new RemoteFetchClient(new Uri(source));
                remoteConfig = client.GetConfiguration(DateTime.MaxValue);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to retrieve configuration of the remote service.\n Exception message: " + ex.Message);
            }
            var res = new List<string>();
            HashSet<string> varsToIgnore = new HashSet<string>();
            var localVars = db.GetEnvVariables().ToArray();
            var localDs = db.GetDataSources(DateTime.MaxValue).ToArray();

            foreach (var i in remoteConfig.EnvironmentalVariables)
            {
                var mayBeLocal = localVars.FirstOrDefault(x => x.DisplayName == i.Name);
                if (mayBeLocal == null)
                {
                    try
                    {
                        db.AddVariable(i.Name, i.Description, i.Units);
                        res.Add("Added variable " + i.Name + " with description: \"" + i.Description + "\" and units: " + i.Units);
                    }
                    catch (Exception ex)
                    {
                        varsToIgnore.Add(i.Name);
                        res.Add("Failed to add variable " + i.Name + ". Mappings referring it will be ignored. \nException message: " + ex.Message);
                    }
                }
                else if (mayBeLocal.Description != i.Description || mayBeLocal.Units != i.Units)
                {
                    varsToIgnore.Add(i.Name);
                    res.Add("Variable " + i.Name + @" already exists, but its description and/or units don't match those of corresponding variable on the remote service. Therefore no new data sources will be bound to it.");
                }
            }

            foreach (var i in remoteConfig.DataSources)
            {
                if (localDs.All(x => x.Name != i.Name))
                {
                    try
                    {

                        db.AddDataSource(i.Name, i.Description, i.Copyright, null, source, i.ID, i.Name);
                        res.Add("Added data source " + i.Name + ".\nDescription:\t" + i.Description + "\nCopyright:\t" + i.Copyright);
                        foreach (var j in i.ProvidedVariables)
                        {
                            if (!varsToIgnore.Contains(j))
                            {
                                try
                                {
                                    db.SetMapping(i.Name, j, j, true, true);
                                    res.Add("Bound data source " + i.Name + " to variable " + j + ".");
                                }
                                catch (Exception ex)
                                {
                                    res.Add("Failed to bind data source " + i.Name + " to variable " + j + ".\nException message: " + ex.Message);
                                }
                            }
                            else
                                res.Add("Data source " + i.Name + " was not bound to variable " + j + " even though such mapping existed on the remote service.");
                        }
                    }
                    catch (Exception ex)
                    {
                        res.Add("Failed to federate datasource " + i.Name + ".\nException message: " + ex.Message);
                    }
                }
                else
                    res.Add("Data source " + i.Name + " already exists. Therefore no such data source will be federated nor would be added any mappings referring it.");
            }

            return res;
        }

        #endregion
    }

    /// <summary>
    /// Marks a method as a FetchConfig command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    class CmdAttr : Attribute
    {
        private readonly string[] verbs;
        private string help;

        /// <summary>
        /// Marks a method as a FetchConfig command.
        /// </summary>
        /// <param name="help">A text that explains command usage.</param>
        /// <param name="verbs">A list of command line verbs that identify the command. Each verb is a colon separated list of verb forms.</param>
        public CmdAttr(string help, params string[] verbs)
        {
            this.verbs = verbs;
            this.help = help;
        }

        public IEnumerable<string> Verbs
        {
            get { return verbs; }
        }

        public string Help
        {
            get
            {
                return help;
            }
            set
            {
                help = value;
            }
        }

        public bool EqualsTo(IEnumerable<String> cmds)
        {
            if (cmds.Count() != verbs.Count())
                return false;

            int index = 0;
            foreach (var c in cmds)
            {
                if (!verbs[index++].Split(':').Contains(c))
                    return false;
            }
            return true;
        }

    }
}
