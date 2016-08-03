using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    /// <summary>
    /// Class provides a set of functions that evaluate standard deviation of the approximation error of evironmental varaible with constant
    /// </summary>
    public static class PartOfAveragedDayApproximatedWithConstant
    {
        /// <summary>
        /// Degrees C
        /// </summary>
        /// <param name="hours">subperiod length</param>
        /// <returns></returns>
        public static double NearSurfaceTemperatureSd(int hours)
        {
            return Math.Sqrt(Math.Abs(-0.162123875 * (double)hours + 3.890973));
        }

        /// <summary>
        /// mm/month
        /// </summary>
        /// <param name="hours">subperiod length</param>
        /// <returns></returns>
        public static double PrecipitationRateSd(int hours)
        {
            return Math.Sqrt(Math.Abs(-333.68595833333333333333333333333 * (double)hours + 8008.463));
        }
    }

    /// <summary>
    /// Class provides a set of functions that evaluate standard deviation of the approximation error of environmental varaible with constant
    /// </summary>
    public static class PartOfYearAveragedMonthsApproximatedWithConstant
    {
        /// <summary>
        /// Degrees C
        /// </summary>        
        /// <returns></returns>
        public static double NearSurfaceTemperatureSd(int firstDay,int lastDay, bool isLeapYear)
        {
            double start = DaysOfYearConversions.ProjectFirstDay(firstDay, isLeapYear);
            double stop = DaysOfYearConversions.ProjectLastDay(lastDay, isLeapYear);
            double unalignedDays = Math.IEEERemainder(stop - start, 1.0)*30.0;
            if (unalignedDays <= 0.0)
                unalignedDays += 30.0;
            return Math.Sqrt(Math.Exp(-0.0929367 * (double)unalignedDays + 2.788101) - 1) / (Math.Truncate(stop - start) + 1);
        }

        /// <summary>
        /// mm/month
        /// </summary>        
        /// <returns></returns>
        public static double PrecipitationRateSd(int firstDay, int lastDay, bool isLeapYear)
        {
            double start = DaysOfYearConversions.ProjectFirstDay(firstDay, isLeapYear);
            double stop = DaysOfYearConversions.ProjectLastDay(lastDay, isLeapYear);
            double unalignedDays = Math.IEEERemainder(stop - start, 1.0) * 30.0;
            if (unalignedDays <= 0.0)
                unalignedDays += 30.0;
            return Math.Sqrt(Math.Exp(Math.Sqrt(-2.61951724 * unalignedDays + 78.5855172)) - 1) / (Math.Truncate(stop - start) + 1);
        }

        /// <summary>
        /// percents
        /// </summary>        
        /// <returns></returns>
        public static double RelativeHumiditySd(int firstDay, int lastDay, bool isLeapYear)
        {
            double start = DaysOfYearConversions.ProjectFirstDay(firstDay, isLeapYear);
            double stop = DaysOfYearConversions.ProjectLastDay(lastDay, isLeapYear);
            double unalignedDays = Math.IEEERemainder(stop - start, 1.0) * 30.0;
            if (unalignedDays <= 0.0)
                unalignedDays += 30.0;
            return Math.Max(-0.000366274 * unalignedDays * unalignedDays * unalignedDays + 0.022035029 * unalignedDays * unalignedDays - 0.647813200 * unalignedDays + 9.184569663, 0.0) / (Math.Truncate(stop - start) + 1);
        }

        /// <summary>
        /// percents
        /// </summary>        
        /// <returns></returns>
        public static double SunPercentageSd(int firstDay, int lastDay, bool isLeapYear)
        {
            double start = DaysOfYearConversions.ProjectFirstDay(firstDay, isLeapYear);
            double stop = DaysOfYearConversions.ProjectLastDay(lastDay, isLeapYear);
            double unalignedDays = Math.IEEERemainder(stop - start, 1.0) * 30.0;
            if (unalignedDays <= 0.0)
                unalignedDays += 30.0;
            return Math.Max(5.699910e-09 * Math.Pow(unalignedDays, 7) - 3.555722e-07 * Math.Pow(unalignedDays, 6) + 7.900013e-06 * Math.Pow(unalignedDays, 5) - 1.357565e-04 * Math.Pow(unalignedDays, 4) + 2.675668e-03 * Math.Pow(unalignedDays, 3) - 4.403412e-03 * unalignedDays * unalignedDays - 1.269137e+00 * unalignedDays + 2.303582e+01, 0.0) / (Math.Truncate(stop - start) + 1);
        }

        /// <summary>
        /// meter/s
        /// </summary>        
        /// <returns></returns>
        public static double WindSpeedSd(int firstDay, int lastDay, bool isLeapYear)
        {
            double start = DaysOfYearConversions.ProjectFirstDay(firstDay, isLeapYear);
            double stop = DaysOfYearConversions.ProjectLastDay(lastDay, isLeapYear);
            double unalignedDays = Math.IEEERemainder(stop - start, 1.0) * 30.0;
            if (unalignedDays <= 0.0)
                unalignedDays += 30.0;
            return Math.Max(7.366328e-10 * Math.Pow(unalignedDays, 7) - 4.211287e-08 * Math.Pow(unalignedDays, 6) + 3.851063e-07 * Math.Pow(unalignedDays, 5) + 1.238918e-05 * Math.Pow(unalignedDays, 4) - 1.343913e-04 * Math.Pow(unalignedDays, 3) - 1.134482e-03 * unalignedDays * unalignedDays - 5.671852e-02 * unalignedDays + 1.619290e+00, 0.0) / (Math.Truncate(stop - start) + 1);
        }
    }

    /// <summary>
    /// If the varaibles are considered to be equal to their average values all over the world (values derived from GHCN, CRU CL 2.0, ETOPO1)
    /// </summary>
    public static class UnknownVaraibleValues
    {
        public const double AirTemperatureSD = 11.70108;//cels
        public const double PrecipitationRateSD = 118.4324; //mm/month
        public const double SoilMoistureSD = 11.84844;//volumic percents
        public const double AirRelativeHumiditySD = 17.68075;//percents
        public const double SurfaceElevationSD = 2650.385;//meters
        public const double WindSpeedSD = 1.309713;//meters/sec
        public const double DiurnalTempRangeSD = 3.324544;//cels
        public const double WetDaysFreqSD = 5.997462;//days/month
        public const double FrostDaysFreqSD = 12.71484;//days/month
        public const double SunshineFractionSD = 20.61431;//percents

        public const double AirTemperatureMean = 12.47208;//cels
        public const double PrecipitationRateMean = 1145.241; //mm/month
        public const double SoilMoistureMean = 21.35137;//volumic percents
        public const double AirRelativeHumidityMean = 67.92355;//percents
        public const double SurfaceElevationMean = -1893.065;//meters
        public const double WindSpeedMean = 3.233378;//meters/sec
        public const double DiurnalTempRangeMean = 11.11124;//cels
        public const double WetDaysFreqMean = 9.744157;//days/month
        public const double FrostDaysFreqMean = 11.42685;//days/month
        public const double SunshineFractionMean = 50.57731;//percents
    }
}
