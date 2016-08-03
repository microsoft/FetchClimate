using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.DataHandlers
{
    public class GenericLinearGridDateTimeMomentsDataHandler : BatchDataHandler
    {
        public static async Task<GenericLinearGridDateTimeMomentsDataHandler> CreateAsync(IStorageContext dataContext)
        {
            var timeIntegrator = new TimeAxisAvgProcessing.TimeAxisAvgFacade(
                await TimeAxisAutodetection.GetTimeAxisAsync(dataContext),
                new TimeAxisProjections.DateTimeMoments(),
                new WeightProviders.StepFunctionInterpolation(),
                new DataCoverageEvaluators.IndividualObsCoverageEvaluator());

            var a = await GenericLinearGridDataHandlerHelper.EasyConstructAsync(dataContext, timeIntegrator);
            var uncertatintyEvaluator = a.Item1;
            var valuesAggregator = a.Item2;

            return new GenericLinearGridDateTimeMomentsDataHandler(dataContext, uncertatintyEvaluator, valuesAggregator);
        }

        GenericLinearGridDateTimeMomentsDataHandler(IStorageContext dataContext, IBatchUncertaintyEvaluator uncertaintyEvaluator, IBatchValueAggregator valueAggregator)
            : base(dataContext, uncertaintyEvaluator, valueAggregator)
        { }
    }
}
