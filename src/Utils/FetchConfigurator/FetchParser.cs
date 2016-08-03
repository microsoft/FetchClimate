using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Research.Science.Data;
using Microsoft.Research.Science.FetchClimate2.Properties;
using System.Collections.Specialized;
using Microsoft.Research.Science.Data.Azure;
using Microsoft.Research.Science.Data.Factory;

namespace Microsoft.Research.Science.FetchClimate2
{
    class FetchParser
    {
        #region Fields

        private const string appFolder = "FetchClimate2";

        private string unknownParamsFormat = "Unknown parameters: {0}";
        private string prompt = "FC2:>";
        private string welcomeStr = @"  ______   _       _      _____ _ _                 _       ___  " + "\n" +
                                    @" |  ____| | |     | |    / ____| (_)               | |     |__ \ " + "\n" +
                                    @" | |__ ___| |_ ___| |__ | |    | |_ _ __ ___   __ _| |_ ___   ) |" + "\n" +
                                    @" |  __/ _ \ __/ __| '_ \| |    | | | '_ ` _ \ / _` | __/ _ \ / / " + "\n" +
                                    @" | | |  __/ || (__| | | | |____| | | | | | | | (_| | ||  __// /_ " + "\n" +
                                    @" |_|  \___|\__\___|_| |_|\_____|_|_|_| |_| |_|\__,_|\__\___|____|" + "\n";
        private string welcomeHelp =
            " * use local - manage configuration database used when running FC2 \'in-process\'\n" +
            " * use cloud - manage configuration database for Windows Azure (including Emulator)\n";

        #region HelpText
        string help =
                    " * Account (acc)\n" +
                    " * Assembly (asm)†\n" +
                    " * DataSource (ds)†\n" +
                    " * DataSet\n" +
                    " * Mirror (m)†\n" +
                    " * Reset (r)†\n" +
                    " * Variable (var)†\n" +
                    " * Use (u)\n" +
                    " * Help,? (h)\n" +
                    " * Quit,Exit (q)\n" +
                    "† Commands marked with dagger require a connected configuration to work. You can connect a configuration with the \"use\" command.";
        string useHelp =
                    " * local - work with InProcess mode configuration database\n" +
                    " * cloud AccountName=<Windows Azure account name> [AccountKey=\"<Windows Azure account key>\"] [SqlConnStr=\"SQL connection string\"] - work with configuration database in Windows Azure\n";

        string datasetHelp =
                    " * init - initializes azure chunked storage\n" +
                    " * create - creates a new azure data set\n" +
                    " * delete - deletes an existing azure data set (use with caution - this may cause incorrect work of data sources that require the data set being deleted!)\n" +
                    " * copy - copies data from one data set into another\n" +
                    " * append - appends one data set with the data from another\n" +
                    " * list - lists data sets stored on given Windows Azure account\n";
        string datasetInitHelp =
                    "Usage: dataset init AccountName=<Windows Azure account name> [AccountKey=\"<Windows Azure account key>\"] ConnectionString=\"SQL connection string\"\n" +
                    "Deploys data base and creates a configuration blob so that azure chunked storage instance credentials to which were provided can be used to store azure data sets.\n";
        string datasetCreateHelp =
                    "Usage: dataset create uri=\"msds:az?name=<Name>&AccountName=<Windows Azure account name>[&AccountKey=<Windows Azure account key>][&DefaultEndpointsProtocol=<http|https>]\" [compression=<on|off>] [chunks=<chunk size definition string>]\n" +
                    "Creates a new azure data set with specified uri. URIs are case-sensitive. Account key can be omitted in case it is present in the configuration file (it can be added there by using  the \"Account\" command). DefaultEndpointsProtocol will be set to \"http\" if omitted in uri. Compression is switched off by default.\n" +
                    "Chunk sizes for data set can be set by explicitly specifying chunk size definition string. This string should contain one or more chunk size definitions for variables with different numbers of dimensions divided by \";\". One chunk size definition must consist of a few numbers divided by a comma, numbers corresponding sizes of a chunk by different dimensions. For example, \"1024;256,256,512\" means than 1D variable chunk size is 1024 elements, 3D variable chunk size is 256x256x512 elements, variables of other dimensions have default chunk sizes.\n";
        string datasetDeleteHelp =
                    "Usage: dataset delete uri=\"msds:az?name=<Name>&AccountName=<Windows Azure account name>[&AccountKey=<Windows Azure account key>][&DefaultEndpointsProtocol=<http|https>]\"\n" +
                    "Deletes an azure data set with specified uri. URIs are case-sensitive. Account key can be omitted in case it is present in the configuration file (it can be added there by using  the \"Account\" command). DefaultEndpointsProtocol will be set to \"http\" if omitted in uri. Compression is switched off by default.\n" +
                    "Use with caution! This may cause incorrect work of data sources that require the data set being deleted!\n";
        string datasetCopyHelp =
                    "Usage: dataset copy target=\"<target data set uri>\" source=\"<source data set uri>\"\n" +
                    "Copies data from data from the source data set to the target data set. URIs are case-sensitive.\n";
        string datasetInfoHelp =
                    "Usage: dataset info source=\"<data set uri>\"\n" +
                    "Prints data set schema. URIs are case-sensitive.\n";
        string datasetAppendHelp =
                    "Usage: dataset append target=\"<target data set uri>\" source=\"<source data set uri>\" dimension=<name of dimension to append data on> [start=<starting index>]\n" +
                    "Appends the target data set with the data from the source data set along the given dimension. URIs are case-sensitive. Append is performed transactionally on \"slice-by-slice\" basis. This means that every new slice (by the given dimension) of data from the source data set is added in a separate transaction, so if something (e.g. network connection) breaks in the middle of operation data set will still be functional and a certain (reported) number of the slices of data will be added to it. You can continue a failed append operation by specifying a starting index, so that data from the source data set will be taken beginning with the slice of data corresponding to that index.\n" +
                    "If operation succeeds and database to be configured is specified (by the \"use\" command) then data sources from the database will be updated so that tey \n";
        string datasetListHelp =
                    "Usage: dataset list AccountName=<Windows Azure account name> [AccountKey=\"<Windows Azure account key>\"] [verbose=<yes|no>]\n" +
                    "Lists all data sets contained on the given Windows Azure account. Account key can be omitted in case it is present in the configuration file (it can be added there by using  the \"Account\" command). Setting \"verbose\" parameter to \"yes\" will cause detailed information about data sets to be shown.\n";

        string accountHelp =
                    " * add, set - adds new Windows Azure credentials to the configuration file or replaces key in credentials already existing there\n" +
                    " * delete - deletes Windows Azure credentials from configuration file\n" +
                    " * list - lists account names from credentials stored in configuration file\n";
        string accountSetHelp =
                    "Usage: account add|set name=<Windows Azure account name> key=\"<Windows Azure account key>\"\n" +
                    "Adds new Windows Azure credentials to the configuration file or replaces key in credentials already existing there. When credentials are stored in the configuration file there is no need to specify stored keys both in command parameters and URIs. Account name will still has to be provided.\n" +
                    "\"account add\" and \"account set\" are synonims.\n";
        string accountDeleteHelp =
                    "Usage: account delete name=<Windows Azure account name>\n" +
                    "Removes Windows Azure credentials with specified account name from the configuration file. If specified name is \"*\" then all credentials will be deleted.\n";
        string accountListHelp =
                    "Usage: account list\n" +
                    "Lists account names from credentials stored in configuration file.\n";

        string keyContainedPrompt = "Configuration file contains keys for the following accounts: ";

        #endregion

        /// <summary>
        /// Command word splitter
        /// </summary>
        private Regex wordsSplitReg = new Regex(@"([^ =!""><]+)|""([^""]*)""|([<>=!])|(@)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private FetchConfigurator commands;

        /// <summary>In batch mode utility never asks user confirmation</summary>
        private bool isBatchMode = false;

        #endregion

        #region Custom Read

        private const int READLINE_BUFFER_SIZE = 1024;
        /// <summary>
        /// Overcome console input max length
        /// </summary>
        /// <returns></returns>
        private static string ReadLine()
        {
            Stream inputStream = Console.OpenStandardInput(READLINE_BUFFER_SIZE);
            byte[] bytes = new byte[READLINE_BUFFER_SIZE];
            int outputLength = inputStream.Read(bytes, 0, READLINE_BUFFER_SIZE);
            //Console.WriteLine(outputLength);
            char[] chars = Encoding.ASCII.GetChars(bytes, 0, outputLength);
            return new string(chars);
        }
        #endregion

        /// <summary>
        /// Confirmation message
        /// </summary>
        /// <param name="message">Confirmation text</param>
        /// <returns></returns>
        private bool Confirm(string message)
        {
            if (isBatchMode)
                return true;

            while (true)
            {
                Console.Write(message);
                string read = Console.ReadLine().Trim().ToLower();
                if (read == "y")
                    return true;
                else if (read == "n")
                    return false;
            }
        }

        /// <summary>
        /// Parser entrance
        /// </summary>
        /// <param name="extCommands"></param>
        public void Start(string[] extCommands)
        {
            DataSetFactory.RegisterAssembly(typeof(Microsoft.Research.Science.Data.Azure.AzureDataSet).Assembly);
            try
            {
                Console.WriteLine(welcomeStr);
                Console.WriteLine("FetchConfig version {0}\n", Assembly.GetExecutingAssembly().GetName().Version);
                Console.WriteLine("Registered Scientific DataSet providers:");
                Console.WriteLine(DataSetFactory.RegisteredToString());
                if (Settings.Default.Accounts == null)
                {
                    Settings.Default.Accounts = new StringDictionary();
                    Settings.Default.Save();
                }
                if (Settings.Default.Accounts.Count > 0)
                {
                    string[] keys = new string[Settings.Default.Accounts.Count];
                    Settings.Default.Accounts.Keys.CopyTo(keys, 0);
                    WriteWarning(keyContainedPrompt + String.Join(", ", keys));
                }
                WriteHelp(welcomeHelp);
                try
                {
                    isBatchMode = true;
                    foreach (var c in extCommands)
                    {
                        Console.WriteLine(c);
                        if (!Parse(c))
                            return;
                    }
                }
                finally
                {
                    isBatchMode = false;
                }
                Console.Write(prompt);
                while (Parse(ReadLine()))
                {
                    Console.Write(prompt);
                }
            }
            finally
            {
                if (commands != null)
                    commands.Db.Dispose();
            }
        }
        #region Write
        private void WriteError(string msg, params object[] args)
        {
            using (new ForegroundColor(ConsoleColor.Red))
                Console.WriteLine(msg + "\n", args);
        }

        private void WriteHelp(string msg)
        {
            using (new ForegroundColor(ConsoleColor.Green))
                Console.WriteLine(msg + "\n");
        }

        private void WriteWarning(string msg)
        {
            using (new ForegroundColor(ConsoleColor.Yellow))
                Console.WriteLine(msg + "\n");
        }

        private void WriteInfo(string msg)
        {
            using (new ForegroundColor(ConsoleColor.DarkYellow))
                Console.WriteLine(msg + "\n");
        }
        #endregion

        /// <summary>
        /// Parses the line, returning true if more commands are allowed to be received (false for the last command in the operator session)
        /// </summary>
        private bool Parse(string line)
        {
            string syntaxError = "Syntax error.";
            line = line.Trim();

            if (string.IsNullOrEmpty(line) || line[0]=='#')
                return true;

            int a = 0;
            List<string> words = wordsSplitReg.Split(line).Where(s => ((a++) % 2 == 1)).ToList();
            List<String> cmds = new List<string>();
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            List<String> removeMapping = new List<string>();
            List<DataSourceMapping> addMapping = new List<DataSourceMapping>();

            int i = 0;
            bool cmdsFinished = false;
            bool err = false;
            while (i < words.Count)
            {
                if (words[i] != "!")
                {
                    if (words.Count > i + 1)
                    {
                        if (words[i + 1] != ">" && words[i + 1] != "<" && words[i + 1] != "=")
                        {
                            if (!cmdsFinished)
                            {
                                cmds.Add(words[i].ToLower());
                                i++;
                                continue;
                            }
                            else
                            {
                                WriteError(syntaxError + " Verbs should be stated at the beginning, but verb \"" + words[i] + "\" is not.");
                                err = true;
                                break;
                            }
                        }
                    }
                    else if (words.Count == i + 1)
                    {
                        if (!cmdsFinished)
                        {
                            cmds.Add(words[i].ToLower());
                            break;
                        }
                        else
                        {
                            WriteError(syntaxError + " Verbs should be stated at the beginning, but verb \"" + words[i] + "\" is not.");
                            err = true;
                            break;
                        }
                    }

                    if (words.Count > i + 2)
                    {
                        if (words[i + 1] == "=")
                        {
                            if (!parameters.ContainsKey(words[i]))
                            {
                                parameters.Add(words[i].ToLower(), words[i + 2]);
                                cmdsFinished = true;
                            }
                            else
                            {
                                WriteError(syntaxError);
                                err = true;
                                break;
                            }
                            i += 3;
                        }
                        else if (words[i + 1] == ">")
                        {
                            addMapping.Add(new DataSourceMapping(DataSourceMappingType.DataVariable, words[i], words[i + 2]));
                            cmdsFinished = true;
                            i += 3;
                        }
                        else if (words[i + 1] == "<")
                        {
                            addMapping.Add(new DataSourceMapping(DataSourceMappingType.DataParameter, words[i], words[i + 2]));
                            cmdsFinished = true;
                            i += 3;
                        }
                    }
                    else
                    {
                        WriteError(syntaxError);
                        err = true;
                        break;
                    }
                }
                else
                {
                    if (words.Count > i + 1)
                    {
                        if (!removeMapping.Contains(words[i + 1]))
                        {
                            removeMapping.Add(words[i + 1]);
                            cmdsFinished = true;
                        }
                        else
                        {
                            WriteError(syntaxError);
                            err = true;
                            break;
                        }
                        i += 2;
                    }
                    else
                    {
                        WriteError(syntaxError);
                        err = true;
                        break;
                    }
                }
            }

            if (cmds.Count == 0)
            {
                WriteError(syntaxError);
                err = true;
            }

            //return ReflectionExecute(cmds, parameters, addMapping, removeMapping);
            if (err) return true;
            return Execute(cmds, parameters, addMapping, removeMapping);
        }

        /// <summary>
        /// Command executer
        /// </summary>
        /// <param name="cmds">Commands list</param>
        /// <param name="parameters">Parameters list</param>
        /// <param name="addMapping">New mappings list</param>
        /// <param name="removeMapping">Mappings to remove list</param>
        /// <returns></returns>
        private bool Execute(List<String> cmds, Dictionary<string, string> parameters, List<DataSourceMapping> addMapping, List<String> removeMapping)
        {
            if (cmds.Count > 0)
            {
                //first, commands that don't require connected Configurator
                switch (cmds[0].ToLower())
                {
                    case "quit":
                    case "exit":
                    case "q":
                        return false;

                    case "use":
                    case "u":
                        Use(cmds.Skip(1).ToList(), parameters);
                        return true;

                    //case ﻿"reset":
                    //case "r":
                    //    Reset(cmds.Skip(1).ToList(), parameters);
                    //    return true;

                    case "@":
                        ExecuteFile(cmds.Skip(1).ToList());
                        return true;

                    case "dataset":
                        DatasetCmd(cmds.Skip(1).ToList(), parameters);
                        return true;

                    case "account":
                    case "acc":
                        Account(cmds.Skip(1).ToList(), parameters);
                        return true;

                    case "help":
                    case "h":
                    case "?": Help(cmds.Skip(1).ToArray()); return true;
                }

                if (commands == null || !commands.Db.DatabaseExists())
                {
                    WriteError("Choose the configuration to work");
                    WriteHelp(welcomeHelp);
                    return true;
                }
                //if (cmds[0].ToLower() == "reset" || cmds[0].ToLower() == "r")
                //{
                //    Reset(cmds.Skip(1).ToList(), parameters);
                //    return true;
                //}
                return ReflectionExecute(cmds, parameters, addMapping, removeMapping);
            }
            return true;
        }

        #region Commands

        #region Dataset

        private void DatasetCmd(List<string> cmds, Dictionary<string, string> parameters)
        {
            if (cmds.Count == 1)
            {
                switch (cmds[0].ToLower())
                {
                    case "create":
                        DatasetCreate(parameters);
                        break;
                    case "delete":
                        DatasetDelete(parameters);
                        break;
                    case "copy":
                        DatasetCopy(parameters);
                        break;
                    case "append":
                        DatasetAppend(parameters);
                        break;
                    case "list":
                        DatasetList(parameters);
                        break;
                    case "info":
                        DatasetInfo(parameters);
                        break;
                    case "init":
                        DatasetInit(parameters);
                        break;
                    default:
                        Help("dataset");
                        break;
                }
            }
            else
            {
                Help("dataset");
            }

        }

        private AzureChunkStorage OpenStorageForUri(string uri)
        {
            DataSetUri dsuri = new DataSetUri(uri);

            if (dsuri.ProviderName != "az")
                throw new Exception("Can process only azure data sets");
            string protocol = "http";
            if (dsuri.ContainsParameter("DefaultEndpointsProtocol"))
                protocol = dsuri.GetParameterValue("DefaultEndpointsProtocol");
            if (!dsuri.ContainsParameter("AccountName"))
                throw new ArgumentException("Uri must contain \"AccountName\" parameter!");
            string accName = dsuri.GetParameterValue("AccountName");
            if (!dsuri.ContainsParameter("AccountKey"))
                throw new ArgumentException("Uri must contain \"AccountKey\" parameter!");
            string accKey = dsuri.GetParameterValue("AccountKey");
            string credentials = @"DefaultEndpointsProtocol=" + protocol + @";AccountName=" + accName + @";AccountKey=" + accKey;
            return new AzureChunkStorage(credentials);
        }

        private void DatasetInit(Dictionary<string, string> parameters)
        {
            const string connStrParam = "sqlconnstr";

            String requiredFields = "Please supply the \"accountName\" and \"" + connStrParam + "\" parameters";
            string accName, accKey, connstring, credentials;

            var unknownParams = ParserHelper.Filter(parameters.Keys.Select(o => o.ToString()), "accountname", "accountkey", connStrParam);
            if (unknownParams.Count() > 0)
            {
                WriteError(String.Format(unknownParamsFormat, String.Join(", ", unknownParams)));
                return;
            }

            if (parameters.ContainsKey("accountname") && parameters.ContainsKey(connStrParam))
            {
                try
                {
                    accName = parameters["accountname"];
                    connstring = parameters[connStrParam];
                    //if (accName != "UseDevelopmentStorage")
                    //{
                    if (parameters.ContainsKey("accountkey"))
                        accKey = parameters["accountkey"];
                    else
                    {
                        if (Settings.Default.Accounts.ContainsKey(accName))
                        {
                            accKey = Settings.Default.Accounts[accName];
                        }
                        else
                        {
                            WriteError("Parameter \"accountKey\" is neither specified nor can be found in the configuration file. Please, specify it explicitly or add it to the configuration file with the \"account add\" command.");
                            return;
                        }
                    }
                    credentials = @"DefaultEndpointsProtocol=http;AccountName=" + accName + @";AccountKey=" + accKey;
                    //}
                    //else
                    //    credentials = "UseDevelopmentStorage=true";

                    AzureChunkStorage azureChunkStorage;
                    try
                    {
                        azureChunkStorage = new AzureChunkStorage(credentials);
                    }
                    catch (Exception ex)
                    {
                        WriteError("Failed to open azure storage: " + ex.Message);
                        return;
                    }

                    bool configExists = true;
                    try
                    {
                        var test1 = azureChunkStorage.ConnectionString;
                    }
                    catch (ApplicationException ex)
                    {
                        if (ex.InnerException != null && ex.InnerException is StorageClientException)
                            configExists = false;
                    }

                    if (configExists)
                    {
                        WriteError("This azure chunked storage is already initialized.");
                        return;
                    }

                    bool isSqlAvailable = SqlHelper.IsSqlServerAvailable(connstring);
                    bool doesDbExists = SqlHelper.DoesDataBaseExist(connstring);
                    bool doesDbSchemaDeployed = SqlHelper.IsDataBaseDeployed(connstring);

                    if (!isSqlAvailable)
                    {
                        WriteError("Specified SQL server can't be reached ({0})", connstring);
                        return;
                    }
                    if (!doesDbExists)
                        SqlHelper.CreateDatabase(connstring);
                    if (!doesDbSchemaDeployed)
                    {
                        string sqlText;
                        using (StreamReader reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Microsoft.Research.Science.FetchClimate2.Database.AzureScript.sql")))
                        {
                            sqlText = reader.ReadToEnd();
                        }
                        SqlHelper.ExecuteSqlScript(connstring, sqlText);
                    }
                    else
                    {
                        WriteError("A data base is already deployed on the specified SQL server ({0})", connstring);
                        return;
                    }

                    azureChunkStorage.CreateConfigurationBlob(connstring);
                }
                catch (Exception ex)
                {
                    WriteError("Unexpected error: " + ex.Message);
                }
            }
            else
            {
                WriteError(requiredFields);
                Help("dataset", "init");
            }
        }

        private void DatasetCreate(Dictionary<string, string> parameters)
        {
            //TODO: consider syntax revision
            String requiredFields = "Please supply the \"uri\" parameter";
            string uri, chunks, compression;
            List<int[]> chunkShapeList = new List<int[]>();

            var unknownParams = ParserHelper.Filter(parameters.Keys.Select(o => o.ToString()), "uri", "compression", "chunks");
            if (unknownParams.Count() > 0)
            {
                WriteError(String.Format(unknownParamsFormat, String.Join(", ", unknownParams)));
                return;
            }

            if (parameters.ContainsKey("uri"))
            {
                try
                {
                    try
                    {
                        uri = ParserHelper.AppendAzureUriWithAccountKey(parameters["uri"]);
                    }
                    catch (Exception ex)
                    {
                        WriteError(ex.Message);
                        return;
                    }

                    if (parameters.ContainsKey("chunks"))
                        chunks = parameters["chunks"];
                    else
                        chunks = string.Empty;
                    if (parameters.ContainsKey("compression"))
                        compression = parameters["compression"].ToLower();
                    else
                        compression = "off";

                    if (compression != "on" && compression != "off")
                    {
                        WriteError("Compression must be either \"on\" or \"off\"");
                        return;
                    }

                    int deflation = compression == "off" ? 0 : 1;

                    try
                    {
                        if (chunks != string.Empty)
                        {
                            var shapes = chunks.Split(';');
                            foreach (var i in shapes)
                            {
                                var sizes = i.Split(',');
                                int rank = sizes.Length;
                                int[] chunkshape = new int[rank];
                                for (int j = 0; j < rank; j++)
                                {
                                    chunkshape[j] = int.Parse(sizes[j], CultureInfo.InvariantCulture);
                                }
                                chunkShapeList.Add(chunkshape);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError("Unable to parse chunks shapes string: " + ex.Message);
                        return;
                    }

                    DataSetUri dsuri = new DataSetUri(uri);
                    if (!dsuri.ContainsParameter("name"))
                    {
                        WriteError("Uri must contain data set name!");
                        return;
                    }

                    string dsname = dsuri.GetParameterValue("name");

                    AzureChunkStorage azureChunkStorage;
                    try
                    {
                        azureChunkStorage = OpenStorageForUri(uri);
                    }
                    catch (Exception ex)
                    {
                        WriteError("Failed to open azure storage: " + ex.Message);
                        return;
                    }

                    //may be should check for name duplication?
                    // VL: yes, that's important. Two datasets with the same name make AzureDataSet fail to access either of them.
                    int dsID;
                    try
                    {
                        var duplicate = azureChunkStorage.Any(x =>
                            x.AttributesDictionary.ContainsKey(AzureChunkStorage.attributeKeyForName)
                            && x.AttributesDictionary[AzureChunkStorage.attributeKeyForName] is string
                            && dsname == (string)x.AttributesDictionary[AzureChunkStorage.attributeKeyForName]
                            );
                        if (duplicate) throw new ArgumentException("The storage contains a DataSet named " + dsname);
                        dsID = azureChunkStorage.CreateDataSet(dsname, deflation);
                    }
                    catch (Exception ex)
                    {
                        WriteError("Failed to create data set: " + ex.Message);
                        return;
                    }
                    WriteInfo(String.Format("DataSet {0} created. DataSet id in storage is {1}.", dsname, dsID));

                    if (chunks != string.Empty)
                    {
                        foreach (var i in chunkShapeList)
                        {
                            try
                            {
                                azureChunkStorage.SetDefaultChunkShape(dsname, i.Length, i);
                            }
                            catch (Exception ex)
                            {
                                WriteError("Failed to set chunk shape " + string.Join(", ", i.ToString()) + ": " + ex.Message);
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    WriteError("Unexpected error: " + ex.Message);
                }
            }
            else
            {
                WriteError(requiredFields);
                Help("dataset", "create");
            }
        }

        private void DatasetDelete(Dictionary<string, string> parameters)
        {
            //TODO:consider syntax revision
            String requiredFields = "Please supply the \"uri\" parameter";
            string uri;

            var unknownParams = ParserHelper.Filter(parameters.Keys.Select(o => o.ToString()), "uri");
            if (unknownParams.Count() > 0)
            {
                WriteError(String.Format(unknownParamsFormat, String.Join(", ", unknownParams)));
                return;
            }

            if (parameters.ContainsKey("uri"))
            {
                try
                {
                    try
                    {
                        uri = ParserHelper.AppendAzureUriWithAccountKey(parameters["uri"]);
                    }
                    catch (Exception ex)
                    {
                        WriteError(ex.Message);
                        return;
                    }

                    DataSetUri dsuri = new DataSetUri(uri);
                    if (!dsuri.ContainsParameter("name"))
                    {
                        WriteError("Uri must contain data set name!");
                        return;
                    }

                    string dsname = dsuri.GetParameterValue("name");

                    AzureChunkStorage azureChunkStorage;
                    try
                    {
                        azureChunkStorage = OpenStorageForUri(uri);
                    }
                    catch (Exception ex)
                    {
                        WriteError("Failed to open azure storage: " + ex.Message);
                        return;
                    }

                    try
                    {
                        azureChunkStorage.DropDataSet(dsname);
                    }
                    catch (Exception ex)
                    {
                        WriteError("Failed to delete data set: " + ex.Message);
                        return;
                    }
                    WriteInfo(String.Format("DataSet {0} deleted.", dsname));
                }
                catch (Exception ex)
                {
                    WriteError("Unexpected error: " + ex.Message);
                }
            }
            else
            {
                WriteError(requiredFields);
                Help("dataset", "delete");
            }
        }

        private void DatasetCopy(Dictionary<string, string> parameters)
        {
            String requiredFields = "Please supply the \"Target\" and \"Source\" parameters";
            string targetUri, sourceUri;

            var unknownParams = ParserHelper.Filter(parameters.Keys.Select(o => o.ToString()), "target", "source");
            if (unknownParams.Count() > 0)
            {
                WriteError(String.Format(unknownParamsFormat, String.Join(", ", unknownParams)));
                return;
            }

            if (parameters.ContainsKey("target") && parameters.ContainsKey("source"))
            {
                try
                {
                    try
                    {
                        targetUri = ParserHelper.AppendAzureUriWithAccountKey(parameters["target"]);
                        sourceUri = ParserHelper.AppendAzureUriWithAccountKey(parameters["source"]);
                    }
                    catch (Exception ex)
                    {
                        WriteError(ex.Message);
                        return;
                    }

                    Microsoft.Research.Science.Data.DataSet ds = Microsoft.Research.Science.Data.DataSet.Open(sourceUri);
                    ds.IsAutocommitEnabled = false;

                    DataSetUri dstUri = DataSetUri.Create(targetUri);
                    if (dstUri.ProviderName.StartsWith("memory"))
                    {
                        WriteError("Copying to memory is not supported.");
                        ds.Dispose();
                        return;
                    }
                    Microsoft.Research.Science.Data.DataSet ds2 = Microsoft.Research.Science.Data.Utilities.DataSetCloning.Clone(ds, dstUri);
                    ds2.Dispose();
                    ds.Dispose();
                }
                catch (Exception ex)
                {
                    WriteError("Unexpected error: " + ex.Message);
                }
            }
            else
            {
                WriteError(requiredFields);
                Help("dataset", "copy");
            }
        }

        private void DatasetInfo(Dictionary<string, string> parameters)
        {
            String requiredFields = "Please supply the \"uri\" parameter";
            string sourceUri;

            var unknownParams = ParserHelper.Filter(parameters.Keys.Select(o => o.ToString()), "uri");
            if (unknownParams.Count() > 0)
            {
                WriteError(String.Format(unknownParamsFormat, String.Join(", ", unknownParams)));
                return;
            }

            if (parameters.ContainsKey("uri"))
            {
                try
                {
                    try
                    {
                        sourceUri = ParserHelper.AppendAzureUriWithAccountKey(parameters["uri"]);
                    }
                    catch (Exception ex)
                    {
                        WriteError(ex.Message);
                        return;
                    }

                    using (Microsoft.Research.Science.Data.DataSet ds = Microsoft.Research.Science.Data.DataSet.Open(sourceUri))
                    using (ForegroundColor fc = new ForegroundColor(ConsoleColor.Green))
                        Console.WriteLine(ds.ToString());
                }
                catch (Exception ex)
                {
                    WriteError("Unexpected error: " + ex.Message);
                }
            }
            else
            {
                WriteError(requiredFields);
                Help("dataset", "info");
            }
        }

        private void DatasetList(Dictionary<string, string> parameters)
        {
            String requiredFields = "Please supply the \"AccountName\" parameter";
            string name, key;
            bool verbose = false;

            var unknownParams = ParserHelper.Filter(parameters.Keys.Select(o => o.ToString()), "accountname", "accountkey", "verbose");
            if (unknownParams.Count() > 0)
            {
                WriteError(String.Format(unknownParamsFormat, String.Join(", ", unknownParams)));
                return;
            }

            if (parameters.ContainsKey("accountname"))
            {
                try
                {
                    name = parameters["accountname"];
                    if (!parameters.ContainsKey("accountkey"))
                    {
                        if (Settings.Default.Accounts.ContainsKey(name))
                            key = Settings.Default.Accounts[name];
                        else
                        {
                            WriteError("Account key was not specified and it does not exist in the configuration file.");
                            return;
                        }
                    }
                    else
                        key = parameters["accountkey"];

                    if (parameters.ContainsKey("verbose"))
                        if (parameters["verbose"].ToLower() == "yes")
                            verbose = true;
                        else if (parameters["verbose"].ToLower() != "no")
                        {
                            WriteError("Verbose must be either \"yes\" or \"no\".");
                            return;
                        }

                    string credentials = @"DefaultEndpointsProtocol=http;AccountName=" + name + @";AccountKey=" + key;

                    AzureChunkStorage storage;
                    try
                    {
                        storage = new AzureChunkStorage(credentials);
                    }
                    catch (Exception ex)
                    {
                        WriteError("Failed to open azure storage: " + ex.Message);
                        return;
                    }

                    foreach (var schema in storage)
                    {
                        Console.Write(schema.ID);
                        if (schema.Deleted) Console.Write(" (deleted)");
                        if (schema.Deflation > 0) Console.Write(" (compressed {0})", schema.Deflation);
                        if (verbose)
                        {
                            Console.WriteLine();
                            foreach (var kv in schema.AttributesDictionary)
                                if (kv.Value is System.Collections.IEnumerable && !(kv.Value is string))
                                {
                                    List<string> v = new List<string>();
                                    var i = ((System.Collections.IEnumerable)kv.Value).GetEnumerator();
                                    while (i.MoveNext()) v.Add(i.Current.ToString());
                                    Console.WriteLine("\t{0} = {1}", kv.Key, string.Join(" ", v));
                                }
                                else
                                    Console.WriteLine("\t{0} = {1}", kv.Key, kv.Value);
                            Console.WriteLine();
                        }
                        else
                        {
                            if (schema.AttributesDictionary.ContainsKey(AzureChunkStorage.attributeKeyForName))
                                Console.WriteLine(" " + schema.AttributesDictionary[AzureChunkStorage.attributeKeyForName]);
                            else
                                Console.WriteLine();
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteError("Unexpected error: " + ex.Message);
                }
            }
            else
            {
                WriteError(requiredFields);
                Help("dataset", "list");
            }
        }

        private void DatasetAppend(Dictionary<string, string> parameters)
        {
            const int MAX_APPENDING_SIZE = 199 * 1024 * 1024;
            String requiredFields = "Please supply the \"Target\", \"Source\", and \"Dimension\" parameters";
            string targetUri, sourceUri, dim;
            int startIdx = 0;
            bool justOneSlice = false;

            var unknownParams = ParserHelper.Filter(parameters.Keys.Select(o => o.ToString()), "target", "source", "dimension", "start");
            if (unknownParams.Count() > 0)
            {
                WriteError(String.Format(unknownParamsFormat, String.Join(", ", unknownParams)));
                return;
            }

            if (parameters.ContainsKey("target") && parameters.ContainsKey("source") && parameters.ContainsKey("dimension"))
            {
                try
                {
                    targetUri = ParserHelper.AppendAzureUriWithAccountKey(parameters["target"]);
                    sourceUri = ParserHelper.AppendAzureUriWithAccountKey(parameters["source"]);
                }
                catch (Exception ex)
                {
                    WriteError(ex.Message);
                    return;
                }

                Microsoft.Research.Science.Data.DataSet src, dst;
                try
                {
                    src = Microsoft.Research.Science.Data.DataSet.Open(sourceUri);
                }
                catch (Exception ex)
                {
                    WriteError("Failed to open source data set: " + ex.Message);
                    return;
                }
                try
                {
                    dst = Microsoft.Research.Science.Data.DataSet.Open(targetUri);
                }
                catch (Exception ex)
                {
                    WriteError("Failed to open target data set: " + ex.Message);
                    src.Dispose();
                    return;
                }

                try
                {
                    if (parameters.ContainsKey("start") && !int.TryParse(parameters["start"], out startIdx))
                    {
                        WriteError("Failed to parse the \"Start\" parameter.");
                        return;
                    }
                    src.IsAutocommitEnabled = false;
                    dst.IsAutocommitEnabled = false;

                    dim = parameters["dimension"];

                    string[] vars = dst.Variables.Where(v => v.Dimensions.Any(d => d.Name == dim)).Select(v => v.Name).ToArray();
                    Dimension[] dims = dst.Dimensions.Where(d => vars.Any(v => src[v].Dimensions.Any(sd => sd.Name == d.Name))).ToArray();

                    if (vars.Any(v => src.Variables.All(dv => dv.Name != v)))
                    {
                        WriteError("Not every variable from the target data set, that depends on dimension \"" + dim + "\" is present in the source data set.");
                        src.Dispose();
                        dst.Dispose();
                        return;
                    }

                    foreach (var varname in vars)
                    {
                        if (dst[varname].Dimensions.Any(d => d.Name != dim && src[varname].Dimensions.All(sd => sd.Name != d.Name)) ||
                            src[varname].Dimensions.Any(sd => dst[varname].Dimensions.All(d => d.Name != sd.Name)))
                        {
                            WriteError("Dimension lists for variable \"" + varname + "\" differ in source and target data sets.");
                            src.Dispose();
                            dst.Dispose();
                            return;
                        }
                    }

                    if (dims.Any(d => d.Name != dim && src.Dimensions[d.Name].Length != d.Length))
                    {
                        WriteError("Dimension lengths in source and target data sets are not syncronized.");
                        src.Dispose();
                        dst.Dispose();
                        return;
                    }

                    if (src.Dimensions.All(d => d.Name != dim) || src.Dimensions[dim].Length == 1 || vars.All(v => src[v].Dimensions.All(d => d.Name != dim)))
                        justOneSlice = true;
                    else if (vars.Any(v => src[v].Dimensions.All(d => d.Name != dim)))
                    {
                        WriteError("Dimension \"" + dim + "\" exists in the source data set, its length is greater than 1, and some of the relevant variables from the source data set do not depend on it.");
                        src.Dispose();
                        dst.Dispose();
                        return;
                    }

                    if (!justOneSlice && src.Dimensions[dim].Length <= startIdx)
                    {
                        WriteError("Dimension \"" + dim + "\"\'s length in the source data set is less than starting index.");
                        src.Dispose();
                        dst.Dispose();
                        return;
                    }

                    if (justOneSlice && startIdx > 0)
                    {
                        WriteError("There is only one slice of data in the source data set but starting index is greater than zero.");
                        src.Dispose();
                        dst.Dispose();
                        return;
                    }

                    if (justOneSlice)
                    {
                        foreach (var varname in vars)
                        {
                            int rank = dst[varname].Rank;
                            if (rank == src[varname].Rank)
                            {
                                for (int i = 0; i < rank; ++i)
                                    if (dst[varname].Dimensions[i].Name != src[varname].Dimensions[i].Name)
                                    {
                                        WriteError("Variable \"" + varname + "\" has different order of dimensions in source and target data sets.");
                                        src.Dispose();
                                        dst.Dispose();
                                        return;
                                    }
                            }
                            else
                            {
                                int i = 0;
                                while (dst[varname].Dimensions[i].Name != dim)
                                {
                                    if (dst[varname].Dimensions[i].Name != src[varname].Dimensions[i].Name)
                                    {
                                        WriteError("Variable \"" + varname + "\" has different order of dimensions in source and target data sets.");
                                        src.Dispose();
                                        dst.Dispose();
                                        return;
                                    }
                                    ++i;
                                }
                                for (i = i + 1; i < rank; ++i)
                                    if (dst[varname].Dimensions[i].Name != src[varname].Dimensions[i - 1].Name)
                                    {
                                        WriteError("Variable \"" + varname + "\" has different order of dimensions in source and target data sets.");
                                        src.Dispose();
                                        dst.Dispose();
                                        return;
                                    }
                            }
                        }
                        foreach (var varname in vars)
                        {
                            var data = src[varname].GetData();
                            dst[varname].Append(data, dim);
                        }
                        try
                        {
                            dst.Commit();
                        }
                        catch (Exception ex)
                        {
                            WriteError("Failed to upload: " + ex.Message);
                            src.Dispose();
                            dst.Dispose();
                            return;
                        }
                        WriteInfo("Successfully added slice 0 to the target data set.");
                    }
                    else
                    {
                        foreach (var varname in vars)
                        {
                            int rank = dst[varname].Rank;
                            for (int i = 0; i < rank; ++i)
                                if (dst[varname].Dimensions[i].Name != src[varname].Dimensions[i].Name)
                                {
                                    WriteError("Variable \"" + varname + "\" has different order of dimensions in source and target data sets.");
                                    src.Dispose();
                                    dst.Dispose();
                                    return;
                                }
                        }
                        int sliceSize = 0;
                        int slicesPerTry = 1;
                        foreach (var varname in vars)
                        {
                            int rank = dst[varname].Rank;
                            Type varType = src[varname].TypeOfData;
                            int varSliceSize = 1;
                            if (varType == typeof(Double)) varSliceSize = sizeof(Double);
                            else if (varType == typeof(Single)) varSliceSize = sizeof(Single);
                            else if (varType == typeof(Int16)) varSliceSize = sizeof(Int16);
                            else if (varType == typeof(Int32)) varSliceSize = sizeof(Int32);
                            else if (varType == typeof(Int64)) varSliceSize = sizeof(Int64);
                            else if (varType == typeof(UInt16)) varSliceSize = sizeof(UInt16);
                            else if (varType == typeof(UInt32)) varSliceSize = sizeof(UInt32);
                            else if (varType == typeof(UInt64)) varSliceSize = sizeof(UInt64);
                            else if (varType == typeof(Byte)) varSliceSize = sizeof(Byte);
                            else if (varType == typeof(SByte)) varSliceSize = sizeof(SByte);
                            else if (varType == typeof(DateTime)) varSliceSize = sizeof(Int64);
                            else if (varType == typeof(Boolean)) varSliceSize = sizeof(Boolean);
                            else
                            {
                                sliceSize = -1;
                                break;
                            }

                            for (int i = 0; i < rank; ++i)
                            {
                                if (src[varname].Dimensions[i].Name != dim) varSliceSize *= src[varname].Dimensions[i].Length;
                            }
                            sliceSize += varSliceSize;
                        }
                        if (sliceSize == -1)
                            slicesPerTry = 1;
                        else
                            slicesPerTry = MAX_APPENDING_SIZE / sliceSize;

                        int length = src.Dimensions[dim].Length;
                        for (int i = startIdx; i < length; i += slicesPerTry)
                        {
                            int toAppend = Math.Min(slicesPerTry, length - i);
                            foreach (var varname in vars)
                            {
                                int rank = dst[varname].Rank;
                                string[] curdims = src[varname].Dimensions.Select(d => d.Name).ToArray();
                                int shapeIndex = Array.IndexOf<string>(curdims, dim);
                                int[] origin = new int[rank];
                                int[] shape = new int[rank];
                                for (int j = 0; j < rank; ++j)
                                {
                                    if (j == shapeIndex)
                                    {
                                        origin[j] = i;
                                        shape[j] = toAppend;
                                    }
                                    else
                                    {
                                        origin[j] = 0;
                                        shape[j] = src[varname].Dimensions[j].Length;
                                    }
                                }
                                var data = src[varname].GetData(origin, shape);
                                dst[varname].Append(data, shapeIndex);
                            }
                            try
                            {
                                dst.Commit();
                            }
                            catch (Exception ex)
                            {
                                WriteError("Failed to upload " + toAppend.ToString() + " slices beginning with slice #" + i.ToString() + ": " + ex.Message);
                                src.Dispose();
                                dst.Dispose();
                                return;
                            }
                            WriteInfo("Successfully added " + toAppend.ToString() + " slices beginning with slice #" + i.ToString() + " to the target data set.");
                        }

                        if (commands != null)
                        {
                            var tgUri = new DataSetUri(targetUri);
                            var localDs = commands.Db.GetDataSources(DateTime.MaxValue).ToArray();
                            foreach (var ds in localDs)
                            {
                                if (ds.RemoteID == null && DataSetUri.IsDataSetUri(ds.Uri))
                                {
                                    try
                                    {
                                        var curUri = new DataSetUri(ds.Uri);
                                        if (curUri.ProviderName == tgUri.ProviderName)
                                        {
                                            if (curUri.ContainsParameter("dimensions"))
                                                curUri.RemoveParameter("dimensions");
                                            if (curUri.ParameterKeys.All(pk => tgUri.ContainsParameter(pk) && (tgUri.GetParameterValue(pk) == curUri.GetParameterValue(pk))))
                                            {
                                                try
                                                {
                                                    commands.DataSourceUpdate(ds.Name);
                                                    WriteInfo("Successfully updated data source \"" + ds.Name + "\"");
                                                }
                                                catch (Exception ex)
                                                {
                                                    WriteError(ex.Message);
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        WriteError("Unexpected error: " + ex.Message);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteError("Unexpected error: " + ex.Message);
                }
                finally
                {
                    src.Dispose();
                    dst.Dispose();
                }
            }
            else
            {
                WriteError(requiredFields);
                Help("dataset", "append");
            }
        }

        #endregion

        #region account

        private void Account(List<string> cmds, Dictionary<string, string> parameters)
        {
            if (cmds.Count == 1)
            {
                switch (cmds[0].ToLower())
                {
                    case "add":
                    case "set":
                        AccountSet(parameters);
                        break;
                    case "delete":
                        AccountDelete(parameters);
                        break;
                    case "list":
                        AccountList(parameters);
                        break;
                    default:
                        Help("account");
                        break;
                }
            }
            else
            {
                Help("account");
            }

        }

        private void AccountDelete(Dictionary<string, string> parameters)
        {
            String requiredFields = "Please supply the \"Name\" parameter";

            var unknownParams = ParserHelper.Filter(parameters.Keys.Select(o => o.ToString()), "name");
            if (unknownParams.Count() > 0)
            {
                WriteError(String.Format(unknownParamsFormat, String.Join(", ", unknownParams)));
                return;
            }

            if (parameters.ContainsKey("name"))
            {
                try
                {
                    var name = parameters["name"];
                    if (name == "*" &&
                        Confirm("This is going to delete ALL the accounts information from configuration file. Are you surely want to proceed?"))
                    {
                        Settings.Default.Accounts.Clear();
                        Settings.Default.Save();
                    }
                    else
                    {
                        if (Settings.Default.Accounts.ContainsKey(name))
                        {
                            Settings.Default.Accounts.Remove(name);
                            Settings.Default.Save();
                        }
                        else
                        {
                            WriteError("Configuration file does not contain the key for given account.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteError("Error while deleting account information: " + ex.ToString());
                }
            }
            else
            {
                WriteError(requiredFields);
                Help("account", "delete");
            }
        }

        private void AccountSet(Dictionary<string, string> parameters)
        {
            String requiredFields = "Please supply the \"Name\" and \"Key\" parameters";

            var unknownParams = ParserHelper.Filter(parameters.Keys.Select(o => o.ToString()), "name", "key");
            if (unknownParams.Count() > 0)
            {
                WriteError(String.Format(unknownParamsFormat, String.Join(", ", unknownParams)));
                return;
            }

            if (parameters.ContainsKey("name") && parameters.ContainsKey("key"))
            {
                try
                {
                    var name = parameters["name"];
                    var key = parameters["key"];
                    if (Settings.Default.Accounts.ContainsKey(name))
                    {
                        if (Confirm("A key for the given account name already exists in the configuration file. Override?"))
                        {
                            Settings.Default.Accounts[name] = key;
                            Settings.Default.Save();
                        }
                    }
                    else
                    {
                        Settings.Default.Accounts.Add(name, key);
                        Settings.Default.Save();
                    }
                }
                catch (Exception ex)
                {
                    WriteError("Error while adding account information: " + ex.ToString());
                }
            }
            else
            {
                WriteError(requiredFields);
                Help("account", "set");
            }
        }

        private void AccountList(Dictionary<string, string> parameters)
        {
            var unknownParams = ParserHelper.Filter(parameters.Keys.Select(o => o.ToString()));
            if (unknownParams.Count() > 0)
            {
                WriteError(String.Format(unknownParamsFormat, String.Join(", ", unknownParams)));
                return;
            }

            try
            {
                if (Settings.Default.Accounts.Count > 0)
                {
                    string[] keys = new string[Settings.Default.Accounts.Count];
                    Settings.Default.Accounts.Keys.CopyTo(keys, 0);
                    Console.WriteLine(String.Join("\n", keys));
                }
            }
            catch (Exception ex)
            {
                WriteError("Unexpected error: " + ex.ToString());
            }
        }

        #endregion

        private void Help(params string[] cmds)
        {
            //TODO: help for account and dataset
            string print = help;

            //Print main help message
            if (cmds.Count() == 0)
            {
                WriteHelp(print);
                return;
            }

            //Search for help in FetchConfigurator method attributes
            MethodInfo mi = null;
            if (commands != null)
                mi = commands.GetType().GetMethods()
                    .Where(m => m.GetCustomAttributes(true)
                    .Any(a => a is CmdAttr && ((CmdAttr)a).EqualsTo(cmds))).FirstOrDefault();

            //If method is found
            if (mi != null)
            {
                var attr = mi.GetCustomAttribute(typeof(CmdAttr));
                print = ((CmdAttr)attr).Help;
            }

            //Such method not found
            else
            {
                //Search for help in current class
                if (!String.IsNullOrEmpty(cmds[0]))
                {
                    switch (cmds[0].ToLower())
                    {
                        case "use": print = useHelp; break;
                        case "acc":
                        case "account":
                            print = accountHelp;
                            if (cmds.Length > 1)
                            {
                                switch (cmds[1].ToLower())
                                {
                                    case "add":
                                    case "set": print = accountSetHelp; break;
                                    case "delete": print = accountDeleteHelp; break;
                                    case "list": print = accountListHelp; break;
                                }
                            }
                            break;
                        case "dataset":
                            print = datasetHelp;
                            if (cmds.Length > 1)
                            {
                                switch (cmds[1].ToLower())
                                {
                                    case "init": print = datasetInitHelp; break;
                                    case "create": print = datasetCreateHelp; break;
                                    case "delete": print = datasetDeleteHelp; break;
                                    case "copy": print = datasetCopyHelp; break;
                                    case "info": print = datasetInfoHelp; break;
                                    case "append": print = datasetAppendHelp; break;
                                    case "list": print = datasetListHelp; break;
                                }
                            }
                            break;
                    }
                }
            }

            WriteHelp(print);
        }

        //private void Reset(List<string> cmds, Dictionary<string, string> parameters)
        //{
        //    if (Confirm("WARNING! You are about to reset all configuration database content. Are you sure? [y/n]:"))
        //    {
        //        commands.Reset();
        //    }
        //}

        #endregion

        #region Use
        /// <summary>
        /// Start working with local database.
        /// </summary>
        /// <param name="cmds"></param>
        private void UseLocal(List<string> cmds)
        {
            commands = new FetchConfigurator(null, SharedConstants.LocalConfigurationConnectionString, false);
        }

        /// <summary>
        /// Start working with cload database.
        /// </summary>
        /// <param name="cmds"></param>
        /// <param name="parameters"></param>
        private void UseCloud(List<string> cmds, Dictionary<string, string> parameters)
        {
            const string accNameParam = "accountname";
            const string accKeyParam = "accountkey";

            String requiredFields = "Please supply the \"" + accNameParam + "\" and (if not present in configuration file) \"" + accKeyParam + "\" parameters";

            var unknownParams = ParserHelper.Filter(parameters.Keys.Select(o => o.ToString()), "sqlconnstr", accNameParam, accKeyParam);
            if (unknownParams.Count() > 0)
            {
                WriteError(String.Format(unknownParamsFormat, String.Join(", ", unknownParams)));
                return;
            }

            if (cmds.Count == 1 && (cmds[0] == "?" || cmds[0] == "help"))
            {
                WriteHelp(welcomeHelp);
                return;
            }

            string sqlConnectionString = null;
            if (parameters.ContainsKey("sqlconnstr"))
                sqlConnectionString = parameters["sqlconnstr"];

            if (parameters.ContainsKey(accNameParam) && !parameters.ContainsKey(accKeyParam) && Settings.Default.Accounts.ContainsKey(parameters[accNameParam]))
                parameters.Add(accKeyParam, Settings.Default.Accounts[parameters[accNameParam]]);

            if (parameters.ContainsKey(accNameParam) && parameters.ContainsKey(accKeyParam))
            {
                try
                {
                    string storageConnStr = string.IsNullOrEmpty(parameters[accNameParam])
                        ? "UseDevelopmentStorage=true"
                        : string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", parameters[accNameParam], parameters[accKeyParam]);
                    CloudStorageAccount csa = CloudStorageAccount.Parse(storageConnStr);
                    try
                    {
                        if (string.IsNullOrEmpty(sqlConnectionString))
                            sqlConnectionString = FetchConfigurator.ExtractSqlConnectionString(storageConnStr);
                        commands = new FetchConfigurator(storageConnStr, sqlConnectionString, true);
                    }
                    catch (InvalidOperationException)
                    {
                        WriteError(String.Format("Supplied azure storage account doesn't contain saved FetchClimate Configuration DB connection string. Please supply \"SqlConnStr\" parameter as well."));
                    }

                }
                catch (Exception ex)
                {
                    WriteError(ex.ToString());
                }
            }
            else
            {
                WriteError(requiredFields);
                WriteHelp(useHelp);
            }
        }

        private void Use(List<string> cmds, Dictionary<string, string> parameters)
        {
            if (cmds.Count >= 1)
            {
                switch (cmds[0].ToLower())
                {
                    case "local":
                    case "l":
                        UseLocal(cmds.Skip(1).ToList());
                        break;

                    case "cloud":
                    case "c":
                        UseCloud(cmds.Skip(1).ToList(), parameters);
                        break;

                    case "help":
                    case "?":
                    default: Help("use"); break;
                }

                if (commands != null)
                {
                    bool isSqlAvailable = commands.IsSqlServerAvailable;
                    bool doesDbExists = commands.DoesDataBaseExist;
                    bool doesDbSchemaDeployed = commands.IsDataBaseDeployed;
                    bool isInitialized = false;
                    if (isSqlAvailable && doesDbExists && doesDbSchemaDeployed) //there is no registered fetch engines, thus nobody has initialized the configuration yet
                        isInitialized = commands.Db.FetchEngineHistories.Count() > 0;

                    if (!isInitialized)
                    {
                        if (!isSqlAvailable)
                        {
                            WriteError("Specified SQL server can't be reached ({0})", commands.Db.Connection.ConnectionString);
                            return;
                        }

                        if (Confirm("It seems you are configuring specified FetchClimate deployment the first time.\nThe program now will prepare the configuration for the first use.\nContinue? [y/n]"))
                        {
                            if (!doesDbExists)
                                commands.CreateDatabase();
                            if (!doesDbSchemaDeployed)
                            {
                                string sqlText;
                                using (StreamReader reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Microsoft.Research.Science.FetchClimate2.Database.FetchConfigurationDB_Create.sql")))
                                {
                                    sqlText = reader.ReadToEnd();
                                }
                                commands.ExecuteSqlScript(sqlText);
                            }
                            commands.Reset();
                        }
                    }
                }
            }
            else
            {
                Help("use");
            }
        }
        #endregion

        #region File
        /// <summary>
        /// Run batch commands
        /// </summary>
        /// <param name="cmds"></param>
        private void ExecuteFile(List<string> cmds)
        {
            string invalidParams = "Only one file is supported";
            if (cmds.Count == 0 || cmds.Count > 1)
            {
                WriteError(invalidParams);
                return;
            }

            string[] lines;
            try
            {
                lines = File.ReadAllLines(cmds[0]);
            }
            catch (Exception exc)
            {
                WriteError("Error reading file {0}: {1}", cmds[0], exc.Message);
                return;
            }

            try
            {
                isBatchMode = true;
                foreach (var l in lines)
                {
                    Console.WriteLine("FC2>{0}", l);
                    if (!Parse(l))
                        break;
                }
            }
            finally
            {
                isBatchMode = false;
            }
        }
        #endregion

        #region Debug
        private void PrintWords(List<string> words)
        {
#if DEBUG
            for (int i = 0; i < words.Count; i++)
            {
                Console.WriteLine(String.Format("[{0}] - {1}", i, words[i]));
            }
#endif
        }
        #endregion

        /// <summary>
        /// Command reflection executer
        /// </summary>
        /// <param name="cmds">Commands list</param>
        /// <param name="parameters">Parameters list</param>
        /// <param name="addMapping">New mappings list</param>
        /// <param name="removeMapping">Mappings to remove list</param>
        /// <returns>Always true</returns>
        public bool ReflectionExecute(List<String> cmds, Dictionary<string, string> parameters, List<DataSourceMapping> addMapping, List<String> removeMapping)
        {
            var mi = commands.GetType().GetMethods()
                .Where(m => m.GetCustomAttributes(true)
                .Any(a => a is CmdAttr && ((CmdAttr)a).EqualsTo(cmds)))
                .FirstOrDefault();
            if (mi == null)
                WriteError("Unknown command. Type <help>.");
            else
            {
                List<String> requiredParams = new List<string>();
                List<Object> methodParams = new List<object>();
                foreach (var p in mi.GetParameters())
                {
                    //If mapping parameters add it separately
                    switch (p.Name)
                    {
                        case "addMapping":
                            methodParams.Add(addMapping);
                            continue;
                        case "removeMapping":
                            methodParams.Add(removeMapping);
                            continue;
                    }

                    //Check for parameter presents
                    if (parameters.ContainsKey(p.Name)
                        && !string.IsNullOrEmpty(parameters[p.Name]))
                    {
                        //String parsing
                        if (p.ParameterType == typeof(String))
                        {
                            if (p.Name == "uri")
                            {
                                methodParams.Add(ParserHelper.AppendAzureUriWithAccountKey(parameters[p.Name]));
                            }
                            else
                            {
                                methodParams.Add(parameters[p.Name]);
                            }
                        }
                        //DateTime parsing
                        else if (p.ParameterType == typeof(DateTime?) || p.ParameterType == typeof(DateTime))
                        {
                            DateTime dt;
                            if (DateTime.TryParse(parameters[p.Name], out dt))
                                methodParams.Add(dt);
                            else
                                requiredParams.Add(p.Name);
                        }
                        //Int parsing
                        else if (p.ParameterType == typeof(int?) || p.ParameterType == typeof(int))
                        {
                            int num;
                            if (int.TryParse(parameters[p.Name], out num))
                                methodParams.Add(num);
                            else
                                requiredParams.Add(p.Name);
                        }
                        //Uint parsing
                        else if (p.ParameterType == typeof(uint?) || p.ParameterType == typeof(uint))
                        {
                            //ToDo: check this code
                            uint num;
                            if (uint.TryParse(parameters[p.Name], out num))
                                methodParams.Add(num);
                            else
                                requiredParams.Add(p.Name);
                        }
                        //Bool parsing
                        if (p.ParameterType == typeof(bool?))
                        {
                            bool b;
                            if (bool.TryParse(parameters[p.Name], out b))
                                methodParams.Add(b);
                            else
                                requiredParams.Add(p.Name);
                        }

                    }
                    //If parameter is not present and it is required
                    else if (!parameters.ContainsKey(p.Name) && p.IsOptional == false)
                    {
                        requiredParams.Add(p.Name);
                    }
                    //If parameter is options then add default value
                    else
                    {
                        if (p.HasDefaultValue)
                            methodParams.Add(p.DefaultValue);
                        else
                            requiredParams.Add(p.Name);
                    }
                }

                //Required parameters not found
                if (requiredParams.Count() > 0)
                {
                    WriteError("You didn't specify the following parameters: {0}. Please specify them", String.Join(", ", requiredParams));
                    return true;
                }
                try
                {
                    var result = mi.Invoke(commands, methodParams.ToArray());
                    var en = result as IEnumerable;
                    if (en != null)
                    {
                        foreach (var obj in en)
                            Console.WriteLine(obj);
                    }
                }
                catch (Exception ex)
                {
                    WriteError(
                        "Error: {0}",
                        ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                }
            }
            return true;
        }
    }

    static class ParserHelper
    {
        public static IEnumerable<T> Filter<T>(this IEnumerable<T> sequence, params T[] items)
        {
            return sequence.Where(elt => items.All(i => !i.Equals(elt)));
        }

        public static string AppendAzureUriWithAccountKey(string uri)
        {
            if (DataSetUri.IsDataSetUri(uri))
            {
                try
                {
                    var dsuri = new DataSetUri(uri);
                    if (dsuri.ProviderName == "az" && dsuri.ContainsParameter("AccountName"))
                    {
                        if (!dsuri.ContainsParameter("AccountKey"))
                        {
                            var name = dsuri.GetParameterValue("AccountName");
                            if (Settings.Default.Accounts.ContainsKey(name))
                            {
                                uri += "&AccountKey=" + Settings.Default.Accounts[name];
                            }
                            else
                                throw new Exception("Configuration file does not contain key for given account.");
                        }
                        if (!dsuri.ContainsParameter("DefaultEndpointsProtocol"))
                        {
                            uri += "&DefaultEndpointsProtocol=http";
                        }
                        return uri;
                    }
                    else
                        return uri;
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to process uri \"" + uri + "\". Reason: " + ex.Message);
                }
            }
            else
                return uri;
        }

        public static string AppendDataSetUriWithDimensions(string uri)
        {
            if (DataSetUri.IsDataSetUri(uri))
            {
                try
                {
                    var dsuri = new DataSetUri(uri);
                    if (!dsuri.ContainsParameter("dimensions"))
                    {
                        var ds = Microsoft.Research.Science.Data.DataSet.Open(dsuri);
                        var newDims = ds.Dimensions;
                        string newDimString = String.Join(",", newDims.Select(d => d.Name + ":" + d.Length.ToString()));
                        ds.Dispose();
                        return uri + "&dimensions=" + newDimString;
                    }
                    else
                        return uri;
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to process uri \"" + uri + "\". Reason: " + ex.Message);
                }
            }
            else
                return uri;
        }
    }

}
