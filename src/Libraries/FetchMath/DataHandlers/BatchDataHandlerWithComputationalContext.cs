using Microsoft.Research.Science.FetchClimate2.DataHandlers.Adapters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.DataHandlers
{
    public interface IBatchUncertaintyEvaluator<TComputationalContext>
    {
        /// <summary>
        /// Evaluates the array of uncertainties for the sequence of rectangular areas (cells)
        /// </summary>
        /// <param name="variable">A variable name (in data source scope)</param>
        /// <param name="points">A sequence of point to evaluate the uncertainties for</param>
        /// <returns></returns>
        Task<double[]> EvaluateCellsBatchAsync(TComputationalContext computationalContext, IEnumerable<ICellRequest> cells);
    }

    public interface IBatchValueAggregator<TComputationalContext>
    {
        /// <summary>
        /// Computes an array of mean values for the sequence of points
        /// </summary>
        /// <param name="variable">A variable name (in data source scope)</param>
        /// <param name="cells">A sequence of cells to calculate the values for</param>
        /// <returns></returns>
        Task<double[]> AggregateCellsBatchAsync(TComputationalContext computationalContext, IEnumerable<ICellRequest> cells);
    }    

    public interface IBatchComputationalContextFactory<TComputationalContext>
    {
        Task<TComputationalContext> CreateAsync(IEnumerable<ICellRequest> cells);
    }

    /// <summary>
    /// Syntax sugar class. An adaptor. Consumes the IEnumerable<GeoCellTuple> instead of raw IRequestContext in all of the interfaces
    /// </summary>
    /// <typeparam name="TComputationalContext"></typeparam>
    public abstract class BatchDataHandlerWithComputationalContext<TComputationalContext> : DataHandlerFacadeWithComputationalContext<TComputationalContext>
    {
        public BatchDataHandlerWithComputationalContext(IStorageContext context,IBatchComputationalContextFactory<TComputationalContext> batchComputationalContextFactory, IBatchUncertaintyEvaluator<TComputationalContext> batchUncertaintyEvaluator, IBatchValueAggregator<TComputationalContext> batchValueAggregator)
            : base(context, new BatchComputationalContextFactoryAdapter<TComputationalContext>(batchComputationalContextFactory), new BatchUncertatintyEvaluatorAdapter<TComputationalContext>(batchUncertaintyEvaluator), new BatchValueAggregatorAdapter<TComputationalContext>(batchValueAggregator))
        {           
        }      
    }
}
