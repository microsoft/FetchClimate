using Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.Research.Science.FetchClimate2.TimeAxisAvgProcessing
{
    /// <summary>
    /// The class substitutes the GetCoverage functionality of MonthlyMeansOverYearsStepIntegratorFacade instance by wrapping it into the ExactYearsDecorator.  
    /// e.g. for years bound check in cru cl 2.0
    /// </summary>
    public class MonthlyMeansOverExactYearsStepIntegratorFacade : ITimeAxisAvgProcessing
    {
        private ITimeAxisAvgProcessing component;
        private ITimeCoverageProvider decoratedProvider;

        /// <param name="firstYear">The first year for which data corresponds</param>
        /// <param name="lastYear">The last year for which data corresponds</param>
        public MonthlyMeansOverExactYearsStepIntegratorFacade(int firstYear, int lastYear)
        {            
            this.component = new MonthlyMeansOverYearsStepIntegratorFacade();
            this.decoratedProvider = new TimeCoverageProviders.ExactYearsDecorator(component, firstYear, lastYear);
        }

        public DataCoverageResult GetCoverage(ITimeSegment t)
        {
            return this.decoratedProvider.GetCoverage(t);
        }

        public double[] getAproximationGrid(ITimeSegment timeSegment)
        {
            return this.component.getAproximationGrid(timeSegment);
        }

        public double[] AxisValues
        {
            get { return this.component.AxisValues; }
        }

        public IndexBoundingBox GetBoundingBox(ITimeSegment t)
        {
            return this.component.GetBoundingBox(t);
        }

        public IPs GetTempIPs(ITimeSegment t)
        {
            return this.component.GetTempIPs(t);
        }
    }
}
