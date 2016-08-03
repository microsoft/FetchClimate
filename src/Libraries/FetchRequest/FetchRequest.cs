using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    /// <summary>
    /// Represents the fetch request to the FetchClimate
    /// </summary>
    public class FetchRequest : IFetchRequest
    {
        private readonly string[] useParticularDataSources;
        private readonly DateTime reproducibilityTimeStampUTC;

        private readonly string environmentVariableName;
        private readonly IFetchDomain domain;

        /// <summary>
        /// Constructs the request
        /// </summary>
        /// <param name="environmentVariableName">The name of the variable used in the particular FetchClimate service deployment</param>
        /// <param name="domain">The domain for which the mean value must be fetched</param>        
        /// <param name="specificDataSources">the array of specific data sources identifiers to supply the mean value with. null value symbolizes automatic choice among all available data sources</param>
        public FetchRequest(string environmentVariableName, IFetchDomain domain, string[] specificDataSources = null)
            : this(environmentVariableName, domain, DateTime.MaxValue, specificDataSources)
        { }

        /// <summary>
        /// Constructs the request
        /// </summary>
        /// <param name="environmentVariableName">The name of the variable used in the particular FetchClimate service deployment</param>
        /// <param name="domain">The domain for which the mean value must be fetched</param>
        /// <param name="reproducibilityTimestamp">Timestamp (UTC) at which the configuration must be taken</param>
        /// <param name="specificDataSources">the array of specific data sources identifiers to supply the mean value with. null value symbolizes automatic choice among all available data sources</param>
        public FetchRequest(string environmentVariableName, IFetchDomain domain, DateTime reproducibilityTimestamp, string[] specificDataSources = null)
        {
            this.environmentVariableName = environmentVariableName;
            this.domain = domain;
            this.useParticularDataSources = specificDataSources;
            this.reproducibilityTimeStampUTC = reproducibilityTimestamp;
        }

        /// <summary>
        /// An environmental variable to fetch
        /// </summary>
        public string EnvironmentVariableName
        {
            get { return environmentVariableName; }
        }

        /// <summary>
        /// A spatial-temporal domain for which the mean value must be calculated
        /// </summary>
        public IFetchDomain Domain
        {
            get { return domain; }
        }

        /// <summary>
        /// Gets the particular data sources constraint (the data source names that allowed be used to generate the result with). Null represent absence of data source constraint
        /// </summary>
        public string[] ParticularDataSource
        {
            get
            {
                return useParticularDataSources;
            }
        }

        /// <summary>
        /// Gets the time (UTC) for the effective service configuration
        /// </summary>
        public DateTime ReproducibilityTimestamp
        {
            get
            {
                return reproducibilityTimeStampUTC;
            }
        }
    }

    /// <summary>
    /// Specifies the temporal region to aggregate. It can be either single time segement or a series of segments. Time segment is embedded continuous time intervals
    /// </summary>
    public class TimeRegion : ITimeRegion
    {
        /// <summary>
        /// Constructs "solid" time regions. Without timeseries
        /// </summary>
        /// <param name="firstYear"></param>
        /// <param name="lastYear"></param>
        /// <param name="firstDay"></param>
        /// <param name="lastDay"></param>
        /// <param name="startHour"></param>
        /// <param name="stopHour"></param>
        public TimeRegion(int firstYear = 1961, int lastYear = 1990, int firstDay = 1, int lastDay = -1, int startHour = 0, int stopHour = 24)
        {
            IsIntervalsGridYears = true;
            IsIntervalsGridDays = true;
            IsIntervalsGridHours = true;
            if (lastDay == -1)
                lastDay = (firstYear == lastYear && DateTime.IsLeapYear(firstYear)) ? 366 : 365;
            Debug.Assert(firstYear <= lastYear);
            Debug.Assert(startHour <= stopHour);
            Debug.Assert(!(firstYear == lastYear && firstDay > lastDay));
            Years = new int[] { firstYear, IsIntervalsGridYears ? lastYear + 1 : lastYear };
            Days = new int[] { firstDay, IsIntervalsGridDays ? lastDay + 1 : lastDay };
            Hours = new int[] { startHour, stopHour };            
        }

        public TimeRegion(int[] years, int[] days, int[] hours, bool isIntervalsGridYears = true, bool isIntervalsGridDays = true, bool isIntervalsGridHours = true)
        {
            Years = years;
            Days = days;
            Hours = hours;
            IsIntervalsGridYears = isIntervalsGridYears;
            IsIntervalsGridDays = isIntervalsGridDays;
            IsIntervalsGridHours = isIntervalsGridHours;
        }

        public TimeRegion(ITimeRegion r)
        {
            Years = r.Years;
            Days = r.Days;
            Hours = r.Hours;
            IsIntervalsGridDays = r.IsIntervalsGridDays;
            IsIntervalsGridHours = r.IsIntervalsGridHours;
            IsIntervalsGridYears = r.IsIntervalsGridYears;
        }

        public int[] Years { get; internal set; }
        public int[] Days { get; internal set; }
        public int[] Hours { get; internal set; }

        public bool IsIntervalsGridYears { get; internal set; }
        public bool IsIntervalsGridDays { get; internal set; }
        public bool IsIntervalsGridHours { get; internal set; }

        /// <summary>Returns number of elements along years axis in resulting dataset/summary>
        public int YearsAxisLength
        {
            get
            {
                if (IsIntervalsGridYears)
                    return Years.Length - 1;
                else
                    return Years.Length;
            }
        }

        /// <summary>Returns number of elements along days axis in resulting dataset</summary>
        public int DaysAxisLength
        {
            get
            {
                if (IsIntervalsGridDays)
                    return Days.Length - 1;
                else
                    return Days.Length;
            }
        }

        /// <summary>Returns number of elements along hours axis in result dataset</summary>
        public int HoursAxisLength
        {
            get
            {
                if (IsIntervalsGridHours)
                    return Hours.Length - 1;
                else
                    return Hours.Length;
            }
        }

        /// <summary>Returns true if time region has more that one time slice</summary>
        public bool IsTimeSeries
        {
            get
            {
                return YearsAxisLength > 1 || DaysAxisLength > 1 || HoursAxisLength > 1;
            }
        }

        /// <summary>
        /// The count of segments in timeseries
        /// </summary>
        public int SegmentsCount
        {
            get
            {
                return YearsAxisLength * DaysAxisLength * HoursAxisLength;
            }
        }
    }

    /// <summary>Defines area in space and time with some points masked out</summary>
    public class FetchDomain : IFetchDomain
    {
        double[] lats, lons, lats2, lons2;
        ITimeRegion timeRegion;
        SpatialRegionSpecification spatialType;

        public double[] Lats { get { return lats; } }
        public double[] Lons { get { return lons; } }
        public double[] Lats2 { get { return lats2; } }
        public double[] Lons2 { get { return lons2; } }
        public ITimeRegion TimeRegion { get { return timeRegion; } }
        public SpatialRegionSpecification SpatialRegionType { get { return spatialType; } }
        public Array Mask { get { return mask; } }

        Array mask;

        public FetchDomain(double[] lats, double[] lons, double[] lats2, double[] lons2, ITimeRegion timeRegion, SpatialRegionSpecification spatType, Array mask)
        {
            if (mask != null && mask.GetType().GetElementType() != typeof(bool))
                throw new ArgumentException("Mask array must have bool type");
            this.mask = mask;
            this.lats = lats;
            this.lons = lons;
            this.lats2 = lats2;
            this.lons2 = lons2;
            this.timeRegion = timeRegion;
            this.spatialType = spatType;
        }


        /// <summary>
        /// Produces a domain that depicts the set of arbitrary placed points
        /// </summary>
        /// <param name="lat">Latitude values of the points</param>
        /// <param name="lon">Longitude values of the points</param>
        /// <param name="times">Temporal specification of the domain</param>
        /// <param name="mask">optional mask (calculation of points corrisponding to false mask is not nessesery, this points can have any value).
        /// The dimenstions of mask: points, time</param>
        /// <returns></returns>
        public static IFetchDomain CreatePoints(double[] lat, double[] lon, ITimeRegion times, Array mask = null)
        {
            return new FetchDomain(lat, lon, null, null, times, SpatialRegionSpecification.Points, mask);
        }

        /// <summary>
        /// Produces a domain that depicts the grid composed of zero-area points
        /// </summary>
        /// <param name="lat">Latitude axis of the grid</param>
        /// <param name="lon">Logitude axis of the grid</param>
        /// <param name="times">Temporal specification of the domain</param>
        /// <param name="mask">optional mask (calculation of points corrisponding to false mask is not nessesery, this points can have any value).
        /// The dimenstions of mask: lon,lat, time</param>
        /// <returns></returns>
        public static IFetchDomain CreatePointGrid(double[] lat, double[] lon, ITimeRegion times, Array mask = null)
        {
            return new FetchDomain(lat, lon, null, null, times, SpatialRegionSpecification.PointGrid, mask);
        }

        /// <summary>
        /// Produces a domain that depicts the set of arbitrary placed rectangular cells
        /// </summary>
        /// <param name="latmin">left latitude bounds of the cells</param>
        /// <param name="latmax">right latitude bounds of the cells</param>
        /// <param name="lonmin">bottom latitude bounds of the cells</param>
        /// <param name="lonmax">upper latitude bounds of the cells</param>
        /// <param name="times">Temporal specification of the domain</param>
        /// <param name="mask">optional mask (calculation of points corrisponding to false mask is not nessesery, this points can have any value).
        /// The dimenstions of mask: points, time</param>
        /// <returns></returns>
        public static IFetchDomain CreateCells(double[] latmin, double[] lonmin, double[] latmax, double[] lonmax, ITimeRegion times, bool[,] mask = null)
        {
            return new FetchDomain(latmin, lonmin, latmax, lonmax, times, SpatialRegionSpecification.Cells, mask);
        }


        /// <summary>
        /// Produces a domain that depicts the grid composed of rectangular cells
        /// </summary>
        /// <param name="lat">Latitude axis of the grid</param>
        /// <param name="lon">Logitude axis of the grid</param>
        /// <param name="times">Temporal specification of the domain</param>
        /// <param name="mask">optional mask (calculation of points corrisponding to false mask is not nessesery, this points can have any value).
        /// The dimenstions of mask: lon,lat, time
        /// The length of lat and lon dimentions of mask array is 1 less than original lat and lon axis dimentions</param>
        /// <returns></returns>
        public static IFetchDomain CreateCellGrid(double[] lat, double[] lon, ITimeRegion times, Array mask = null)
        {
            return new FetchDomain(lat, lon, null, null, times, SpatialRegionSpecification.CellGrid, mask);
        }

        
        

    }

    


    
}
