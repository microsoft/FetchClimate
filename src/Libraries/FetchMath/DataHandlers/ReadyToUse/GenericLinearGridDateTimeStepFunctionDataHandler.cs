using Microsoft.Research.Science.FetchClimate2.TimeAxisAvgProcessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.DataHandlers
{
    public class GenericLinearGridDateTimeStepFunctionDataHandler : BatchDataHandler
    {
        public static async Task<GenericLinearGridDateTimeStepFunctionDataHandler> CreateAsync(IStorageContext dataContext)
        {
            var timeAxis = await TimeAxisAutodetection.GetTimeAxisAsync(dataContext);
            var timeIntegrator = new TimeAxisAvgFacade(timeAxis,new TimeAxisProjections.DateTimeMoments(),new WeightProviders.StepFunctionInterpolation(), new DataCoverageEvaluators.ContinousMeansCoverageEvaluator());

            var a = await GenericLinearGridDataHandlerHelper.EasyConstructAsync(dataContext, timeIntegrator);
            var uncertatintyEvaluator = a.Item1;
            var valuesAggregator = a.Item2;

            return new GenericLinearGridDateTimeStepFunctionDataHandler(dataContext, uncertatintyEvaluator, valuesAggregator);
        }

        GenericLinearGridDateTimeStepFunctionDataHandler(IStorageContext dataContext, IBatchUncertaintyEvaluator uncertaintyEvaluator, IBatchValueAggregator valueAggregator)
            : base(dataContext, uncertaintyEvaluator, valueAggregator)
        { }
    }
}
