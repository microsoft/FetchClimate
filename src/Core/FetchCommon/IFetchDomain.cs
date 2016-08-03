using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Research.Science.FetchClimate2
{
    /// <summary>
    /// Specify the kind of spatial coverage that is asked to process
    /// </summary>
    public enum SpatialRegionSpecification
    {
        /// <summary>
        /// A set of arbitrary placed points
        /// </summary>
        Points,
        /// <summary>
        /// A grid that is composed of zero-area points
        /// </summary>
        PointGrid,
        /// <summary>
        /// A set of arbitrary placed rectangular areas
        /// </summary>
        Cells,
        /// <summary>
        /// A grid that is composed of rectangular cells. The bounds of the cells are defined with spatial axis values
        /// </summary>
        CellGrid
    }

    /// <summary>
    /// Specifies the temporal region to aggregate. It can be either single time segement or a series of segments. Time segment is embedded continuous time intervals
    /// </summary>
    public interface ITimeRegion
    {
        int[] Years { get; }
        int[] Days { get; }
        int[] Hours { get; }

        bool IsIntervalsGridYears { get; }
        bool IsIntervalsGridDays { get; }
        bool IsIntervalsGridHours { get; }

        /// <summary>Returns number of elements along years axis in resulting dataset/summary>
        int YearsAxisLength
        {
            get;
        }

        /// <summary>Returns number of elements along days axis in resulting dataset</summary>
        int DaysAxisLength
        {
            get;
        }

        /// <summary>Returns number of elements along hours axis in result dataset</summary>
        int HoursAxisLength
        {
            get;
        }

        /// <summary>Returns true if time region has more that one time slice</summary>
        bool IsTimeSeries
        {
            get;
        }

        /// <summary>
        /// The count of segments in timeseries
        /// </summary>
        int SegmentsCount
        {
            get;
        }

    }

    /// <summary>Defines area in space and time with some points masked out</summary>
    public interface IFetchDomain
    {
        double[] Lats { get; }
        double[] Lons { get; }
        double[] Lats2 { get; }
        double[] Lons2 { get; }
        ITimeRegion TimeRegion { get; }
        SpatialRegionSpecification SpatialRegionType { get; }
        Array Mask { get; }
    }

    /// <summary>
    /// Embeded continuous time intervals which FetchClimate operates with
    /// </summary>
    public interface ITimeSegment
    {
        /// <summary>
        /// The first year of years enumeration
        /// </summary>
        int FirstYear { get; }
        /// <summary>
        /// The last year of years enumeration (included bounds)
        /// </summary>
        int LastYear { get; }

        /// <summary>
        /// The first day of days enumeration over years
        /// </summary>
        int FirstDay { get; }

        /// <summary>
        /// The last day of days enumeration (included bounds) over years
        /// </summary>
        int LastDay { get; }

        /// <summary>
        /// The start bound of hours interval inside days
        /// </summary>
        int StartHour { get;  }

        /// <summary>
        /// The end bound of hours interval inside days
        /// </summary>
        int StopHour { get; }
    }

   
    
}
