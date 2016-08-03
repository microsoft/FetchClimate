using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators
{
    /// <summary>
    /// Returns NaN as base uncertainty symbolizing no base uncertainty information available
    /// </summary>
    public class NoBaseUncertaintyProvider : INodeUncertaintyProvider
    {
        public double GetBaseNodeStandardDeviation(ICellRequest cell)
        {
            return double.NaN;
        }
    }

    /// <summary>
    /// Returns MaxValue (e.g. have data. no uncertainty for every cell)
    /// </summary>
    public class NoUncertaintyEvaluator : IBatchUncertaintyEvaluator
    {
        public async Task<double[]> EvaluateCellsBatchAsync(IEnumerable<ICellRequest> cells)
        {
            return cells.Select(c => Double.MaxValue).ToArray();
        }
    }
}
