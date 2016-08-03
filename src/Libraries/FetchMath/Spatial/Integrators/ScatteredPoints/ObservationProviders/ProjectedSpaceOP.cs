using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.ObservationProviders
{    
    /// <summary>
    /// Provides the observations from the storage that contains time series at lat,lon locations.
    /// Parses metadata (e.g. MVs) as well
    /// </summary>
    public abstract class ProjectedSpaceOP : IScatteredObservationsProvider
    {        
        protected DataRepresentationDictionary dataRepresentationDictionary;
        protected MissingValuesDictionary missingValuesDictionary;

        protected ITimeAxisIntegrator timeIntegrator;
        
        /// <summary>
        /// Latitudes of all known stations
        /// </summary>
        public double[] StationsLats
        {
            get { return stationsLats; }
        }

        /// <summary>
        /// Longitudes of all known stations
        /// </summary>
        public double[] StationsLons
        {
            get { return stationsLons; }
        }

        public ITimeAxisIntegrator TimeIntegrator
        {
            get { return timeIntegrator; }
        }

        protected ProjectedSpaceOP(IStorageContext storageContext, ITimeAxisIntegrator timeIntegrator, string LatsAxisName, string LonsAxisName)
        {
            this.timeIntegrator = timeIntegrator;

            

            missingValuesDictionary = new MissingValuesDictionary(storageContext);
            dataRepresentationDictionary = new DataRepresentationDictionary(storageContext);
        }




        public abstract Task<IObservationsInformation> GetObservationsAsync(IStorageContext context, string variableName, object missingValue, double latmin, double latmax, double lonmin, double lonmax, ITimeSegment timeSegment);
    }

}
