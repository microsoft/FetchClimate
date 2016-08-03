using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    public interface ITimeAxisProjection
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="interval">supposed to be returned by GetTimeIntervals method</param>
        /// <returns></returns>
        Tuple<double, double> ProjectIntervalToTheAxis(Tuple<DateTime, DateTime> interval);

        DateTime ProjectAxisValueToDateTime(double value);
    }
}
