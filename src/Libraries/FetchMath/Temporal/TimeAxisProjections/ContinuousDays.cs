using Microsoft.Research.Science.FetchClimate2.Integrators.Temporal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.TimeAxisProjections
{
    public class ContinuousDays : ITimeAxisProjection
    {
        private readonly DateTime baseTime;

        public ContinuousDays(DateTime baseTime)
        {
            this.baseTime = baseTime;
        }

        public Tuple<double, double> ProjectIntervalToTheAxis(Tuple<DateTime, DateTime> interval)
        {
            if (interval == null)
                return null;
            TimeSpan diff1 = interval.Item1 - baseTime;
            TimeSpan diff2 = interval.Item2 - baseTime;
            return Tuple.Create(diff1.TotalDays, diff2.TotalDays);
        }

        public DateTime ProjectAxisValueToDateTime(double value)
        {
            return baseTime + TimeSpan.FromDays(value);
        }
    }
}
