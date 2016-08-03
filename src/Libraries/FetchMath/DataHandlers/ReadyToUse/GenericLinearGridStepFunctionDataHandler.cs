using Microsoft.Research.Science.FetchClimate2.Integrators.Spatial;
using Microsoft.Research.Science.FetchClimate2.Integrators.Temporal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.DataHandlers
{
    public class GenericLinearGridStepFunctionDataHandler : BatchDataHandler
    {
        public static async Task<GenericLinearGridStepFunctionDataHandler> CreateAsync(IStorageContext dataContext)
        {
            var timeAxisDeterction = StepFunctionAutoDetectHelper.SmartDetectAxis(dataContext);
            var detected = timeAxisDeterction as StepFunctionAutoDetectHelper.AxisFound;
            if (detected == null)
                throw new InvalidOperationException("Can't autodetect time axis. See logs for particular failure reason");
            var axis = await dataContext.GetDataAsync(detected.AxisName);
            var timeIntegrator = StepFunctionAutoDetectHelper.ConstructAverager(detected.AxisKind, axis, detected.BaseOffset);
            var a = await GenericLinearGridDataHandlerHelper.EasyConstructAsync(dataContext, timeIntegrator);
            var uncertatintyEvaluator = a.Item1;
            var valuesAggregator = a.Item2;

            return new GenericLinearGridStepFunctionDataHandler(dataContext, uncertatintyEvaluator, valuesAggregator);
        }

        GenericLinearGridStepFunctionDataHandler(IStorageContext dataContext, IBatchUncertaintyEvaluator uncertaintyEvaluator, IBatchValueAggregator valueAggregator)
            : base(dataContext, uncertaintyEvaluator, valueAggregator)
        { }
    }
}
