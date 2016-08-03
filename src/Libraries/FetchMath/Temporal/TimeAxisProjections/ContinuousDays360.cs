using Microsoft.Research.Science.FetchClimate2.Integrators.Temporal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.TimeAxisProjections
{
    public class ContinuousDays360 : ITimeAxisProjection
    {
        private readonly int baseYear;
        private readonly double baseYearFraction;

        public ContinuousDays360(int baseYear, int baseDay)
        {
            this.baseYearFraction = (double)(baseDay - 1) / 360.0;
            this.baseYear = baseYear;
        }

        public Tuple<double, double> ProjectIntervalToTheAxis(Tuple<DateTime, DateTime> interval)
        {
            if (interval == null)
                return null;

            int leftYear = interval.Item1.Year;
            int leftDay = interval.Item1.DayOfYear;
            double leftYearFraction = DaysOfYearConversions.ProjectFirstDay(leftDay, DateTime.IsLeapYear(leftYear)) / 12.0;

            int rightYear = interval.Item2.Year;
            int rightDay = interval.Item2.DayOfYear;
            if (interval.Item2.DayOfYear == 1 && interval.Item2.TimeOfDay.TotalSeconds < 1.0)//exact new year value. the right bound is previous year
            {
                rightYear--;
                rightDay = DateTime.IsLeapYear(rightYear) ? 366 : 365;
            }
            else if (interval.Item2.TimeOfDay.TotalSeconds < 1.0) //exact midnight value. right day is the day before
                rightDay--;

            double rightYearFraction = DaysOfYearConversions.ProjectLastDay(rightDay, DateTime.IsLeapYear(rightYear)) / 12.0;

            double leftVal = (leftYear + leftYearFraction - baseYear - baseYearFraction) * 360.0;
            double rightVal = (rightYear + rightYearFraction - baseYear - baseYearFraction) * 360.0;


            return Tuple.Create(
                Math.Floor(leftVal),
                Math.Ceiling(rightVal)
                );
        }

        public DateTime ProjectAxisValueToDateTime(double value)
        {
            double yearsVal = value / 360.0;
            int year = (int)(Math.Floor(yearsVal)) + baseYear;
            int daysInYear = DateTime.IsLeapYear(year) ? 366 : 365;
            int day = (int)Math.Round((yearsVal + baseYear - year) * daysInYear);
            return new DateTime(year, 1, 1).AddDays(day);
        }
    }
}
