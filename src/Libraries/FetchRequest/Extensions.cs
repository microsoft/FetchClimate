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
    public static class FetchDomainExtensions
    {
        /// <summary>
        /// Check for validness of request content. e.g coordinate ranges, ascending of time axes
        /// </summary>
        /// <returns></returns>
        public static bool IsContentValid(this IFetchDomain domain, out string errorMessage)
        {
            StringBuilder sb = new StringBuilder();
            bool valid = true;

            if (domain.Lats == null)
            {
                sb.AppendLine("Latitude bounds are not specified");
                valid = false;
            }

            if (domain.Lons == null)
            {
                sb.AppendLine("Longitude bounds are not specified");
                valid = false;
            }


            for (int i = 0; i < domain.Lats.Length; i++)
            {
                if (domain.Lats[i] > 90.0 || domain.Lats[i] < -90.0)
                {
                    sb.AppendLine("One of the latitudes are out of bounds -90 .. 90");
                    valid = false;
                    break;
                }
            }

            for (int i = 0; i < domain.Lons.Length; i++)
            {
                if (domain.Lons[i] > 360.0 || domain.Lons[i] < -180.0)
                {
                    sb.AppendLine("One of the longitudes are out of bounds -180 .. 360");
                    valid = false;
                    break;
                }
            }

            if (domain.SpatialRegionType == SpatialRegionSpecification.PointGrid || domain.SpatialRegionType == SpatialRegionSpecification.CellGrid)
            {
                for (int i = 0; i < domain.Lats.Length - 1; i++)
                {
                    if (domain.Lats[i] > domain.Lats[i + 1])
                    {
                        sb.AppendLine("Latitude axis doesn't ascend");
                        valid = false;
                        break;
                    }
                }

                for (int i = 0; i < domain.Lons.Length - 1; i++)
                {
                    if (domain.Lons[i] > domain.Lons[i + 1])
                    {
                        sb.AppendLine("Longitude axis doesn't ascend");
                        valid = false;
                        break;
                    }
                }
            }


            if (domain.SpatialRegionType == SpatialRegionSpecification.Cells)
            {
                //checking cells grid
                if (domain.Lats2 != null)
                {
                    for (int i = 0; i < domain.Lats2.Length; i++)
                    {
                        if (domain.Lats2[i] < domain.Lats[i])
                        {
                            sb.AppendLine("Cells upper latitude bounds are lower that lower bounds");
                            valid = false;
                            break;
                        }
                    }
                }
                if (domain.Lons2 != null)
                {
                    for (int i = 0; i < domain.Lons2.Length; i++)
                    {
                        if (domain.Lons2[i] < domain.Lons[i])
                        {
                            sb.AppendLine("Cells upper longitude bounds are lower that lower bounds");
                            valid = false;
                            break;
                        }
                    }
                }
            }

            if (domain.TimeRegion.Days == null)
            {
                sb.AppendLine("Days bounds are not specified");
                valid = false;
            }
            else
            {
                for (int i = 0; i < domain.TimeRegion.Days.Length - 1; i++)
                {
                    if (domain.TimeRegion.Days[i] >= domain.TimeRegion.Days[i + 1])
                    {
                        sb.AppendLine("Days axis doesn't not ascend");
                        valid = false;
                        break;
                    }
                }

                int maxDayAllowed = domain.TimeRegion.IsIntervalsGridDays ? 367 : 366;

                for (int i = 0; i < domain.TimeRegion.Days.Length; i++)
                {
                    if (domain.TimeRegion.Days[i] < 1 || domain.TimeRegion.Days[i] > maxDayAllowed)
                    {
                        sb.AppendLine("One of the days specified is outside 1 .. 366 interval");
                        valid = false;
                        break;
                    }
                }
            }



            if (domain.TimeRegion.Hours == null)
            {
                sb.AppendLine("Hours bounds are not specified");
                valid = false;
            }
            else
            {
                for (int i = 0; i < domain.TimeRegion.Hours.Length - 1; i++)
                {
                    if (domain.TimeRegion.Hours[i] >= domain.TimeRegion.Hours[i + 1])
                    {
                        sb.AppendLine("Hours axis doesn't not ascend");
                        valid = false;
                        break;
                    }
                }
                for (int i = 0; i < domain.TimeRegion.Hours.Length; i++)
                {
                    if (domain.TimeRegion.Hours[i] < 0 || domain.TimeRegion.Hours[i] > 24)
                    {
                        sb.AppendLine("One of the hours specified is outside 0 .. 24 interval");
                        valid = false;
                        break;
                    }
                }
            }

            if (domain.TimeRegion.Years == null)
            {
                sb.AppendLine("Years bounds are not specified");
                valid = false;
            }
            else
            {
                for (int i = 0; i < domain.TimeRegion.Years.Length - 1; i++)
                {
                    if (domain.TimeRegion.Years[i] >= domain.TimeRegion.Years[i + 1])
                    {
                        sb.AppendLine("Years axis doesn't not ascend");
                        valid = false;
                        break;
                    }
                }
            }
            errorMessage = sb.ToString();
            return valid;
        }


        public static int[] GetDataArrayShape(this IFetchDomain domain)
        {
            List<int> timeDimsLengths = new List<int>(3);
            if (domain.TimeRegion.YearsAxisLength > 1)
                timeDimsLengths.Add(domain.TimeRegion.YearsAxisLength);
            if (domain.TimeRegion.DaysAxisLength > 1)
                timeDimsLengths.Add(domain.TimeRegion.DaysAxisLength);
            if (domain.TimeRegion.HoursAxisLength > 1)
                timeDimsLengths.Add(domain.TimeRegion.HoursAxisLength);

            switch (domain.SpatialRegionType)
            {
                case SpatialRegionSpecification.Points:
                case SpatialRegionSpecification.Cells:
                    return new int[1] { domain.Lats.Length }.Concat(timeDimsLengths).ToArray();
                case SpatialRegionSpecification.CellGrid:
                    return new int[2] { domain.Lons.Length - 1, domain.Lats.Length - 1 }.Concat(timeDimsLengths).ToArray();
                case SpatialRegionSpecification.PointGrid:
                    return new int[2] { domain.Lons.Length, domain.Lats.Length }.Concat(timeDimsLengths).ToArray();
                default:
                    throw new Exception("Unknown spatial region type");
            }
        }
    }

    public static class TimeRegionExtensions
    {
        /// <summary>
        /// Embeded continuous time intervals
        /// </summary>
        public struct TimeSegment : ITimeSegment
        {
            public TimeSegment(int firstYear, int lastYear, int firstDay, int lastDay, int startHour, int stopHour)
                : this()
            {
                FirstYear = firstYear;
                LastYear = lastYear;
                FirstDay = firstDay;
                LastDay = lastDay;
                StartHour = startHour;
                StopHour = stopHour;
            }

            /// <summary>
            /// The first year of years enumeration
            /// </summary>
            public int FirstYear { get; private set; }
            /// <summary>
            /// The last year of years enumeration (included bounds)
            /// </summary>
            public int LastYear { get; private set; }

            /// <summary>
            /// The first day of days enumeration over years
            /// </summary>
            public int FirstDay { get; private set; }

            /// <summary>
            /// The last day of days enumeration (included bounds) over years
            /// </summary>
            public int LastDay { get; private set; }

            /// <summary>
            /// The start bound of hours interval inside days
            /// </summary>
            public int StartHour { get; private set; }

            /// <summary>
            /// The end bound of hours interval inside days
            /// </summary>
            public int StopHour { get; private set; }

            public new TimeSegment MemberwiseClone()
            {
                return (TimeSegment)base.MemberwiseClone();
            }
        }

        /// <summary>
        /// Gets the sequence of TimeSegments that are defined by TimeRegion
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<ITimeSegment> GetSegments(this ITimeRegion timeRegion)
        {
            foreach (var years in timeRegion.GetYearsSegments())
                foreach (var days in timeRegion.GetDaysSegments())
                {
                    Tuple<int, int> effectiveDays;
                    if (years.Item1 == years.Item2 && days.Item2 == 365 && DateTime.IsLeapYear(years.Item1))
                        effectiveDays = Tuple.Create(days.Item1, 366);
                    else
                        effectiveDays = days;

                    foreach (var hours in timeRegion.GetHoursSegments())
                        yield return new TimeSegment(years.Item1, years.Item2, effectiveDays.Item1, effectiveDays.Item2, hours.Item1, hours.Item2);
                }
        }

        public static IEnumerable<Tuple<int, int>> GetYearsSegments(this ITimeRegion timeRegion)
        {
            int len = timeRegion.YearsAxisLength;
            if (timeRegion.IsIntervalsGridYears)
                for (int i = 0; i < len; i++)
                    yield return Tuple.Create(timeRegion.Years[i], timeRegion.Years[i + 1] - 1);
            else
                for (int i = 0; i < len; i++)
                    yield return Tuple.Create(timeRegion.Years[i], timeRegion.Years[i]);
        }

        public static IEnumerable<Tuple<int, int>> GetDaysSegments(this ITimeRegion timeRegion)
        {
            int len = timeRegion.DaysAxisLength;
            if (timeRegion.IsIntervalsGridDays)
                for (int i = 0; i < len; i++)
                    yield return Tuple.Create(timeRegion.Days[i], timeRegion.Days[i + 1] - 1);
            else
                for (int i = 0; i < len; i++)
                    yield return Tuple.Create(timeRegion.Days[i], timeRegion.Days[i]);

        }

        public static IEnumerable<Tuple<int, int>> GetHoursSegments(this ITimeRegion timeRegion)
        {
            int len = timeRegion.HoursAxisLength;
            if (timeRegion.IsIntervalsGridHours)
                for (int i = 0; i < len; i++)
                    yield return Tuple.Create(timeRegion.Hours[i], timeRegion.Hours[i + 1]);
            else
                for (int i = 0; i < len; i++)
                    yield return Tuple.Create(timeRegion.Hours[i], timeRegion.Hours[i]);
        }
    }
    /// <summary>
    /// Performs conversions between days of the year and double axis depicting months
    /// </summary>
    public static class DaysOfYearConversions
    {
        public static readonly int[] DaysInMonth = new int[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        public static readonly int[] DaysInMonthLY = new int[] { 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        public static readonly int[] MonthFirstDay = new int[24];
        public static readonly int[] MonthLastDay = new int[24];
        public static readonly int[] MonthFirstDayLY = new int[12];
        public static readonly int[] MonthLastDayLY = new int[12];

        static DaysOfYearConversions()
        {
            for (int i = 0; i < 24; i++) //filling numbers of the first and the last days of the months for 2 years
            {
                if (i == 0)
                    MonthFirstDay[i] = 1;
                else
                    MonthFirstDay[i] = MonthLastDay[i - 1] + 1;
                MonthLastDay[i] = MonthFirstDay[i] + DaysInMonth[i % 12] - 1;
            }
            Debug.Assert(MonthLastDay[11] == 365);
            Debug.Assert(MonthLastDay[23] == 730);

            for (int i = 0; i < 12; i++)
            {
                if (i == 0)
                    MonthFirstDayLY[i] = 1;
                else
                    MonthFirstDayLY[i] = MonthLastDayLY[i - 1] + 1;
                MonthLastDayLY[i] = MonthFirstDayLY[i] + DaysInMonthLY[i] - 1;
            }
            Debug.Assert(MonthLastDayLY[11] == 366);
        }

        /// <summary>
        /// Returns start point at zero-based double months axis
        /// </summary>
        /// <param name="day">1..365(366 in case of leap year)</param>
        /// <param name="isLeapYear"></param>
        /// <returns></returns>
        public static double ProjectFirstDay(int day, bool isLeapYear)
        {
            Debug.Assert(day <= 366); //first day is always in the "first" year, even if the user specified overlap of 2 years

            int[] effectiveMonthFirstDay;
            int[] effectiveDaysInMonth;
            if (isLeapYear)
            {
                effectiveDaysInMonth = DaysInMonthLY;
                effectiveMonthFirstDay = MonthFirstDayLY;
            }
            else
            {
                effectiveDaysInMonth = DaysInMonth;
                effectiveMonthFirstDay = MonthFirstDay;
            }

            int index = Array.BinarySearch(effectiveMonthFirstDay, day);
            if (index >= 0)
            { //exact match
                return (double)index;
            }
            else
            {
                index = ~index;
                Debug.Assert(index < 12);
                Debug.Assert(index > 0);

                int fullCoveredMonthsIndex = index - 1;
                int unexactOffet = day - effectiveMonthFirstDay[fullCoveredMonthsIndex];
                return fullCoveredMonthsIndex + ((double)unexactOffet) / ((double)(effectiveDaysInMonth[fullCoveredMonthsIndex]));
            }

        }

        /// <summary>
        /// Returns end point at zero-based double month axis
        /// </summary>
        /// <param name="day">1..730</param>
        /// <param name="isLeapYear"></param>
        /// <returns></returns>
        public static double ProjectLastDay(int day, bool isLeapYear)
        {
            Debug.Assert(!(isLeapYear && day > 366));

            int[] effectiveMonthFirstDay;
            int[] effectiveMonthLastDay;
            int[] effectiveDaysInMonth;
            if (isLeapYear)
            {
                effectiveDaysInMonth = DaysInMonthLY;
                effectiveMonthFirstDay = MonthFirstDayLY;
                effectiveMonthLastDay = MonthLastDayLY;
            }
            else
            {
                effectiveDaysInMonth = DaysInMonth;
                effectiveMonthFirstDay = MonthFirstDay;
                effectiveMonthLastDay = MonthLastDay;
            }

            int index = Array.BinarySearch(effectiveMonthLastDay, day);
            if (index >= 0)
            {//exact match of month bound
                return index + 1.0;
            }
            else
            {
                index = ~index;
                Debug.Assert(index < 24);
                int monthIndex = index;
                int unexactOffset = day - effectiveMonthFirstDay[monthIndex] + 1;
                return monthIndex + ((double)unexactOffset) / ((double)effectiveDaysInMonth[monthIndex % 12]);
            }
        }

    }

    public static class FetchRequestExtensions
    {

        private static HashAlgorithm sha = null;

        public static string GetSHAHash(this IFetchRequest request)
        {
            lock (typeof(FetchRequestExtensions))
                if (sha == null)
                    sha = new SHA1CryptoServiceProvider();

            MemoryStream memStm = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(memStm))
            {
                writer.Write(request.EnvironmentVariableName);
                if (request.ParticularDataSource != null)
                    foreach (var item in request.ParticularDataSource)
                    {
                        writer.Write(item);
                    }
                else
                    writer.Write("null");
                writer.Write(request.ReproducibilityTimestamp.Ticks);
                foreach (var i in request.Domain.TimeRegion.Days)
                    writer.Write(i);
                foreach (var i in request.Domain.TimeRegion.Hours)
                    writer.Write(i);
                foreach (var i in request.Domain.TimeRegion.Years)
                    writer.Write(i);
                foreach (var x in request.Domain.Lats)
                    writer.Write(x);
                foreach (var x in request.Domain.Lons)
                    writer.Write(x);
                if (request.Domain.Lats2 != null)
                    foreach (var x in request.Domain.Lats2)
                        writer.Write(x);
                if (request.Domain.Lons2 != null)
                    foreach (var x in request.Domain.Lons2)
                        writer.Write(x);
                writer.Write((int)request.Domain.SpatialRegionType);
                if (request.Domain.Mask != null)
                    foreach (bool b in request.Domain.Mask)
                        writer.Write(b);
                writer.Flush();
                memStm.Seek(0, SeekOrigin.Begin);
                lock (typeof(FetchRequestExtensions))
                    return String.Concat(sha.ComputeHash(memStm).Select(b => String.Format("{0:x2}", b)));
            }
        }
    }
    
}
