using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.ObservationProviders
{
    /// <summary>
    /// A provider that returns the observation for the stations all over the world (all available timeseries)
    /// </summary>
    public class AllStationsOP : ProjectedSpaceOP
    {
        public AllStationsOP(IStorageContext storageContext, ITimeAxisIntegrator timeIntegrator, string LatsAxisName, string LonsAxisName)
            : base(storageContext,timeIntegrator,LatsAxisName,LonsAxisName)
        {

        }

        public async override Task<IObservationsInformation> GetObservationsAsync(IStorageContext context, string variableName, object missingValue, double latmin, double latmax, double lonmin, double lonmax, ITimeSegment timeSegment)
        {
            IndexBoundingBox bounds;
            
            var r1 = TimeIntegrator.GetBoundingBox(timeSegment, out bounds);

            if (r1 == DataCoverageResult.OutOfData)
                return new ObservationsInformation(new GeoPointWithValue2D[0]);
            
            int[] origin = new int[2];
            int[] shape = new int[2];

            int timeDimNum = timeDimNumber[variableName];
            int stationDimNum = stationsDimNumber[variableName];

            int stationsCount = StationsLats.Length;

            //data fetching
            origin[timeDimNum] = bounds.first;
            origin[stationDimNum] = 0;            
            shape[timeDimNum] = bounds.last-bounds.first+1;
            shape[stationDimNum] = stationsCount;
            var dataTask = context.GetDataAsync(variableName, origin, null, shape);

            //integartion points calculation
            IPs timeIntegrationPoints;
            var r2 =  TimeIntegrator.GetTempIPs(timeSegment, out timeIntegrationPoints);
            if (r2 == DataCoverageResult.OutOfData)
                throw new InvalidOperationException("Inconsistent behavior of Time Integrator: Bounding box calculation gave non-outOfData, but weights calculates gave outOfData");

            System.Diagnostics.Debug.Assert(r1 == r2);            
            
            IPs[][] ips = new IPs[stationsCount][];
            for(int i=0;i<stationsCount;i++)
            {
                var curr = new IPs[2];
                ips[i] = curr;
                curr[stationDimNum] = new IPs() { Weights = new double[] { 1.0 }, Indices = new int[] { i }, BoundingIndices = new IndexBoundingBox() { first = i, last = i } };
                curr[timeDimNum]=timeIntegrationPoints;                
            }
                       
            var data = await dataTask;

            var obsResults = Utils.ArrayIntegrator.IntegrateSequenceWithMVs2D(variableName, data, missingValue, origin, ips);

            double scaleFacotor = dataRepresentationDictionary.ScaleFactors[variableName];
            double addOffset = dataRepresentationDictionary.AddOffsets[variableName];

            var scaledResults = obsResults.Select(r => (r.Integral / r.SumOfWeights)*scaleFacotor+addOffset).ToArray();

            GeoPointWithValue2D[] obs = new GeoPointWithValue2D[stationsCount];
            for(int i=0;i<stationsCount;i++)
                obs[i] = new GeoPointWithValue2D(StationsLats[i],StationsLons[i],scaledResults[i]);
            obs = obs.Where(ob => !double.IsNaN(ob.Value)).ToArray();

            return new ObservationsInformation(obs);
        }
    }
}
