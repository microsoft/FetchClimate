using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Integrators.Temporal
{
    public class ContinousDaysAxisIntegrator<WeightsProvider, CoverageEvaluator> : ContinousTimeAxisIntegratorFacade<WeightsProvider, CoverageEvaluator>
       where WeightsProvider : IWeightsProvider, new()
        where CoverageEvaluator : IDataCoverageEvaluator, new()
    {
        DateTime baseTime;

        public ContinousDaysAxisIntegrator(Array offsetsAxis, DateTime baseTime)
            : base(offsetsAxis)
        {
            this.baseTime = baseTime;            
        }

        protected override Tuple<double, double> ProjectIntervalToTheAxis(Tuple<DateTime, DateTime> interval)
        {
            if (interval == null)
                return null;
            TimeSpan diff1 = interval.Item1 - baseTime;
            TimeSpan diff2 = interval.Item2 - baseTime;
            return Tuple.Create(diff1.TotalDays, diff2.TotalDays);
        }

        protected override DateTime ProjectAxisValueToDateTime(double value)
        {
            return baseTime + TimeSpan.FromDays(value);
        }

        public static async Task<ContinousDaysAxisIntegrator<WeightsProvider, CoverageEvaluator>> ConstructAsync(IStorageContext context, string offsetsAxis, DateTime baseTime)
        {
            return new ContinousDaysAxisIntegrator<WeightsProvider, CoverageEvaluator>(await context.GetDataAsync(offsetsAxis), baseTime);
        }
    }
}
