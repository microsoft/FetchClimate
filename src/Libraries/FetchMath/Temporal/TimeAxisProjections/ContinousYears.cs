using Microsoft.Research.Science.FetchClimate2.Integrators.Temporal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.TimeAxisProjections
{
    public class ContinousYears : ITimeAxisProjection
    {
        private readonly int baseYear;

        public ContinousYears(int baseYear)
        {
            this.baseYear = baseYear;
        }

        public Tuple<double, double> ProjectIntervalToTheAxis(Tuple<DateTime, DateTime> interval)
        {
            if (interval == null)
                return null;
            DateTime start = interval.Item1;
            DateTime end = interval.Item2;
            int startYear = start.Year;
            int endYear = end.Year;
            double startReminder = (start - new DateTime(startYear, 1, 1)).TotalDays;
            double endReminder = (end - new DateTime(endYear, 1, 1)).TotalDays;
            int daysInStartYear = DateTime.IsLeapYear(startYear) ? 366 : 365;
            int daysInEndYear = DateTime.IsLeapYear(endYear) ? 366 : 365;
            double startReminderYearFraction = startReminder / daysInStartYear;
            double endReminderYearFraction = endReminder / daysInEndYear;
            return Tuple.Create(startYear - baseYear + startReminderYearFraction, endYear - baseYear + endReminderYearFraction);
        }

        public DateTime ProjectAxisValueToDateTime(double value)
        {
            int floored = (int)Math.Floor(value);
            if (value - floored != 0.0)
                throw new ArgumentException(string.Format("Time axis contains non integer offset"));
            return new DateTime(baseYear + floored);
        }
    }
}
