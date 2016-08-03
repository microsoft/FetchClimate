using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Integrators.Temporal
{
    public class ContinousHourAxisIntegrator<WeightsProvider, CoverageEvaluator> : ContinousTimeAxisIntegratorFacade<WeightsProvider, CoverageEvaluator>
    where WeightsProvider : IWeightsProvider, new()
        where CoverageEvaluator : IDataCoverageEvaluator, new()
    {
        DateTime baseTime;                

        public ContinousHourAxisIntegrator(Array offsetsAxis,DateTime baseTime):base(offsetsAxis)
        {
            this.baseTime = baseTime;
            
        }

        protected override Tuple<double, double> ProjectIntervalToTheAxis(Tuple<DateTime, DateTime> interval)
        {
            if (interval == null)
                return null;
            TimeSpan diff1 = interval.Item1 - baseTime;
            TimeSpan diff2 = interval.Item2 - baseTime;
            return Tuple.Create(diff1.TotalHours, diff2.TotalHours);
        }

        protected override DateTime ProjectAxisValueToDateTime(double value)
        {
            return baseTime + TimeSpan.FromHours(value);
        }

        public static async Task<ContinousHourAxisIntegrator<WeightsProvider, CoverageEvaluator>> ConstructAsync(IStorageContext context, string offsetsAxis, DateTime baseTime)
        {
            return new ContinousHourAxisIntegrator<WeightsProvider, CoverageEvaluator>(await context.GetDataAsync(offsetsAxis), baseTime);
        }
    }
}
