using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.DataHandlers
{    
    /// <summary>
    /// Build a dalanay triangulation aprroximating Earth as sphere. Perfoming NNI 
    /// </summary>
    public abstract class NaturalNeigbourInterpolationOnSphereDataHandler : ScatteredPointsDataHandler        
    {
        /// <summary>
        /// The emelent count of the grid that is used for discrete approximation of mean value insize the cell
        /// </summary>
        const int cellAveragingGridSize = 4;

        private readonly ISpatPointsLinearInterpolator2D nni;

        public NaturalNeigbourInterpolationOnSphereDataHandler(IStorageContext context, bool performCheckForMissingValues, ITimeAxisIntegrator timeAxisIntegrator, IScatteredObservationsProvider observationProvider,ISpatPointsLinearInterpolator2D nni, string latArrayName = null, string lonArrayName = null)
            : base(context, performCheckForMissingValues, timeAxisIntegrator,observationProvider, nni, latArrayName, lonArrayName)
        {
            this.nni = nni;
        }

        /// <summary>
        /// Computes delanay triangulation for each unique time segment that is present in the cells enumeration. Saves them into comp context
        /// </summary>
        /// <param name="context"></param>
        /// <param name="computationalContext"></param>
        /// <param name="cells"></param>
        /// <returns></returns>
        protected async Task SaveObservationsAndDalanayDiagsForCells(IRequestContext context, ComputationalContext computationalContext, IEnumerable<GeoCellTuple> cells)
        {
            var variable = context.Request.EnvironmentVariableName;            

            var cellsArray = cells.ToArray();

            var numberedCells = Enumerable.Zip(cellsArray, Enumerable.Range(0, int.MaxValue), (cell, num) => Tuple.Create(cell, num)).ToArray();

            var missingValue = MissingValuesDictionary[variable];

            int count = numberedCells.Length;
            Dictionary<ITimeSegment, IObservationsInformation> observations = new Dictionary<ITimeSegment, IObservationsInformation>();
            Dictionary<ITimeSegment, object> delanays = new Dictionary<ITimeSegment, object>();

            var timeGroups = numberedCells.GroupBy(cell => cell.Item1.Time).ToArray();
            
            //fetching observations for different timeSegments, constructing delanay triangulations for them. storing them into the comp context
            TraceVerbose("Fetching observations for different time segments, building delanays in parallel");
            var timeSegmentGroupedTasks = timeGroups.Select(group =>
            Task.Run(async () =>
                {
                var timeSegment = group.Key;

                int groupHashCode = group.GetHashCode();

                TraceVerbose("Getting observations for time segment {0}", groupHashCode);
                System.Diagnostics.Stopwatch sw1 = System.Diagnostics.Stopwatch.StartNew();
                var observation = await observationProvider.GetObservationsAsync(context, variable, missingValue, 0.0, 0.0, 0.0, 0.0, timeSegment);
                sw1.Stop();
                TraceVerbose("Got observations for time segment {0} in {1}", groupHashCode, sw1.Elapsed);

                double[] lats = observation.Observations.Select(o => o.Latitude).ToArray();
                double[] lons = observation.Observations.Select(o => o.Longitude).ToArray();
                double[] vals = observation.Observations.Select(o => o.Value).ToArray();

                TraceVerbose("Generating dalanay for time segment {0} ({1} observations)", groupHashCode, observation.Observations.Length);
                System.Diagnostics.Stopwatch sw2 = System.Diagnostics.Stopwatch.StartNew();
                var dalanay = nni.GetInterpolationContext(lats, lons, vals);
                sw2.Stop();
                TraceVerbose("Generated dalanay for time segment {0} ({1} observations) in {2}", groupHashCode, observation.Observations.Length, sw2.Elapsed);


                observations.Add(timeSegment,observation);
                delanays.Add(timeSegment,dalanay);
                }));

            var syncTask = Task.WhenAll(timeSegmentGroupedTasks);

            await syncTask;
            TraceVerbose("Fetched observations for different time segments. Delanay triangulations are computed");
            computationalContext["observations"] = observations;
            computationalContext["dalanays"] = delanays;
            computationalContext["requestCells"] = cellsArray;
        }


        /// <summary>
        /// Returns a set of linear weights along with timeSegmint for with dataIndeces in linear weights are valid 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="computationalContext"></param>
        /// <param name="cells"></param>
        /// <returns></returns>
        protected IEnumerable<Tuple<ITimeSegment,LinearWeight[]>> CalcLinearWeights(ComputationalContext computationalContext, IEnumerable<GeoCellTuple> cells)
        {
            if (!computationalContext.ContainsKey("observations"))
                throw new InvalidOperationException("Call SaveObservationsAndDalanayDiagsForCells prior calling CompleteAggregateCellsBatch");

            ISpatPointsLinearInterpolator2D spli2d = (ISpatPointsLinearInterpolator2D)spatialIntegrator;

            Dictionary<ITimeSegment, IObservationsInformation> observations = (Dictionary<ITimeSegment, IObservationsInformation>)computationalContext["observations"];
            Dictionary<ITimeSegment, object> delanays = (Dictionary<ITimeSegment, object>)computationalContext["dalanays"];                        

            var sw1 = System.Diagnostics.Stopwatch.StartNew();
            TraceVerbose("Computing field values");
            
            //WARNING: can't be paralleled as dalanays are not thread safe
            foreach (var cell in cells)
            {
                var timeSegment = cell.Time;
                var observation = observations[timeSegment].Observations;
                object delanay = delanays[timeSegment];                

                double latmax = cell.LatMax, latmin = cell.LatMin, lonmax = cell.LonMax, lonmin = cell.LonMin;

                if (latmax == latmin && lonmax == lonmin)
                    yield return Tuple.Create(timeSegment,spli2d.GetLinearWeigths(latmin, lonmin, delanay));
                else
                {                    
                    var sizeSqrt = (int)Math.Sqrt(cellAveragingGridSize);
                    LinearWeight[][] toFlatten = new LinearWeight[sizeSqrt * sizeSqrt][];
                    double latStep = (latmax - latmin) / (sizeSqrt - 1);
                    double lonStep = (lonmax - lonmin) / (sizeSqrt - 1);                    
                    for (int i = 0; i < sizeSqrt; i++)
                        for (int j = 0; j < sizeSqrt; j++)
                        {
                            var w = spli2d.GetLinearWeigths(latmin + latStep * i, lonmin + lonStep * j, delanay);
                            toFlatten[sizeSqrt * i + j] = w;
                        }
                    var devisor = sizeSqrt*sizeSqrt;
                    var flattenedWeights = toFlatten.SelectMany(weights => weights).GroupBy(w => w.DataIndex).Select(g => new LinearWeight(g.Key, g.Select(g1 => g1.Weight).Sum() / devisor)).ToArray();
                    yield return Tuple.Create(timeSegment, flattenedWeights);
                }
            }
            TraceVerbose("Computing field values finished in {0}",sw1.Elapsed);

        }

        protected async override Task<double[]> AggregateCellsBatchAsync(IRequestContext context, ComputationalContext computationalContext, IEnumerable<GeoCellTuple> cells)
        {
            await SaveObservationsAndDalanayDiagsForCells(context, computationalContext, cells);
            Dictionary<ITimeSegment, IObservationsInformation> observations = (Dictionary<ITimeSegment, IObservationsInformation>)computationalContext["observations"];
            var res= CalcLinearWeights(computationalContext, cells).Select(t => 
                {
                    var releventObservations=observations[t.Item1].Observations;
                    return t.Item2.Sum(x => x.Weight * releventObservations[x.DataIndex].Value);
                }).ToArray();
            return res;
        }
    }
}
