using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{

    public interface IObservationsInformation
    {
        GeoPointWithValue2D[] Observations { get; }                
    }

    public interface IObsInfoWithDataIndeces : IObservationsInformation
    {
        int[] CorrespondingDataIndeces { get; }
    }

    public class ObservationsInformation : IObservationsInformation
    {
        public ObservationsInformation(GeoPointWithValue2D[] observations)
        {
            this.Observations = observations;
        }

        public GeoPointWithValue2D[] Observations { get; private set; }
    }

    public class ObservationsInformationExt : IObsInfoWithDataIndeces
    {
        public ObservationsInformationExt(GeoPointWithValue2D[] observations, int[] dataIndeces, double areaSize)
        {
            this.Observations = observations;
            this.AreaSize = areaSize;
            this.CorrespondingDataIndeces = dataIndeces;
        }

        public GeoPointWithValue2D[] Observations { get; private set; }
        public int[] CorrespondingDataIndeces { get; private set; }

        /// <summary>
        /// The area size in geo degrees^2 where the neighbor points where searched
        /// </summary>
        public double AreaSize { get; private set; }
    }

    public interface IScatteredObservationsProvider {
        double[] StationsLats { get; }
        double[] StationsLons { get; }

        ITimeAxisIntegrator TimeIntegrator { get; }

        Task<IObservationsInformation> GetObservationsAsync(IStorageContext context, string variableName, object missingValue, double latmin, double latmax, double lonmin, double lonmax, ITimeSegment timeSegment);
    }    
}
