using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.DataHandlers
{   
    /// <summary>
    /// Generates context that can be passed to UncertaintyEvaluator and ValueAggregator
    /// </summary>
    /// <typeparam name="TComputationalContext"></typeparam>
    public interface IComputationalContextFactory<TComputationalContext> {
        Task<TComputationalContext> CreateAsync(IRequestContext context);
    }

    public interface IUncertaintyEvaluator<TComputationalContext>
    {
        Task<Array> EvaluateAsync(IRequestContext context, TComputationalContext computationalContext);
    }

    public interface IValuesAggregator<TComputationalContext>
    {
        Task<Array> AggregateAsync(IRequestContext context, TComputationalContext computationalContext, Array mask = null);
    }

    /// <summary>
    /// Generates the computational context using IComputationalContextFactory<TComputationalContext> and passed it to IUncertaintyEvaluator<TCmputationalContext> and IValuesAggregator<TComputationalContext> along with the request
    /// </summary>
    public class DataHandlerFacadeWithComputationalContext<TComputationalContext> : DataSourceHandler
    {
        private readonly IComputationalContextFactory<TComputationalContext> compContextFactory;
        private readonly IUncertaintyEvaluator<TComputationalContext> uncertaintyEvaluator;
        private readonly IValuesAggregator<TComputationalContext> valuesAggregator;
        protected readonly AutoRegistratingTraceSource traceSwitch;

        public DataHandlerFacadeWithComputationalContext(IStorageContext context,IComputationalContextFactory<TComputationalContext> computationalContextFactory, IUncertaintyEvaluator<TComputationalContext> uncertaintyEvaluator, IValuesAggregator<TComputationalContext> valuesAggregator)
            : base(context)
        {
            string typeString = this.GetType().ToString();
            traceSwitch = new AutoRegistratingTraceSource(string.Format("DataHandlerCC_{0}", typeString));
            this.uncertaintyEvaluator = uncertaintyEvaluator;
            this.valuesAggregator = valuesAggregator;
            this.compContextFactory = computationalContextFactory;
        }

        /// <summary>
        /// Implements the logic of sequential uncertainties evaluation, reporting it, acquiring data mask (a subset of the data which is actually needs to be processed), generating the mean values for points left
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override sealed async Task<Array> ProcessRequestAsync(IRequestContext context)
        {
            Stopwatch sw = Stopwatch.StartNew();
            var compContext = await compContextFactory.CreateAsync(context);
            sw.Stop();
            traceSwitch.TraceEvent(TraceEventType.Information, 1, string.Format("Computational context is ready in {0}",sw.Elapsed));
            sw = Stopwatch.StartNew();
            var uncertaintes = await uncertaintyEvaluator.EvaluateAsync(context, compContext);
            sw.Stop();
            traceSwitch.TraceEvent(TraceEventType.Information, 2, string.Format("Uncertatinty was evaluated in {0}", sw.Elapsed));
            sw = Stopwatch.StartNew();
            //WARNING!!!
            //TODO: Implicit dependency here. The user of the ProcessRequestAsync EXPECTS that context.GetMaskAsync is called during the execution and waits for it. Separate the method!
            var mask = await context.GetMaskAsync(uncertaintes);
            sw.Stop();
            traceSwitch.TraceEvent(TraceEventType.Verbose, 3, string.Format("FetchEngine returned a bitmask in {0}", sw.Elapsed));
            sw = Stopwatch.StartNew();
            var result = await valuesAggregator.AggregateAsync(context, compContext, mask);
            sw.Stop();
            traceSwitch.TraceEvent(TraceEventType.Information, 3, string.Format("Aggregated values were got in {0}", sw.Elapsed));
            return result;
        }
    }
}
