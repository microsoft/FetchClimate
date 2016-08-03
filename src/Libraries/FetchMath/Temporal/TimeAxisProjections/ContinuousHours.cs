using Microsoft.Research.Science.FetchClimate2.Integrators.Temporal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.TimeAxisProjections
{
    public class ContinuousHours : ITimeAxisProjection
    {
        private readonly DateTime baseTime;

        public ContinuousHours(DateTime baseTime)
        {
            this.baseTime = baseTime;
        }

        public Tuple<double, double> ProjectIntervalToTheAxis(Tuple<DateTime, DateTime> interval)
        {
            if (interval == null)
                return null;
            TimeSpan diff1 = interval.Item1 - baseTime;
            TimeSpan diff2 = interval.Item2 - baseTime;
            return Tuple.Create(diff1.TotalHours, diff2.TotalHours);
        }

        public DateTime ProjectAxisValueToDateTime(double value)
        {
            return baseTime + TimeSpan.FromHours(value);
        }
    }
}
