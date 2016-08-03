using Microsoft.Research.Science.FetchClimate2.Integrators.Spatial;
using Microsoft.Research.Science.FetchClimate2.Integrators.Temporal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.DataHandlers
{
    public class GenericLinearMonthlyMeansGridDataHandler : BatchDataHandler
    {
        public static async Task<GenericLinearMonthlyMeansGridDataHandler> CreateAsync(IStorageContext dataContext)
        {
            var timeIntegrator = new TimeAxisAvgProcessing.MonthlyMeansOverYearsStepIntegratorFacade();
            var a = await GenericLinearGridDataHandlerHelper.EasyConstructAsync(dataContext, timeIntegrator);
            var uncertatintyEvaluator = a.Item1;
            var valuesAggregator = a.Item2;

            return new GenericLinearMonthlyMeansGridDataHandler(dataContext, uncertatintyEvaluator, valuesAggregator);
        }

        GenericLinearMonthlyMeansGridDataHandler(IStorageContext dataContext, IBatchUncertaintyEvaluator uncertaintyEvaluator, IBatchValueAggregator valueAggregator)
            : base(dataContext, uncertaintyEvaluator, valueAggregator)
        { }
    }
}
