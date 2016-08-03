using System;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{   
    /// <summary>Provides all required information for data source handler</summary>
    [Obsolete("Multiple responsibility: request fields and data storage access callback. Refactoring required")]
    public interface IRequestContext : IStorageContext
    {
        /// <summary>Gets request being processed</summary>
        IFetchRequest Request { get; }

        /// <summary>Data handler invokes this method when uncertainties are estimated to pass uncertainty to computational engine</summary>
        /// <param name="uncertainty">Array of double elements with uncertainty for each result cell. Value of <see cref="Double.NaN"/> means no data for the point. Value of <see cref="Double.MaxValue"/> means data exists but uncertainty cannot be evaluated.</param>
        /// <returns>Array of boolean elements for each result cell. False value for the cell indicates that data handler is not required
        /// to compute the value for the cell. This method is asyncronous.</returns>
        Task<Array> GetMaskAsync(Array uncertainty);
        
        /// <summary>Performs FC request</summary>
        /// <param name="requests">Array of requests to perform</param>
        /// <returns>Array of responses in the same order as requests. This method is asynchronous</returns>
        Task<IFetchResponse[]> FetchDataAsync(params IFetchRequest[] requests);        
    }

    /// <summary>Base class for data source handlers</summary>
    public abstract class DataSourceHandler
    {        
        /// <summary>Initializes instance of data source handler. Context provides access to 
        /// attached data set for additional initialization such as loading axis variables and
        /// metadata</summary>
        /// <param name="ctx">Context with access to data set</param>
        public DataSourceHandler(IStorageContext ctx) { /* Nothing to do here */ }        

        /// <summary>Processes one request</summary>
        /// <param name="ctx">Context containing the request, storage information and uncertainty report callback</param>
        /// <remarks>WARNING! The user of this method currently expects that all implementations of the ProcessRequestAsync method
        /// supply uncertainties by calling ctx.GetMaskAsync and uses the mask returned from GetMaskAsync.</remarks>
        /// <returns>Array of computed values for each cell. This method is asynchronous</returns>
        public abstract Task<Array> ProcessRequestAsync(IRequestContext ctx);
    }
}