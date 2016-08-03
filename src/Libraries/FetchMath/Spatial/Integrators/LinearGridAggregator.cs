using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Integrators.Spatial
{
    public class LinearGridIntegrator : GridIntegratorFacade
    {
        public LinearGridIntegrator(Array axis)
            : base(axis,
            new WeightProviders.LinearInterpolation(),
            new DataCoverageEvaluators.IndividualObsCoverageEvaluator())
        {}

        public static async Task<LinearGridIntegrator> ConstructAsync(IStorageContext context, string axisArrayName)
        {            
            return new LinearGridIntegrator(await context.GetDataAsync(axisArrayName));
        }
    }
}
