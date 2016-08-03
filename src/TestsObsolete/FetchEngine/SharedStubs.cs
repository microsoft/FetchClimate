using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Tests.FetchEngine
{
    class StaticValuesForRectData
    {
        protected double lat1, lat2, lon1, lon2;

        public StaticValuesForRectData(double latmin, double latmax, double lonmin, double lonmax)
        {
            lat1 = latmin;
            lat2 = latmax;
            lon1 = lonmin;
            lon2 = lonmax;
        }

        protected bool isPointCoveredByRect(double lat, double lon)
        {
            if (lat < lat1 || lon < lon1 || lat > lat2 || lon > lon2)
                return false;
            return true;
        }

    }

    class StaticValuesForRectDataValueAggregator : StaticValuesForRectData, IValuesAggregator
    {
        double valueIn, valueOut;

        public StaticValuesForRectDataValueAggregator(double latmin, double latmax, double lonmin, double lonmax, double valueIn, double valueOut)
            : base(latmin, latmax, lonmin, lonmax)
        {
            this.valueIn = valueIn;
            this.valueOut = valueOut;
        }

        public async Task<Array> AggregateAsync(IRequestContext context, Array mask = null)
        {
            FetchRequest request = context.Request;

            Array res = Array.CreateInstance(typeof(double), request.Domain.GetDataArrayShape());
            int pointsCount = request.Domain.Lats.Length;
            for (int i = 0; i < pointsCount; i++)
            {
                res.SetValue(isPointCoveredByRect(request.Domain.Lats[i], request.Domain.Lons[i]) ? valueIn : valueOut, i);
            }
            return res;
        }
    }

    class StaticValuesForRectDataUncertaintyEvaluator : StaticValuesForRectData, IUncertaintyEvaluator
    {
        double uncIn, uncOut;

        public StaticValuesForRectDataUncertaintyEvaluator(double latmin, double latmax, double lonmin, double lonmax, double uncIn, double uncOut)
            : base(latmin, latmax, lonmin, lonmax)
        {
            this.uncIn = uncIn;
            this.uncOut = uncOut;
        }


        public async Task<Array> EvaluateAsync(IRequestContext context)
        {
            var request = context.Request;
            Array res = Array.CreateInstance(typeof(double), request.Domain.GetDataArrayShape());
            int pointsCount = request.Domain.Lats.Length;
            for (int i = 0; i < pointsCount; i++)
            {
                res.SetValue(isPointCoveredByRect(request.Domain.Lats[i], request.Domain.Lons[i]) ? uncIn : uncOut, i);
            }
            return res;
        }
    }

    /// <summary>
    /// a handler that returns one uncertainty-value pair for requests with spatial domain covered the rectangle area, and other uncertainty-value pair for all other requests
    /// </summary>
    abstract class StaticValuesForRectDataHandler : DataHandlerFacade
    {
        public StaticValuesForRectDataHandler(double latmin, double latmax, double lonmin, double lonmax, double valueIn, double valueOut, double uncIn, double uncOut)
            : base(null,
            new StaticValuesForRectDataUncertaintyEvaluator(latmin, latmax, lonmin, lonmax,uncIn,uncOut),
            new StaticValuesForRectDataValueAggregator(latmin, latmax, lonmin, lonmax,valueIn,valueOut))
            { }

    }


    class Static_20_40_Handler : StaticValuesForRectDataHandler
    {
        public Static_20_40_Handler()
            : base(-10, 10, 20, 40, -50, -150, 50, 150)
        { }
    }

    class Static_25_45_Handler : StaticValuesForRectDataHandler
    {
        public Static_25_45_Handler()
            : base(-10, 10, 25, 45, -60, -160, 60, 160)
        { }
    }

    class Static_30_50_Handler : StaticValuesForRectDataHandler
    {
        public Static_30_50_Handler()
            : base(-10, 10, 30, 50, -70, -170, 70, 170)
        { }
    }

    class Static_35_55_Handler : StaticValuesForRectDataHandler
    {
        public Static_35_55_Handler()
            : base(-10, 10, 35, 55, -80, -180, 80, 180)
        { }
    }

    class StaticNanYielding_30_50_Handler : StaticValuesForRectDataHandler
    {
        public StaticNanYielding_30_50_Handler()
            : base(-10, 10, 30, 50, double.NaN, double.NaN, 70, 170)
        {
        }
    }
}
