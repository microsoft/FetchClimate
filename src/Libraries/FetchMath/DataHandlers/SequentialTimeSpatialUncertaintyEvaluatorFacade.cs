using Microsoft.Research.Science.FetchClimate2.Integrators.Spatial;
using Microsoft.Research.Science.FetchClimate2.Integrators.Temporal;
using Microsoft.Research.Science.FetchClimate2.ValueAggregators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    public interface ITimeAxisLocator
    {
        /// <summary>
        /// Get the values in temporal space that can be passed to covariance function to estimate the covariance between some point and the timeSegment. Defined only if IsSegmentAligned true, otherwise can return arbitrary value.
        /// </summary>
        /// <param name="timeSegment"></param>
        /// <returns></returns>
        double[] getAproximationGrid(ITimeSegment timeSegment);

        /// <summary>
        /// Get the values of the axis. They can be used later to calculate the distances 
        /// </summary>
        /// <returns></returns>
        double[] AxisValues { get; }
    }

    public interface ISpatialAxisInfo
    {
        /// <summary>
        /// The locations of nodes in terms of IPs class (integration points). Getting the value of the array for the index specified in IPs gets corresponding grid node location
        /// </summary>
        double[] AxisValues { get; }
    }

    public interface ILinearCombination1DVarianceCalculator
    {
        Task<double> GetVarianceForCombinationAsync(IPs temporalWeights, ICellRequest cell, double baseNodeVariance);
    }

    public interface ILinearCombinationOnSphereVarianceCalculator
    {
        Task<double> GetVarianceForCombinationAsync(IPs latWeights, IPs lonWeights, ICellRequest cell, double baseNodeVariance);
    }    

    public interface INodeUncertaintyProvider
    {
        /// <summary>
        /// The method returns an sd value of the grid nodes in the region of specified cell for the specified variable
        /// The value will be propagated through the interpolation and averaging process
        /// NaN means that no prior information about the node uncertainty is available. NaN is default
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="cell"></param>
        /// <returns></returns>
        double GetBaseNodeStandardDeviation(ICellRequest cell);
    }

    class SpatialVarianceProperties //not nested class for testing purposes
    {
        public SpatialVarianceProperties(double cov0, Func<double, double, double, double, double> distance, Func<double,double> covaraince)
        {
            Cov0 = cov0;
            Covariance = covaraince;
            Distance = distance;
        }
        public double Cov0 { get; private set; }

        public Func<double, double> Covariance { get; private set; }        
        public Func<double, double, double, double, double> Distance { get; private set; }
    }

    class TemporalVarianceProperties //not nested class for testing purposes
    {
        public TemporalVarianceProperties(double cov0, Func<double, double, double> covariance)
        {
            Cov0 = cov0;
            Covariance = covariance;
        }
        public double Cov0 { get; private set; }
        public Func<double, double, double> Covariance { get; private set; }
    }

    public class SequentialTimeSpatialUncertaintyEvaluatorFacade : IBatchUncertaintyEvaluator
    {
        private static AutoRegistratingTraceSource ts = new AutoRegistratingTraceSource("GaussianFieldUncertaintyEvaluator", SourceLevels.All);
        private readonly ITimeAxisAvgProcessing timeAggregator2;
        private readonly IGridAxisAvgProcessing latIntegrator2;
        private readonly IGridAxisAvgProcessing lonIntegrator2;
        private readonly ILinearCombination1DVarianceCalculator temporalVarianceCalculator;
        private readonly ILinearCombinationOnSphereVarianceCalculator spatialVarianceCalculator;
        private readonly INodeUncertaintyProvider baseNodeUncertatintyProvider;


        public SequentialTimeSpatialUncertaintyEvaluatorFacade(
            ITimeAxisAvgProcessing timeAxisIntegrator,
            IGridAxisAvgProcessing latAxisIntegrator,
            IGridAxisAvgProcessing lonAxisIntegrator,
            ILinearCombination1DVarianceCalculator temporalVarianceCalculator,
            ILinearCombinationOnSphereVarianceCalculator spatialVarianceCalculator,
            INodeUncertaintyProvider baseNodeUncertatintyProvider)
        {
            this.temporalVarianceCalculator = temporalVarianceCalculator;
            this.spatialVarianceCalculator = spatialVarianceCalculator;
            this.baseNodeUncertatintyProvider = baseNodeUncertatintyProvider;

            timeAggregator2 = timeAxisIntegrator;
            latIntegrator2 = latAxisIntegrator;
            lonIntegrator2 = lonAxisIntegrator;
        }



        public async Task<double[]> EvaluateCellsBatchAsync(IEnumerable<ICellRequest> cells)
        {
            ICellRequest first = cells.FirstOrDefault();
            if (first == null)
                return new double[0];
            else
            {
                var cellsArray = cells.ToArray();

                ts.TraceEvent(TraceEventType.Start, 3, "Uncertainty evaluation started");
                Stopwatch sw = Stopwatch.StartNew();

                int N = cellsArray.Length;

                Task<double>[] resultTasks = new Task<double>[N];

                for (int i = 0; i < N; i++)
                {
                    var cell = cellsArray[i];
                    var coverage = GetIPsForCell(cell);
                    
                    IPs tempIps = coverage.Item1;

                    var capturedValues = Tuple.Create(coverage.Item2, coverage.Item3, cell);

                    double sd = baseNodeUncertatintyProvider.GetBaseNodeStandardDeviation(cell);
                    resultTasks[i] = temporalVarianceCalculator.GetVarianceForCombinationAsync(tempIps, cell, sd * sd).ContinueWith((t, obj) => //Using continuations to queue all the cells for calculation instead of awaiting for each of them
                        {
                            Tuple<IPs, IPs,ICellRequest> captured = (Tuple<IPs, IPs,ICellRequest>)obj;
                            IPs latIps = captured.Item1, lonIps = captured.Item2;
                            var cell2 = captured.Item3;
                            double temporalVariance = t.Result;
                            return spatialVarianceCalculator.GetVarianceForCombinationAsync(latIps, lonIps, cell2, temporalVariance).ContinueWith(t2 =>
                                {
                                    var res = t2.Result;
                                    return Math.Sqrt(res);
                                });
                        }, capturedValues).Unwrap();
                }
                double[] result = await Task.WhenAll(resultTasks);
                sw.Stop();
                ts.TraceEvent(TraceEventType.Stop, 3, string.Format("Calculated uncertainty for {0} cells in {1}", N, sw.Elapsed));
                return result;
            }
        }

        private Tuple<IPs, IPs, IPs> GetIPsForCell(ICellRequest cell)
        {
            var timeR = timeAggregator2.GetTempIPs(cell.Time);
            var latR = latIntegrator2.GetIPsForCell(cell.LatMin, cell.LatMax);
            var lonR = lonIntegrator2.GetIPsForCell(cell.LonMin, cell.LonMax);
            return Tuple.Create(timeR, latR, lonR);
        }


    }
}
