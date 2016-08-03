using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.TimeCoverageProviders
{
    /// <summary>
    /// returns DatawithUncertainty if the requested years range is equal or greater than specifind length
    /// e.g. WorldClim 1.4: the data covers the current time, >=50 years long requests.
    /// </summary>
    public class YearsRangeLengthDecorator : ITimeCoverageProvider
    {
        private int permitedYearsLength;
        private ITimeCoverageProvider component;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="component"></param>
        /// <param name="permitedYearsLength">The minimum length of the requested years range for wich the uncertainty is returned</param>        
        public YearsRangeLengthDecorator(ITimeCoverageProvider component, int permitedYearsLength)
        {
            this.component = component;
            this.permitedYearsLength = permitedYearsLength;            
        }

        public DataCoverageResult GetCoverage(ITimeSegment t)
        {
            var componentResult = component.GetCoverage(t);
            if (componentResult == DataCoverageResult.DataWithUncertainty)
            {
                if (t.LastYear-t.FirstYear+1 >= this.permitedYearsLength)
                    return DataCoverageResult.DataWithUncertainty;
                else
                    return DataCoverageResult.DataWithoutUncertainty;
            }
            else
                return componentResult;
        }
    }
}
