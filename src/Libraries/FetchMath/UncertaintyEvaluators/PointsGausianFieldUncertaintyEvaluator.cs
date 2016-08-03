using Microsoft.Research.Science.FetchClimate2.DataHandlers;
using Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints.LinearCombination;
using Microsoft.Research.Science.FetchClimate2.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators
{
    public interface IVariogramProvider
    {
        Task<VariogramModule.IVariogram> GetSpatialVariogramAsync(RealValueNodes nodes);
    }

    public interface IVariogramProviderFactory
    {
        Task<IVariogramProvider> ConstructAsync();
    }

    public class PointsGausianFieldUncertaintyEvaluator : IUncertaintyEvaluatorOfLinearCombination
    {
        public static readonly AutoRegistratingTraceSource traceSource = new AutoRegistratingTraceSource("PointsGausianFieldUncertaintyEvaluator", SourceLevels.All);
        private readonly IVariogramProviderFactory variogramProviderFactory;

        public PointsGausianFieldUncertaintyEvaluator(IVariogramProviderFactory variogramProviderFactory)
        {
            this.variogramProviderFactory = variogramProviderFactory;
        }

        public async Task<double[]> EvaluateCellsBatchAsync(LinearCombinationContext computationalContext, IEnumerable<ICellRequest> cells)
        {
            var variogramProvider = await variogramProviderFactory.ConstructAsync();

            var combinationsIterator = computationalContext.Combinations.GetEnumerator();


            traceSource.TraceEvent(TraceEventType.Start, 1, "Filtering out cells that are not requested");

            var filtered = cells.Select(cell =>
            {
                Tuple<ICellRequest, RealValueNodes, IEnumerable<LinearWeight>> currentElement;
                ICellRequest contextCell;
                do
                {
                    var nextExists = combinationsIterator.MoveNext();
                    if (!nextExists)
                        throw new InvalidOperationException("Received linear combination does not containg information about requested cell. Context is not synchronize with requested cells sequenece.");
                    currentElement = combinationsIterator.Current;
                    contextCell = currentElement.Item1;
                } while (!contextCell.Equals(cell));

                return currentElement;
            }).ToArray();

            traceSource.TraceEvent(TraceEventType.Stop, 1, "Filtered out cells that are not requested");
            traceSource.TraceEvent(TraceEventType.Start, 2, "Evaluating uncertatinty for sequence of cells using precomputed linear combinations");

            var resultsTasks = filtered.AsParallel().AsOrdered().Select(async tuple =>            
            {
                if (tuple.Item2.Lats.Length == 0)
                    return Double.NaN; //there are no nodes. Out of data request? can't produce uncertainty
                var variogram = await variogramProvider.GetSpatialVariogramAsync(tuple.Item2);
                var cell = tuple.Item1;
                var weights = tuple.Item3.ToArray();
                var nodes = tuple.Item2;

                Debug.Assert(Math.Abs(weights.Sum(w => w.Weight) - 1.0) < 1e-10);

                double sill = variogram.Sill;

                double cellLat = (cell.LatMax + cell.LatMin) * 0.5;
                double cellLon = (cell.LonMax + cell.LonMin) * 0.5;
                //var = cov(0)+ sum sum (w[i]*w[j]*cov(i,j))-2.0*sum(w[i]*cov(x,i))
                double cov_at_0 = sill;

                double acc = cov_at_0; //cov(0)                    
                for (int i = 0; i < weights.Length; i++)
                {
                    double w = weights[i].Weight;
                    int idx1 = weights[i].DataIndex;
                    double lat1 = nodes.Lats[idx1];
                    double lon1 = nodes.Lons[idx1];
                    for (int j = 0; j < i; j++)
                    {
                        int idx2 = weights[j].DataIndex;
                        double lat2 = nodes.Lats[idx2];
                        double lon2 = nodes.Lons[idx2];
                        double dist = SphereMath.GetDistance(lat1, lon1, lat2, lon2);
                        double cov = sill - variogram.GetGamma(dist);
                        acc += 2.0 * w * weights[j].Weight * cov;
                    }
                    acc += w * w * cov_at_0; //diagonal elements
                    double dist2 = SphereMath.GetDistance(lat1, lon1, cellLat, cellLon);
                    double cov2 = sill - variogram.GetGamma(dist2);
                    acc -= 2.0 * w * cov2;
                }
                return acc*0.5; //as dealing with full variogram instead of semivariogram
            }).ToArray();
            var results = await Task.WhenAll(resultsTasks);
            traceSource.TraceEvent(TraceEventType.Stop, 2, "Evaluated uncertatinty for sequence of cells using precomputed linear combinations");
            return results;
        }
    }
}
