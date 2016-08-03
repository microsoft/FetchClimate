using Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints.LinearCombination;
using Microsoft.Research.Science.FetchClimate2.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.DataHandlers
{
    /// <summary>
    /// Represents a sequence of cells with corresponding nodes and linear weights to apply to the nodes to get the average value for the cell
    /// </summary>
    public class LinearCombinationContext
    {
        private readonly IEnumerable<Tuple<ICellRequest, RealValueNodes, IEnumerable<LinearWeight>>> combinations;

        public LinearCombinationContext(IEnumerable<Tuple<ICellRequest, RealValueNodes, IEnumerable<LinearWeight>>> combinations)
        {
            this.combinations = combinations;
        }

        /// <summary>
        /// Returns a sequence of cells with corresponding nodes and linear weights to apply to the nodes to get the average value for the cell
        /// </summary>
        public IEnumerable<Tuple<ICellRequest, RealValueNodes, IEnumerable<LinearWeight>>> Combinations
        {
            get {
                return combinations;
            }
        }        
    }

    public interface IUncertaintyEvaluatorOfLinearCombination : IBatchUncertaintyEvaluator<LinearCombinationContext>
    { }

    public interface ILinearCombintaionContextFactory : IBatchComputationalContextFactory<LinearCombinationContext>
    { }

    /// <summary>
    /// Applies the linear combination to the nodes extracted from computational Context to get aggregate the values for the cells
    /// </summary>
    public sealed class LinearCombinationAggregator : IBatchValueAggregator<LinearCombinationContext>
    {
        AutoRegistratingTraceSource traceSource = new AutoRegistratingTraceSource("LinearCombinationAggregator");

        public async Task<double[]> AggregateCellsBatchAsync(LinearCombinationContext computationalContext, IEnumerable<ICellRequest> cells)
        {
            Stopwatch sw = Stopwatch.StartNew();
            var contextEnumerator = computationalContext.Combinations.GetEnumerator();
            List<double> result = new List<double>();

            var exc = new InvalidOperationException("Cells in computationalContext do not correspond to the cells passed to the method");

            ICellRequest contextCell;
            Tuple<ICellRequest, RealValueNodes, IEnumerable<LinearWeight>> contextElement;

             foreach (var cell in cells)
            {
                do
                {
                    var nextExists = contextEnumerator.MoveNext();
                    if (!nextExists)
                        throw exc;
                    contextElement = contextEnumerator.Current;
                    contextCell = contextElement.Item1;
                } while (!contextCell.Equals(cell));

                double[] nodeValues = contextElement.Item2.Values;

                bool isEmpty = true;
                double acc = 0.0;
                foreach (var linearWeight in contextElement.Item3)
                {
                    acc += linearWeight.Weight * nodeValues[linearWeight.DataIndex];
                    isEmpty = false;
                }
                if (isEmpty)
                    result.Add(Double.NaN);
                else
                    result.Add(acc);
            }
             sw.Stop();
             traceSource.TraceEvent(TraceEventType.Information,1,string.Format("Averages for all cells are computed in {0}",sw.Elapsed));
            return result.ToArray();
        }
    }

    /// <summary>
    /// Uses linear combination of scattered point values to serve the requests
    /// </summary>
    public class ScatteredPointsAsLinearCombinationDataHandler : BatchDataHandlerWithComputationalContext<LinearCombinationContext>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="linearCombFactory">A factory that produces linear combinations for latter uncertainty evaluation and averaging for the series of requested cells</param>
        /// <param name="uncertainatyEvaluator">Evaluates the uncertainty of linear combination</param>
        public ScatteredPointsAsLinearCombinationDataHandler(IStorageContext context,ILinearCombintaionContextFactory linearCombFactory, IUncertaintyEvaluatorOfLinearCombination uncertainatyEvaluator)
            : base(context, linearCombFactory, uncertainatyEvaluator, new LinearCombinationAggregator())
        { }
    }
}
