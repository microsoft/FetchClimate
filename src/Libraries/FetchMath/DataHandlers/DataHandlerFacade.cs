using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Research.Science.FetchClimate2.DataHandlers;

namespace Microsoft.Research.Science.FetchClimate2
{
    public interface IUncertaintyEvaluator
    {
        Task<Array> EvaluateAsync(IRequestContext context);
    }

    public interface IValuesAggregator
    {
        Task<Array> AggregateAsync(IRequestContext context, Array mask = null);
    }

    /// <summary>
    /// Implements base DataHandler protocol using two separate components, an uncertainty evaluator and a values aggregator.
    /// </summary>
    /// <remarks>
    /// The <paramref name="uncertaintyEvaluator"/> uses  and IValuesAggregator
    /// </remarks>
    public abstract class DataHandlerFacade : DataSourceHandler
    { 
        private readonly IUncertaintyEvaluator uncertaintyEvaluator;
        private readonly IValuesAggregator valuesAggregator;
        protected readonly AutoRegistratingTraceSource traceSwitch;
        /// <summary>
        /// Creates and instance of the <see cref="DataHandlerFacade"/> class.
        /// </summary>
        /// <param name="context">Gives <paramref name="uncertaintyEvaluator"/> and <paramref name="valuesAggregator"/> access to data schema and values.</param>
        /// <param name="uncertaintyEvaluator">Uses <paramref name="context"/> to compute uncertainties.</param>
        /// <param name="valuesAggregator">Uses <paramref name="context"/> and a boolean mask to compute values.</param>
        public DataHandlerFacade(IStorageContext context, IUncertaintyEvaluator uncertaintyEvaluator, IValuesAggregator valuesAggregator) : base(context)
        {
            string typeString = this.GetType().ToString();
            traceSwitch = new AutoRegistratingTraceSource(string.Format("DataHandler_{0}", typeString));
            this.uncertaintyEvaluator = uncertaintyEvaluator;
            this.valuesAggregator = valuesAggregator;
        }        

        /// <summary>
        /// Implements the logic of sequential uncertainties evaluation, reporting it, acquiring data mask (a subset of the data which is actually needs to be processed), generating the mean values for points left
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override sealed async Task<Array> ProcessRequestAsync(IRequestContext context)
        {            
            var uncertaintes = await uncertaintyEvaluator.EvaluateAsync(context);
            //WARNING!!!
            //TODO: Implicit dependency here. The user of the ProcessRequestAsync EXPECTS that context.GetMaskAsync is called during the execution and waits for it. Separate the method!
            var mask = await context.GetMaskAsync(uncertaintes);
            return await valuesAggregator.AggregateAsync(context, mask);
        }                
    }
}