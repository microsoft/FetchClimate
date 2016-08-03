using Microsoft.Research.Science.Data;
using System;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{   
    /// <summary>Provides most low-level interface to FetchClimate system</summary>
    public interface IFetchClient
    {
        /// <summary>Starts fetching process. Returns task that is complete when fetch is complete</summary>
        /// <param name="progressReport">A callback returning progress messages</param>
        /// <returns>Started fetching task that results in new DataSet</returns>
        Task<DataSet> FetchAsync(IFetchRequest request, Action<FetchStatus> progressReport = null);

        /// <summary>Gets FetchClimate configuration (available data sources and environmental variables to fetch) at the specified UTC time</summary>
        IFetchConfiguration GetConfiguration(DateTime utcTime);
    }
}