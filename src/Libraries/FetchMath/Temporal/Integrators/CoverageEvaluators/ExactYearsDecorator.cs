using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.TimeCoverageProviders
{
    /// <summary>
    /// returns DatawithUncertainty if the requested years match the data covered years. Otherwise returns the components result.
    /// e.g. CRU CL 2.0 if for years 1961-1990
    /// </summary>
    public class ExactYearsDecorator : ITimeCoverageProvider
    {
        private int firstDataYear, lastDataYear;
        private ITimeCoverageProvider component;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="component"></param>
        /// <param name="firstYear">The first year for which data corresponds</param>
        /// <param name="lastYear">The last year for which data corresponds</param>
        public ExactYearsDecorator(ITimeCoverageProvider component, int firstYear, int lastYear)
        {
            this.component = component;
            this.firstDataYear = firstYear;
            this.lastDataYear = lastYear;
        }

        public DataCoverageResult GetCoverage(ITimeSegment t)
        {
            var componentResult = component.GetCoverage(t);
            if (componentResult == DataCoverageResult.DataWithUncertainty)
            {
                if (t.FirstYear == this.firstDataYear && t.LastYear == this.lastDataYear)
                    return DataCoverageResult.DataWithUncertainty;
                else
                    return DataCoverageResult.DataWithoutUncertainty;
            }
            else
                return componentResult;
        }
    }
}
