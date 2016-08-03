using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    /// <summary>
    /// Specifies how the requested interval can be handled
    /// </summary>
    public enum DataCoverageResult
    {
        /// <summary>
        /// Neither mean value nor uncertainty can be calculated for the interval
        /// </summary>
        OutOfData,
        /// <summary>
        /// The mean value and its uncertainty can be calculated for the requested interval
        /// </summary>
        DataWithUncertainty,
        /// <summary>
        /// Mean value can be calculated, but there is insufficient data to produce its uncertainty
        /// </summary>        
        DataWithoutUncertainty
    }
}
