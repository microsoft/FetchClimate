using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.DataCoverageEvaluators
{
    /// <summary>
    /// Describes the interpolation between individual observations (without extrapolation)
    /// </summary>
    public class IndividualObsCoverageEvaluator : IDataCoverageEvaluator
    {
        public IndividualObsCoverageEvaluator()
        { }

        public DataCoverageResult EvaluateInterval(double[] grid, double min, double max)
        {
            int l = grid.Length;
            if(l<1)
                throw new ArgumentException("Grid for search can't be empty");
            if (min < grid[0] || max > grid[l - 1])
                return DataCoverageResult.OutOfData;
            else
                return DataCoverageResult.DataWithUncertainty;
                
        }
    }
}
