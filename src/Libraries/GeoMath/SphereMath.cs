using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Research.Science.FetchClimate2
{
    public static class SphereMath
    {
        /// <summary>
        /// A radius of the sphere. Average Earth radius in km is default. Callculated from the difinition of the nautical mile and it's value in SI
        /// </summary>
        public static double EarthRadius
        {
            get
            {
                return 6366.70702;
            }
        }

        /// <summary>
        /// The implementation of haversine formula of the great-circle distance on the Earth.
        /// R.W. Sinnott, "Virtues of the Haversine", Sky and Telescope, vol. 68, no. 2, 1984, p. 159
        /// </summary>
        /// <param name="lat1">standpoint lat in degrees</param>
        /// <param name="lon1">standpoint lon in degrees</param>
        /// <param name="lat2">forepoint lat in degrees</param>
        /// <param name="lon2">forepoint lon in degrees</param>
        /// <param name="radius">A radius of the sphere. Average Earth radius in km is default. Callculated from the difinition of the nautical mile and it's value in SI</param>
        /// <returns></returns>       
        public static double GetDistance(double lat1, double lon1, double lat2, double lon2, double radius = 6366.70702)
        {
            double lat1r = ToRad(lat1);
            double lat2r = ToRad(lat2);
            double lon1r = ToRad(lon1);
            double lon2r = ToRad(lon2);
            double dlat = lat2r - lat1r;
            double dlon = lon2r - lon1r;

            double halfLatDiff = ToRad((lat2 - lat1) * 0.5);
            double halfLonDiff = ToRad((lon2 - lon1) * 0.5);

            double sinLat = Math.Sin(halfLatDiff);
            double sinLon = Math.Sin(halfLonDiff);

            double sinSqrLats = sinLat * sinLat;
            double sinSqrLons = sinLon * sinLon;


            double underSqrt = sinSqrLats + Math.Cos(lat1r) * Math.Cos(lat2r) * sinSqrLons;
            System.Diagnostics.Debug.Assert(underSqrt > -1e-14  && underSqrt < 1.0+1e-14);
            underSqrt = Math.Max(0.0, Math.Min(1.0, underSqrt));

            return 2 * radius * Math.Asin(Math.Sqrt(underSqrt));
        }

        static double ToRad(double degrees)
        {
            return degrees / 180.0 * Math.PI;
        }

        private static readonly Func<double, double, double, double, double> sphereDistance = (lat1, lon1, lat2, lon2) => GetDistance(lat1, lon1, lat2, lon2);

        /// <summary>
        /// The implementation of haversine formula of the great-circle distance on the Earth.
        /// R.W. Sinnott, "Virtues of the Haversine", Sky and Telescope, vol. 68, no. 2, 1984, p. 159
        /// </summary>
        public static Func<double, double, double, double, double> HaversineDistanceFunc
        {
            get
            {
                return sphereDistance;
            }
        }
    }
}
