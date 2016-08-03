using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    public struct GeoPointWithValue2D
    {
        public GeoPointWithValue2D(double lat, double lon, double value)
        {
            this.Latitude = lat;
            this.Longitude = lon;
            this.Value = value;
        }

        public readonly double Latitude;
        public readonly double Longitude;
        public readonly double Value;

        public override int GetHashCode()
        {
            return (int)(Latitude.GetHashCode() ^ Longitude.GetHashCode() ^ Value.GetHashCode());
        }
    }

    /// <summary>
    /// A class that serves the time series data sets of fixed points in space
    /// </summary>
    /// <typeparam name="ScatteredPointsIntegrator"></typeparam>
    /// <typeparam name="TimeAxisIntegrator"></typeparam>
    public abstract class ScatteredPointsDataHandler : MultidimArraysDataHandler
    {        
        protected ISpatPointsInterpolator2D spatialIntegrator = null;
        protected IScatteredObservationsProvider observationProvider = null;

        public ScatteredPointsDataHandler(IStorageContext context, bool performCheckForMissingValues, ITimeAxisIntegrator timeAxisIntegrator, IScatteredObservationsProvider observationProvider, ISpatPointsInterpolator2D pointsIntegrator, string latArrayName = null, string lonArrayName = null)
            : base(context, performCheckForMissingValues, timeAxisIntegrator, latArrayName, lonArrayName)
        {
            this.spatialIntegrator = pointsIntegrator;
            this.observationProvider = observationProvider;                        
        }

        protected virtual async Task<double> CalculateCellMean(IRequestContext requestContext, ComputationalContext computationalContext, double latmin, double latmax, double lonmin, double lonmax, double[] obsLats, double[] obsLons, double[] obsVals)
        {
            if (latmin == latmax && lonmin == lonmax)
                return spatialIntegrator.Interpolate(latmin, lonmin, obsLats, obsLons, obsVals);


            //non-zero area
            var interpolationContext = spatialIntegrator.GetInterpolationContext(obsLats, obsLons, obsVals);
            double latStep = (latmax - latmin) / 19.0;
            double lonStep = (lonmax - lonmin) / 19.0;
            double acc = 0.0;
            double r;
            for (int i = 0; i < 20; i++)
                for (int j = 0; j < 20; j++)
                {
                    r=spatialIntegrator.Interpolate(latmin + latStep * i, lonmin + lonStep * j, interpolationContext);                    
                    acc += r;
                }
            return acc / 400.0;
        }
    }

}
