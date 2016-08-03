using Microsoft.Research.Science.FetchClimate2.DataHandlers;
using Microsoft.Research.Science.FetchClimate2.DataHandlers.Adapters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    public interface IBatchUncertaintyEvaluator
    {
        /// <summary>
        /// Evaluates the array of uncertainties for the sequence of rectangular areas (cells)
        /// </summary>
        /// <param name="variable">A variable name (in data source scope)</param>
        /// <param name="points">A sequence of point to evaluate the uncertainties for</param>
        /// <returns></returns>
        Task<double[]> EvaluateCellsBatchAsync(IEnumerable<ICellRequest> cells);
    }

    public interface IBatchValueAggregator
    {
        /// <summary>
        /// Computes an array of mean values for the sequence of points
        /// </summary>
        /// <param name="variable">A variable name (in data source scope)</param>
        /// <param name="cells">A sequence of cells to calculate the values for</param>
        /// <returns></returns>
        Task<double[]> AggregateCellsBatchAsync(IEnumerable<ICellRequest> cells);
    }

    /// <summary>
    /// Uses IBatchUncertaintyEvaluator and IBatchValueAggregator instead of IUncertaintyEvaluator and IValuesAggregator in the constructor respectively
    /// </summary>
    public abstract class BatchDataHandler : DataHandlerFacade
    {       
        public BatchDataHandler(IStorageContext context, IBatchUncertaintyEvaluator batchUncertaintyEvaluator, IBatchValueAggregator batchValueAggregator)
            : base(context, new BatchUncertatintyEvaluatorAdapter(batchUncertaintyEvaluator), new BatchValueAggregatorAdapter(batchValueAggregator))
        {           
        }           
    }
}
