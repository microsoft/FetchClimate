using Microsoft.Research.Science.FetchClimate2;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
//using fc1 = Microsoft.Research.Science.FetchClimate;

namespace Microsoft.Research.Science.Data
{
    /// <summary>Provides static methods with core FetchClimate2 API</summary>
    public static partial class ClimateService
    {
        private static bool isInProcessMode = false;
        private static string serviceUrl = "http://fetchclimate2.cloudapp.net";

        static ClimateService()
        {
            try
            {
                string svc = System.Environment.GetEnvironmentVariable("FETCHCLIMATESERVICE");
                if (String.IsNullOrEmpty(svc))
                    svc = Microsoft.Research.Science.FetchClimate2.Properties.Settings.Default.ServiceUrl;
                if(!String.IsNullOrEmpty(svc)) {
                    serviceUrl = svc;
                    isInProcessMode = svc.ToLower() == "(local)";
                }
            }
            catch (Exception exc)
            {
                Trace.WriteLine("ClimateService cannot get default settings: " + exc.Message);
            }
        }
       
        private static IFetchClient instance;
        public static IFetchClient Instance
        {
            get
            {
                if (instance == null)
                {
                    if (IsInProcessMode)
                        //instance = CreateLocalFetchClient();
                        throw new NotImplementedException("local fetches are not migrated yet");
                    else
                        instance = new RemoteFetchClient(new Uri(serviceUrl));
                }
                return instance;
            }
        }

        private static IFetchConfiguration configuration;
        /// <summary>Gets latest FetchClimate configuration (available data sources and environmental variables to fetch)</summary>
        public static IFetchConfiguration Configuration
        {
            get
            {
                if (configuration == null)
                    configuration = Instance.GetConfiguration(DateTime.MaxValue);
                return configuration;
            }
        }

        /// <summary>Gets FetchClimate configuration (available data sources and environmental variables to fetch)
        /// for the specified timestamp</summary>
        /// <param name="timestamp">Timestamp in UTC</param>
        public static IFetchConfiguration GetConfiguration(DateTime timestamp)
        {
            return Instance.GetConfiguration(timestamp);
        }

        /// <summary>Gets or sets Boolean value indicating if FetchClimate is running in-process mode. Default value is false. Default value can be overridden 
        /// by setting environmental variable FETCHCLIMATESERVICE to "(local") or in configuration file by setting ServiceUrl parameter to "(local)".
        /// </summary>
        /// </summary>
        public static bool IsInProcessMode
        {
            get
            {
                return isInProcessMode;
            }
            set
            {
                isInProcessMode = value;
            }
        }

        /// <summary>
        /// Gets or sets URL of the service to send requests (ignored in in-process mode). Default value is "fetchclimate.cloudapp.net". Default value can be overridden 
        /// by setting environmental variable FETCHCLIMATESERVICE or in configuration file.
        /// </summary>
        public static string ServiceUrl
        {
            get
            {
                return serviceUrl;
            }
            set
            {
                serviceUrl = value;
            }
        }

        /// <summary>Performs request for climate parameter</summary>
        /// <param name="request">Request to perform</param>
        /// <param name="progressReport">Handler to call periodically with information about request</param>
        /// <param name="cacheFileName">Optional file name with Dmitrov dataset (CSV or NetCDF) to cache results. If specified file exists DataSet from it is returned and
        /// no actual request is made</param>
        /// <returns>Task that results in DataSet with climate parameter values, optional uncertainty and provenance information</returns>
        public static Task<DataSet> FetchAsync(IFetchRequest request, Action<FetchStatus> progressReport = null, string cacheFileName = null)
        {
            if (String.IsNullOrEmpty(cacheFileName))
                return Instance.FetchAsync(request, progressReport);
            else
            {
                if(File.Exists(cacheFileName))
                    try
                    {
                        return Task.FromResult<DataSet>(DataSet.Open(cacheFileName)); // Try to open cached data set
                    }
                    catch(Exception exc)
                    {
                        Trace.WriteLine(String.Format("Cached response is not found at {0}. Performing request...", cacheFileName));
                    }

                return Instance.FetchAsync(request, progressReport).ContinueWith<DataSet>(t => {
                    var result = t.Result;
                    try
                    {
                        result = result.Clone(cacheFileName);
                    }
                    catch(Exception exc2)
                    {
                        Trace.WriteLine(String.Format("Error writing cached response to {0}: {1}", cacheFileName, exc2.Message));
                    }
                    return result;
                }, TaskContinuationOptions.OnlyOnRanToCompletion);
            }
        }

        //private static IFetchClient CreateLocalFetchClient()
        //{
        //    return new LocalFetch();
        //}
    }
}