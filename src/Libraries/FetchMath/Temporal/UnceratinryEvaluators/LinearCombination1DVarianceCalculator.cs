using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators
{
    public interface IGaussianProcessDescription
    {
        double Dist(double location1, double location2);
        VariogramModule.IVariogram Variogram { get; }
    }

    public interface IGaussianProcessDescriptionFactory
    {
        IGaussianProcessDescription Create(string varName);
    }

    /// <summary>
    /// Propagetes base node uncertatinty as is. Useful for intance for reduced time dimension datasets (e.g. DEM datasets with no time dimension)
    /// </summary>
    public class ReducedDemensionLinearCombVarianceCalc : ILinearCombination1DVarianceCalculator
    {        
        public ReducedDemensionLinearCombVarianceCalc()
        {            
        }

        public async Task<double> GetVarianceForCombinationAsync(IPs temporalWeights, ICellRequest cell, double baseVariance)
        {
            return baseVariance;
        }
    }

    /// <summary>
    /// Reutns an uncertatinty using variogram extracted via IGaussianProcessDescriptionFactory, propagating base uncertatinty as a nugget value
    /// </summary>
    public class LinearCombination1DVarianceCalc : ILinearCombination1DVarianceCalculator
    {
        private static AutoRegistratingTraceSource ts = new AutoRegistratingTraceSource("VariogramBasedLinearCombVarianceCalc", SourceLevels.All);
        private readonly IGaussianProcessDescriptionFactory variogramFactory;
        private readonly ConcurrentDictionary<string,Lazy<IGaussianProcessDescription>> dict= new ConcurrentDictionary<string,Lazy<IGaussianProcessDescription>>();
        private readonly ITimeAxisLocator axisLocator;
        private readonly double[] timeAxis;

        public LinearCombination1DVarianceCalc(IGaussianProcessDescriptionFactory factory, ITimeAxisLocator timeAxisLocator)
        {            
            this.variogramFactory = factory;
            this.axisLocator = timeAxisLocator;
            timeAxis = axisLocator.AxisValues;
        }

        public Task<double> GetVarianceForCombinationAsync(IPs temporalWeights, ICellRequest cell, double baseNodeVariance)
        {
            var capturedValues = Tuple.Create(temporalWeights, cell, baseNodeVariance);

            return Task.Factory.StartNew(obj =>
                {
                    Tuple<IPs, ICellRequest, double> typedObj = (Tuple<IPs, ICellRequest, double>)obj;

                    var name = typedObj.Item2.VariableName;

                    var processDescription = dict.GetOrAdd(name,new Lazy<IGaussianProcessDescription>(() =>                        
                        {
                            var v = variogramFactory.Create(name);
                            if (v == null)
                                ts.TraceEvent(TraceEventType.Warning, 1, string.Format("Could not find temporal variogram for the \"{0}\" variable. Skipping uncertainty propagation analysis", name));
                            else
                                ts.TraceEvent(TraceEventType.Information, 2, string.Format("Loaded temporal variogram for \"{0}\" variable", name));
                            return v;
                        },System.Threading.LazyThreadSafetyMode.ExecutionAndPublication)).Value;

                    if (processDescription == null)//Variogram not found.
                        return double.MaxValue;

                    var variogram = processDescription.Variogram;

                    var tempPrecomputedNugget = variogram.Nugget;
                    var tempPrecomputedSill = variogram.Sill;

                    double tempSpatIndependentVariance = double.IsNaN(typedObj.Item3) ? (tempPrecomputedNugget) : (typedObj.Item3);
                    var tempVarProps = new TemporalVarianceProperties(tempPrecomputedSill - tempPrecomputedNugget + tempSpatIndependentVariance, new Func<double, double, double>((coord1, coord2) => (tempPrecomputedSill - variogram.GetGamma(processDescription.Dist(coord1, coord2)))));
                    double temporalVariance = GetTemporalVariance(typedObj.Item1, timeAxis, axisLocator.getAproximationGrid(cell.Time), tempVarProps);
                    Debug.Assert(temporalVariance >= 0.0);
                    return temporalVariance;
                }, capturedValues);
        }

        private static double GetTemporalVariance(IPs temporalWeights, double[] nodeLocations, double[] targetLocations, TemporalVarianceProperties varianceProps)
        {
            //Var(x) = Cov(0) + sum(sum(v_i*v_j*Cov(X_i,X_j)) - 2.0 * sum(v_i*Cov(X_i,X))) for semivariograms.
            double tempCov0 = varianceProps.Cov0;
            var tempCovariogram = varianceProps.Covariance;
            double acc = tempCov0; //Cov(0)
            var weights = temporalWeights.Weights;
            var indices = temporalWeights.Indices;
            int N = temporalWeights.Weights.Length;
            int M = targetLocations.Length;
            double sum2 = 0.0;
            for (int i = 0; i < N; i++)
            {
                // sum(sum(lambda.[i]*lambda.[j]*Cov(X_i,X_j))
                var weight_i = weights[i];
                acc += weight_i * weight_i * tempCov0;
                for (int j = i + 1; j < N; j++)
                    acc += 2.0 * weight_i * weights[j] * tempCovariogram.Invoke(nodeLocations[indices[i]], nodeLocations[indices[j]]);

                //cov(x,x_i))
                double cov_x_xi = 0.0;
                for (int j = 0; j < M; j++) //average cov(x,x_i) as in block kriging for region requsts
                    cov_x_xi += tempCovariogram.Invoke(nodeLocations[indices[i]], targetLocations[j]);
                sum2 += weight_i * cov_x_xi / M;
            }
            return 0.5*acc - sum2; //as dealing with full variograms instead of semivariogrmas acc - 2.0* sum2
        }
    }    
}
