using Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.Research.Science.FetchClimate2.TimeAxisAvgProcessing
{
    /// <summary>
    /// The class substitutes the GetCoverage functionality of MonthlyMeansOverYearsStepIntegratorFacade instance by wrapping it into the YearsRangeLengthDecorator.  
    /// e.g. for years bound check in cru WorldClim 1.4
    /// </summary>
    public class MonthlyMeansOverEnoughYearsStepIntegratorFacade : ITimeAxisAvgProcessing
    {
        private ITimeAxisAvgProcessing component;
        private ITimeCoverageProvider decoratedProvider;

        /// <param name="permittedYearsLength">The minimum length of the requested years range for wich the uncertainty is returned</param>        
        public MonthlyMeansOverEnoughYearsStepIntegratorFacade(int permittedYearsLength)
        {            
            this.component = new MonthlyMeansOverYearsStepIntegratorFacade();
            this.decoratedProvider = new TimeCoverageProviders.YearsRangeLengthDecorator(component, permittedYearsLength);
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
