using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Research.Science.FetchClimate2
{
    public interface IFetchRequest
    {
        /// <summary>
        /// An environmental variable to fetch
        /// </summary>
        string EnvironmentVariableName
        {
            get;
        }

        /// <summary>
        /// A spatial-temporal domain for which the mean value must be calculated
        /// </summary>
        IFetchDomain Domain
        {
            get;
        }

        /// <summary>
        /// Gets the particular data sources constraint (the data source names that allowed be used to generate the result with). Null represent absence of data source constraint
        /// </summary>
        string[] ParticularDataSource
        {
            get;
        }

        /// <summary>
        /// Gets the time (UTC) for the effective service configuration
        /// </summary>
        DateTime ReproducibilityTimestamp
        {
            get;
        }
    }

    public interface IFetchResponse
    {

        Array Values
        {
            get;
        }

        Array Uncertainty
        {
            get;
        }

        IFetchRequest Request
        {
            get;
        }
    }       
}
