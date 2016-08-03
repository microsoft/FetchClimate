using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    public abstract class ContinousTimeAxis
    {
        protected readonly double[] grid;
        public ContinousTimeAxis(Array axis)
        {
            Type dataType = axis.GetType().GetElementType();
            if (dataType == typeof(double))
            {
                grid = (double[])axis;
            }
            else if (dataType == typeof(float))
            {
                grid = ((float[])axis).Select(a => (double)a).ToArray();
            }
            else if (dataType == typeof(Int64))
            {
                grid = ((Int64[])axis).Select(a => (double)a).ToArray();
            }
            else if (dataType == typeof(Int32))
            {
                grid = ((Int32[])axis).Select(a => (double)a).ToArray();
            }
            else if (dataType == typeof(Int16))
            {
                grid = ((Int16[])axis).Select(a => (double)a).ToArray();
            }
            else if (dataType == typeof(sbyte))
            {
                grid = ((sbyte[])axis).Select(a => (double)a).ToArray();
            }
            else if (dataType == typeof(UInt64))
            {
                grid = ((UInt64[])axis).Select(a => (double)a).ToArray();
            }
            else if (dataType == typeof(UInt32))
            {
                grid = ((UInt32[])axis).Select(a => (double)a).ToArray();
            }
            else if (dataType == typeof(UInt16))
            {
                grid = ((UInt16[])axis).Select(a => (double)a).ToArray();
            }
            else if (dataType == typeof(byte))
            {
                grid = ((byte[])axis).Select(a => (double)a).ToArray();
            }
            else if (dataType == typeof(DateTime))
            {
                grid = ((DateTime[])axis).Select(a => a.DateTimeToDouble()).ToArray();
            }
            else
                grid = ((object[])axis).Select(a => (a.GetType() == typeof(DateTime)) ? (((DateTime)a).DateTimeToDouble()) : Convert.ToDouble(a)).ToArray();
        }

        /// <summary>
        /// Returns an array of ordered continuous time intervals depicted by time segment
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        protected static IEnumerable<Tuple<DateTime, DateTime>> GetTimeIntervals(ITimeSegment t)
        {
            int firstDay = t.FirstDay;
            int lastDay = t.LastDay;
            int firstYear = t.FirstYear;
            int lastYear = t.LastYear;
            int startHour = t.StartHour;
            int stopHour = t.StopHour;

            bool isLeap = (lastYear == firstYear && DateTime.IsLeapYear(firstYear));
            bool toTheEndOfTheYear = (isLeap && lastDay == 366) || (!isLeap && lastDay == 365);
            bool fullDay = startHour == 0 && stopHour == 24;



            if (fullDay && firstDay == 1 && toTheEndOfTheYear)
            {
                //whole years
                yield return Tuple.Create(new DateTime(firstYear, 1, 1), new DateTime(lastYear + 1, 1, 1));
                yield break;
            }



            bool isNewYearCrossed = lastDay < firstDay;
            Debug.Assert(lastYear > firstYear || !isNewYearCrossed);

            if (fullDay)
            {
                if (isNewYearCrossed)
                {
                    //we need to iterate one year less, as we iterate new year crossing     
                    for (int year = firstYear; year < lastYear; year++)
                        yield return Tuple.Create(
                        new DateTime(year, 1, 1) + TimeSpan.FromDays(firstDay - 1),
                        new DateTime(year + 1, 1, 1) + TimeSpan.FromDays(lastDay));
                }
                else
                {
                    for (int year = firstYear; year <= lastYear; year++)
                        yield return Tuple.Create(
                        new DateTime(year, 1, 1) + TimeSpan.FromDays(firstDay - 1),
                        new DateTime(year, 1, 1) + TimeSpan.FromDays(lastDay));
                }
            }
            else
            {
                DateTime dayStart; //reusable structures in the loop bodies
                TimeSpan startHourTimeSpan = TimeSpan.FromHours(startHour);
                TimeSpan stopHourTimrSpan = TimeSpan.FromHours(stopHour);

                //part of the day specified
                if (isNewYearCrossed)
                {
                    int overlapedLastDay;
                    //we need to iterate one year less, as we iterate new year crossing     
                    for (int year = firstYear; year < lastYear; year++)
                    {
                        overlapedLastDay = lastDay + (DateTime.IsLeapYear(year) ? 366 : 365);
                        for (int day = firstDay; day <= overlapedLastDay; day++)
                        {
                            dayStart = new DateTime(year, 1, 1) + TimeSpan.FromDays(day - 1);
                            yield return Tuple.Create(
                                dayStart + startHourTimeSpan,
                                dayStart + stopHourTimrSpan);
                        }
                    }
                }
                else
                {
                    for (int year = firstYear; year <= lastYear; year++)
                    {
                        int effectiveLastDay = (DateTime.IsLeapYear(year) && lastDay == 365) ? 366 : lastDay;
                        for (int day = firstDay; day <= effectiveLastDay; day++)
                        {
                            dayStart = new DateTime(year, 1, 1) + TimeSpan.FromDays(day - 1);
                            yield return Tuple.Create(
                            dayStart + startHourTimeSpan,
                            dayStart + stopHourTimrSpan
                            );
                        }
                    }
                }
            }
        }

    }

}
