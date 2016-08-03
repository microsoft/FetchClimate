using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Research.Science.FetchClimate2
{
    public interface ITimeAxisBoundingBoxCalculator
    {
        /// <summary>
        /// Provides the method the get Index Bounding Box (a pair of lowerest and highest indeces) of some time axis for the specified Time Segment
        /// </summary>
        /// <param name="t">A structure that describes the time interval to produce the mean value for</param>        
        /// <returns>bounding box (a pair of lowerest and highest indeces) of some data axis describing the points sequence that is needed to produce the mean value for the specified TimeSegment</returns>
        IndexBoundingBox GetBoundingBox(ITimeSegment t);
    }

    public interface ITimeAxisIntegrationPointsCalculator
    {
        /// <summary>
        /// Provides the method the get IntegrationPoints (element indeces with corresponding weights) for the specified Time Segment
        /// </summary>
        /// <param name="t">A structure that describes the time interval to produce the mean value for</param>        
        /// <returns>IntegrationPoints (element indeces with corresponding weights) that can be used to get mean value for the specified TimeSegment</returns>
        IPs GetTempIPs(ITimeSegment t);
    }

    /// <summary>
    /// 
    /// </summary>
    public interface ITimeAxisIntegrator : ITimeAxisBoundingBoxCalculator, ITimeAxisIntegrationPointsCalculator
    { }
}
