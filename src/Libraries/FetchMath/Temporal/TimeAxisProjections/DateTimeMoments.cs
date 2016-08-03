using Microsoft.Research.Science.FetchClimate2.Integrators.Temporal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.TimeAxisProjections
{
    public class DateTimeMoments : ITimeAxisProjection
    {
        public Tuple<double, double> ProjectIntervalToTheAxis(Tuple<DateTime, DateTime> interval)
        {
            return Tuple.Create(interval.Item1.DateTimeToDouble(), interval.Item2.DateTimeToDouble());
        }

        public DateTime ProjectAxisValueToDateTime(double value)
        {
            return value.DoubleToDateTime();
        }

    }
}
