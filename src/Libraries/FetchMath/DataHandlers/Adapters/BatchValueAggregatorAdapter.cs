using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.DataHandlers.Adapters
{
    /// <summary>
    /// Adapter from IBatchValueAggregator to BatchValueAggregatorAdapter
    /// </summary>
    public class BatchValueAggregatorAdapter : IValuesAggregator
    {
        private readonly IBatchValueAggregator component;

        public BatchValueAggregatorAdapter(IBatchValueAggregator batchValueAggregator)
        {
            this.component = batchValueAggregator;
        }

        /// <summary>
        /// Produces the mean values array for the request supplied, applying the optional boolean mask to the request
        /// </summary>
        /// <param name="request">A spatio-temporal region to produce a mean values for</param>        
        /// <param name="mask">An optional boolean array (the same dimensions as a result array) containing false values for the nodes where the mean values calculation can be omitted</param>
        /// <returns></returns>
        public async Task<Array> AggregateAsync(IRequestContext context, Array mask = null)
        {
            IFetchRequest request = context.Request;

            var name = context.Request.EnvironmentVariableName;

            IEnumerable<IGeoCell> cells = RequestToBatchAdapter.Stratch(request, mask);
            IEnumerable<ICellRequest> requests = cells.Select(c => new NameAnnotatedGeoCell(c,name));
            double[] strechedResults = await component.AggregateCellsBatchAsync(requests);
            Array res = RequestToBatchAdapter.Fold(strechedResults, request, mask);
            return res;
        }       
    }

    /// <summary>
    /// Adapter from IBatchValueAggregator<TCompContext> to BatchValueAggregatorAdapter<TCompContext>
    /// </summary>
    public class BatchValueAggregatorAdapter<TComputationalContext> : IValuesAggregator<TComputationalContext>
    {
        private readonly IBatchValueAggregator<TComputationalContext> component;

        public BatchValueAggregatorAdapter(IBatchValueAggregator<TComputationalContext> batchValueAggregator)
        {
            this.component = batchValueAggregator;
        }

        /// <summary>
        /// Produces the mean values array for the request supplied, applying the optional boolean mask to the request
        /// </summary>
        /// <param name="request">A spatio-temporal region to produce a mean values for</param>        
        /// <param name="mask">An optional boolean array (the same dimensions as a result array) containing false values for the nodes where the mean values calculation can be omitted</param>
        /// <returns></returns>
        public async Task<Array> AggregateAsync(IRequestContext context,TComputationalContext computationalContext, Array mask = null)
        {
            IFetchRequest request = context.Request;
            string name = request.EnvironmentVariableName;

            IEnumerable<IGeoCell> cells = RequestToBatchAdapter.Stratch(request, mask);
            IEnumerable<ICellRequest> requests = cells.Select(c => new NameAnnotatedGeoCell(c,name));
            double[] strechedResults = await component.AggregateCellsBatchAsync(computationalContext, requests);
            Array res = RequestToBatchAdapter.Fold(strechedResults, request, mask);
            return res;
        }
    }
}
