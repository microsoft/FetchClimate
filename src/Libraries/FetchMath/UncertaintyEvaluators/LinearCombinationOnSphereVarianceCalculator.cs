using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators
{
    public interface IGaussianFieldDescription
    {
        double Dist(double lat1, double lon1, double lat2, double lon2);
        VariogramModule.IVariogram Variogram { get; }
    }

    public interface IGaussianFieldDescriptionFactory
    {
        IGaussianFieldDescription Create(string varName);
    }

    public class LinearCombinationOnSphereVarianceCalculator : ILinearCombinationOnSphereVarianceCalculator
    {
        private int cellDevisionCount;

        private static AutoRegistratingTraceSource ts = new AutoRegistratingTraceSource("LinearCombinationOnSphereVarianceCalculator", SourceLevels.All);
        private readonly IGaussianFieldDescriptionFactory variogramsFactory;
        private readonly ConcurrentDictionary<string, Lazy<IGaussianFieldDescription>> dict = new ConcurrentDictionary<string, Lazy<IGaussianFieldDescription>>();
        private double[] latAxis, lonAxis;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="variogramsFactory"></param>
        /// <param name="latAxis"></param>
        /// <param name="lonAxis"></param>
        /// <param name="cellDevisionCount">How many nodes approximate a cell. total number of nodes in grid approximating the cell will be squared</param>
        public LinearCombinationOnSphereVarianceCalculator(IGaussianFieldDescriptionFactory variogramsFactory, ISpatialAxisInfo latAxis, ISpatialAxisInfo lonAxis, int cellDevisionCount = 2)
        {
            this.variogramsFactory = variogramsFactory;
            this.cellDevisionCount = cellDevisionCount;
            this.latAxis = latAxis.AxisValues;
            this.lonAxis = lonAxis.AxisValues;
        }

        private static double[] Align(double[] locations, double[] candidates)
        {
            int N = candidates.Length;
            int M = locations.Length;
            double[] result = new double[N];
            for (int i = 0; i < N; i++)
            {
                double candidate = candidates[i];
                int idx = Array.BinarySearch(locations, candidate);
                if (idx >= 0)
                    result[i] = candidate;
                else
                {
                    idx = ~idx;
                    if (idx == M)
                        result[i] = locations[M - 1];
                    else if (idx == 0)
                        result[i] = locations[0];
                    else
                        if (locations[idx] - candidate < candidate - locations[idx - 1])
                            result[i] = locations[idx];
                        else
                            result[i] = locations[idx - 1];
                }
            }

            return result;
        }

        public Task<double> GetVarianceForCombinationAsync(IPs latWeights, IPs lonWeights, ICellRequest cell, double baseNodeVariance)
        {
            Tuple<IPs, IPs, ICellRequest, double> capturedValues = Tuple.Create(latWeights,lonWeights,cell,baseNodeVariance);

            return Task.Factory.StartNew(obj =>
                {
                    Tuple<IPs, IPs, ICellRequest, double> arg = (Tuple<IPs, IPs, ICellRequest, double>)obj;

                    var cell2 = arg.Item3;
                    var name = cell2.VariableName;

                    double[] targetLats, targetLons;

                    if (cell2.LatMax == cell2.LatMin && cell2.LonMax == cell2.LonMin)
                    {
                        targetLats = new double[] { cell2.LatMax };
                        targetLons = new double[] { cell2.LonMax };
                    }
                    else
                    {
                        double latStep = (cell2.LatMax - cell2.LatMin) / (cellDevisionCount - 1);
                        double lonStep = (cell2.LonMax - cell2.LonMin) / (cellDevisionCount - 1);
                        targetLats = Enumerable.Range(0, cellDevisionCount).Select(j => cell2.LatMin + j * latStep).ToArray();
                        targetLons = Enumerable.Range(0, cellDevisionCount).Select(j => cell2.LonMin + j * lonStep).ToArray();
                    }

                    //coerce each target location to the closest data grid node. As closest data grid node is representative for its region. (indirect coercing of distance function by moving target locations)
                    targetLats = Align(latAxis, targetLats);
                    targetLons = Align(lonAxis, targetLons);

                    var fieldDescription = dict.GetOrAdd(name, new Lazy<IGaussianFieldDescription>(() =>
                        {
                            var v = variogramsFactory.Create(name);
                            if (v == null)
                                ts.TraceEvent(TraceEventType.Warning, 1, string.Format("Could not find spatial variogram for the \"{0}\" variable. Skipping uncertainty propagation analysis", name));
                            else
                                ts.TraceEvent(TraceEventType.Information, 2, string.Format("Loaded spatial variogram for \"{0}\" variable", name));
                            return v;
                        }, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication)).Value;

                    if (fieldDescription == null)
                        return double.MaxValue;

                    var variogram = fieldDescription.Variogram;
                    var spatPrecomputedSill = variogram.Sill;
                    var spatPrecomputedNugget = variogram.Nugget;

                    var coercedBaseVariance = double.IsNaN(arg.Item4) ? (spatPrecomputedNugget) : arg.Item4;
                    var spatDistance = new Func<double, double, double, double, double>((lat1, lon1, lat2, lon2) => fieldDescription.Dist(lat1, lon1, lat2, lon2));
                    var spatCovariogram = new Func<double, double>(distance => (spatPrecomputedSill - variogram.GetGamma(distance)));
                    SpatialVarianceProperties svp = new SpatialVarianceProperties(spatPrecomputedSill - spatPrecomputedNugget + coercedBaseVariance, spatDistance,spatCovariogram);

                    return GetSpatialVariance(arg.Item1, arg.Item2, latAxis, lonAxis, targetLats, targetLons, svp); //basic scale_factor/add_offset data transform are assumed to be applied during variograms fitting
                }, capturedValues);
        }

        private static double GetSpatialVariance(IPs latWeights, IPs lonWeights, double[] latNodes, double[] lonNodes, double[] targetLats, double[] targetLons, SpatialVarianceProperties varianceProps)
        {
            //Var(x) = Cov(0) + sum(sum(v_i*v_j*Cov(X_i,X_j)) - 2.0 * sum(v_i*Cov(X_i,X))) for semivariograms
            double spatCov0 = varianceProps.Cov0;
            var spatCovariogram = varianceProps.Covariance;
            var dist = varianceProps.Distance;
            double acc = spatCov0; //Cov(0)
            var weights1 = latWeights.Weights;
            var indices1 = latWeights.Indices;
            var weights2 = lonWeights.Weights;
            var indices2 = lonWeights.Indices;
            var N1 = weights1.Length;
            var N2 = weights2.Length;
            int N_T = N1 * N2;
            var M1 = targetLats.Length;
            var M2 = targetLons.Length;
            double sum2 = 0.0;
            Tuple<double, double, double>[] stratched = new Tuple<double, double, double>[N_T];
            for (int i = 0; i < N1; i++)
                for (int j = 0; j < N2; j++)
                    stratched[N2 * i + j] = Tuple.Create(weights1[i] * weights2[j], latNodes[indices1[i]], lonNodes[indices2[j]]);

            for (int i = 0; i < N_T; i++)
            {
                var element_i = stratched[i];
                double weight_i = element_i.Item1;
                double lat_i = element_i.Item2;
                double lon_i = element_i.Item3;
                acc += weight_i * weight_i * spatCov0;
                for (int j = i + 1; j < N_T; j++)
                {
                    var element_j = stratched[j];
                    // sum(sum(lambda.[i]*lambda.[j]*Cov(X_i,X_j))
                    acc += 2.0 * weight_i * element_j.Item1 * spatCovariogram(dist(lat_i, lon_i, element_j.Item2, element_j.Item3));
                }

                double cov_x_xi = 0.0;
                for (int k = 0; k < M1; k++) //average cov(x,x_i) as in block kriging for region requsts
                    for (int l = 0; l < M2; l++)
                        cov_x_xi += spatCovariogram(dist(targetLats[k], targetLons[l], lat_i, lon_i));
                //v_i*Cov(X_i,X)))
                sum2 += weight_i * cov_x_xi / (M1 * M2);
            }
            return 0.5*acc - sum2; //as working with full variograms instead of semivariograms
        }

    }
}
