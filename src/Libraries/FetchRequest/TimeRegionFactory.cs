using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    public static class TimeRegionFactory
    {

        /// <summary>
        /// Fills the dataset with monthly timeseries definition
        /// </summary>
        /// <param name="job"></param>
        /// <param name="firstYear"></param>
        /// <param name="lastYear"></param>
        /// <param name="firstMonth">1..12</param>
        /// <param name="lastMonth">1..12</param>
        /// <param name="startHour"></param>
        /// <param name="stopHour"></param>
        public static ITimeRegion GetMonthlyTimeseries(this ITimeRegion r, int firstMonth = 1, int lastMonth = 12)
        {
            var region = new TimeRegion(r);
            Debug.Assert(firstMonth <= lastMonth);
            bool isOneYear = ((region.Years.Length==1 && !region.IsIntervalsGridYears) || (region.Years.Length==2 && (region.Years[0] == region.Years[region.Years.Length - 1] - 1)));
            bool isLeap = isOneYear && DateTime.IsLeapYear(region.Years[0]);
            int monthsCount = lastMonth - firstMonth + 1;
            int[] firstDays = new int[monthsCount + 1];

            int[] effectiveFirstDays = isLeap ? DaysOfYearConversions.MonthFirstDayLY : DaysOfYearConversions.MonthFirstDay;
            int[] effectiveLastDays = isLeap ? DaysOfYearConversions.MonthLastDayLY : DaysOfYearConversions.MonthLastDay;

            for (int i = 0; i < monthsCount; i++)
            {
                firstDays[i] = effectiveFirstDays[i + firstMonth - 1];
            }
            firstDays[monthsCount] = effectiveLastDays[(monthsCount - 1) + firstMonth - 1] + 1;

            region.Days = firstDays;
            return region;
        }

        /// <summary>
        /// Fills the dataset with seasonly timeseries definition (iterating days withing each year)
        /// </summary>
        /// <param name="job"></param>
        /// <param name="firstYear"></param>
        /// <param name="lastYear"></param>
        /// <param name="firstDay"></param>
        /// <param name="lastDay"></param>
        /// <param name="startHour"></param>
        /// <param name="stopHour"></param>
        /// <param name="stepLen">1..n</param>
        public static ITimeRegion GetSeasonlyTimeseries(this ITimeRegion r, int firstDay = 1, int lastDay = -1, int stepLen = 1, bool isIntervalTimeseries = true)
        {
            TimeRegion region = new TimeRegion(r);
            int firstYear = region.Years[0];
            bool isOneYear = ((region.Years.Length == 1 && !region.IsIntervalsGridYears) || (region.Years.Length == 2 && (region.Years[0] == region.Years[region.Years.Length - 1] - 1)));
            
            if (isIntervalTimeseries && lastDay != -1)
                lastDay++;

            if (lastDay == -1)                
                    lastDay = (isOneYear && DateTime.IsLeapYear(firstYear)) ? 366 : 365;

            if (isIntervalTimeseries)
                lastDay++;

            List<int> firstDaysList = new List<int>();

            int day = firstDay;
            bool overlap = firstDay > lastDay; //crossing new year
            if (overlap)
                lastDay += 365;
            while (day <= lastDay)
            {
                firstDaysList.Add(overlap ? ((day - 1) % 365) + 1 : day);
                day += stepLen;
            }

            region.Days = firstDaysList.ToArray();
            region.IsIntervalsGridDays = isIntervalTimeseries;
            return region;
        }

        /// <summary>
        /// Fills the dataset with daily timeseries definition (iterating hours within each day)
        /// </summary>
        /// <param name="job"></param>
        /// <param name="firstYear"></param>
        /// <param name="lastYear"></param>
        /// <param name="firstDay"></param>
        /// <param name="lastDay"></param>
        /// <param name="startHour"></param>
        /// <param name="stopHour"></param>
        /// <param name="stepLen">0..n</param>
        public static ITimeRegion GetHourlyTimeseries(this ITimeRegion r, int startHour = 0, int stopHour = -1, int stepLen = 1, bool isIntervalTimeseries = false)
        {
            var region = new TimeRegion(r);

            List<int> startHoursList = new List<int>();

            if (stopHour == -1)
                stopHour = isIntervalTimeseries ? 24 : 23;

            int hour = startHour;
            while (hour <= stopHour)
            {
                startHoursList.Add(hour);
                hour += stepLen;
            }
            region.Hours = startHoursList.ToArray();
            region.IsIntervalsGridHours = isIntervalTimeseries;

            return region;
        }

        /// <summary>
        /// Fills the dataset with yearly timeseries definition (iterating years)
        /// </summary>
        /// <param name="job"></param>
        /// <param name="firstYear"></param>
        /// <param name="lastYear"></param>
        /// <param name="firstDay"></param>
        /// <param name="lastDay"></param>
        /// <param name="startHour"></param>
        /// <param name="stopHour"></param>
        /// <param name="stepLen">1..n</param>
        public static ITimeRegion GetYearlyTimeseries(this ITimeRegion r, int firstYear = 1961, int lastYear = 1990, int stepLen = 1, bool isIntervalTimeseries = true)
        {
            var region = new TimeRegion(r);

            if (isIntervalTimeseries)
                lastYear++;
            List<int> firstYearsList = new List<int>();            
            int year = firstYear;
            while (year <= lastYear)
            {
                firstYearsList.Add(year);
                year += stepLen;  
            }
            region.Years = firstYearsList.ToArray();
            region.IsIntervalsGridYears = isIntervalTimeseries;

            return region;
        }
    }
}
