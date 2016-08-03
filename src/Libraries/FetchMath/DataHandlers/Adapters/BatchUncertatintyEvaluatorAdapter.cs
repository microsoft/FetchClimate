using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.DataHandlers.Adapters
{
    /// <summary>
    /// Adapter from IBatchUncertaintyEvaluator to IUncertaintyEvaluator
    /// </summary>
    public class BatchUncertatintyEvaluatorAdapter : IUncertaintyEvaluator
    {
        private readonly IBatchUncertaintyEvaluator component;

        public BatchUncertatintyEvaluatorAdapter(IBatchUncertaintyEvaluator batchUncertatintyEvaluator)
        {
            this.component = batchUncertatintyEvaluator;
        }

        /// <summary>
        /// Produces an array of uncertainties for the request supplied
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<Array> EvaluateAsync(IRequestContext context)
        {
            var name = context.Request.EnvironmentVariableName;
            var stratched = RequestToBatchAdapter.Stratch(context.Request);
            var annotated = stratched.Select(c => new NameAnnotatedGeoCell(c, name));
            var result = await component.EvaluateCellsBatchAsync(annotated);
            Array res = RequestToBatchAdapter.Fold(result, context.Request);
            return res;
        }
    }

    /// <summary>
    /// Adapter from IBatchUncertaintyEvaluator<TCompContext> to IUncertaintyEvaluator<TCompContext>
    /// </summary>
    public class BatchUncertatintyEvaluatorAdapter<TComputationalContext> : IUncertaintyEvaluator<TComputationalContext>
    {
        private readonly IBatchUncertaintyEvaluator<TComputationalContext> component;

        public BatchUncertatintyEvaluatorAdapter(IBatchUncertaintyEvaluator<TComputationalContext> batchUncertatintyEvaluator)
        {
            this.component = batchUncertatintyEvaluator;
        }

        /// <summary>
        /// Produces an array of uncertainties for the request supplied
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<Array> EvaluateAsync(IRequestContext context, TComputationalContext computationalContext)
        {
            var name = context.Request.EnvironmentVariableName;
            var stratched = RequestToBatchAdapter.Stratch(context.Request);
            var annotated = stratched.Select(c => new NameAnnotatedGeoCell(c,name));
            var result = await component.EvaluateCellsBatchAsync(computationalContext, annotated);
            Array res = RequestToBatchAdapter.Fold(result, context.Request);
            return res;
        }
    }
}
