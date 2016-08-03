using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Integrators.Temporal
{
    public class ContinousYearsAxisIntegrator<WeightsProvider, CoverageEvaluator> : ContinousTimeAxisIntegratorFacade<WeightsProvider, CoverageEvaluator>
       where WeightsProvider : IWeightsProvider, new()
       where CoverageEvaluator : IDataCoverageEvaluator, new()
    {
        int baseYear;

        public ContinousYearsAxisIntegrator(Array offsetsAxis, DateTime baseTime)
            : base(offsetsAxis)
        {
            baseYear = baseTime.Year;
            TimeSpan diff = baseTime - new DateTime(baseYear, 1, 1);
            if (diff.TotalSeconds != 0.0)
                throw new ArgumentException(string.Format("The axis start offset value (basetime) must indicate the begining of some year, but it does not: {0}",baseTime));
        }

        protected override Tuple<double, double> ProjectIntervalToTheAxis(Tuple<DateTime, DateTime> interval)
        {
            if (interval == null)
                return null;
            DateTime start = interval.Item1;
            DateTime end = interval.Item2;
            int startYear = start.Year;
            int endYear = end.Year;
            double startReminder = (start - new DateTime(startYear,1,1)).TotalDays;
            double endReminder = (end - new DateTime(endYear,1,1)).TotalDays;
            int daysInStartYear = DateTime.IsLeapYear(startYear) ? 366 : 365;
            int daysInEndYear = DateTime.IsLeapYear(endYear) ? 366 : 365;
            double startReminderYearFraction = startReminder / daysInStartYear;
            double endReminderYearFraction = endReminder / daysInEndYear;
            return Tuple.Create(startYear-baseYear+startReminderYearFraction,endYear-baseYear+endReminderYearFraction);
        }

        protected override DateTime ProjectAxisValueToDateTime(double value)
        {
            int floored = (int)Math.Floor(value);
            if(value - floored != 0.0)
                throw new ArgumentException(string.Format("Time axis contains non integer offset"));
            return new DateTime(baseYear + floored);
        }

        public static async Task<ContinousYearsAxisIntegrator<WeightsProvider, CoverageEvaluator>> ConstructAsync(IStorageContext context, string offsetsAxis, DateTime baseTime)
        {
            return new ContinousYearsAxisIntegrator<WeightsProvider, CoverageEvaluator>(await context.GetDataAsync(offsetsAxis), baseTime);
        }
    }
}
