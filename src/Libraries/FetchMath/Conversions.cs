using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    public static class Conversions
    {
        private readonly static DateTime baseDateTime = new DateTime(1700, 1, 1);

        public static double DateTimeToDouble(this DateTime dt)
        {
            return (dt - baseDateTime).TotalMinutes;
        }

        public static DateTime DoubleToDateTime(this double d)
        {
            return baseDateTime + TimeSpan.FromMinutes(d);
        }
    }

}
