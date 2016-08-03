using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Research.Science.FetchClimate2
{
    public interface ISpatGridBoundingBoxCalculator
    {
        /// <summary>
        /// How the integrationPoints value returned via out parameter can be used
        /// </summary>
        /// <param name="coord">A point to produce the mean value for</param>        
        /// <returns>bounding box (a pair of lowerest and highest indices) describing the points sequence that is needed to produce the mean value for the specified point</returns>
        IndexBoundingBox GetBoundingBox(double coord);

        /// <summary>
        /// How the integrationPoints value returned via out parameter can be used
        /// </summary>
        /// <param name="min">A start of interval to produce the mean value for</param>
        /// <param name="max">An end of interval to produce the mean value for</param>        
        /// <returns>bounding box (a pair of lowerest and highest indices) describing the points sequence that is needed to produce the mean value for the specified interval</returns>
        IndexBoundingBox GetBoundingBox(double min, double max);
    }

    /// <summary>
    /// Provides the method the get IntegrationPoints (element indices with corresponding weights) for the specified interval
    /// </summary>
    public interface ISpatGridIntegrationPointsCalculator
    {        
        /// <summary>
        /// How the integrationPoints value returned via out parameter can be used
        /// </summary>
        /// <param name="coord">A point to produce the mean value for</param>        
        /// <returns>IntegrationPoints (element indices with corresponding weights) that can be used to get mean value for the specified point</returns>
        IPs GetIPsForPoint(double coord);

        /// <summary>
        /// How the integrationPoints value returned via out parameter can be used
        /// </summary>
        /// <param name="min">A start of interval to produce the mean value for</param>
        /// <param name="max">An end of interval to produce the mean value for</param>       
        /// <returns>IntegrationPoints (element indices with corresponding weights) that can be used to get mean value for the specified interval</returns>
        IPs GetIPsForCell(double min, double max);
    }

    public interface ISpatGridIntegrator : ISpatGridBoundingBoxCalculator, ISpatGridIntegrationPointsCalculator { }
}
