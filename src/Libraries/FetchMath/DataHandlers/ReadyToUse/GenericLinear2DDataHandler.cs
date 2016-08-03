using Microsoft.Research.Science.FetchClimate2.Integrators;
using Microsoft.Research.Science.FetchClimate2.Integrators.Spatial;
using Microsoft.Research.Science.FetchClimate2.Integrators.Temporal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.DataHandlers
{
    /// <summary>
    /// An universal handler to use with 2D grids. Applies bilinear interpolation. Checks for missing values
    /// </summary>
    public class GenericLinear2DDataHandler : BatchDataHandler
    {
        public static async Task<GenericLinear2DDataHandler> CreateAsync(IStorageContext dataContext)
        {
            var timeIntegrator = new NoTimeAvgProcessing();

            var a = await GenericLinearGridDataHandlerHelper.EasyConstructAsync(dataContext, timeIntegrator);
            var uncertatintyEvaluator = a.Item1;
            var valuesAggregator = a.Item2;

            return new GenericLinear2DDataHandler(dataContext, uncertatintyEvaluator, valuesAggregator);
        }

        GenericLinear2DDataHandler(IStorageContext dataContext, IBatchUncertaintyEvaluator uncertaintyEvaluator, IBatchValueAggregator valueAggregator)
            : base(dataContext, uncertaintyEvaluator, valueAggregator)
        { }
    }
}
