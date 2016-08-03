using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.DataCoverageEvaluators
{
    public class ContinousMeansCoverageEvaluator : IDataCoverageEvaluator
    {
        public DataCoverageResult EvaluateInterval(double[] grid, double min, double max)
        {
            int mini = Array.BinarySearch(grid,min);
            int maxi = Array.BinarySearch(grid,max);
            //if (mini >= 0 && maxi >= 0)
            //    return DataCoverageResult.DataWithUncertainty;
            if (mini < 0)
            {
                mini = ~mini;
                if (mini == 0 || mini == grid.Length)
                    return DataCoverageResult.OutOfData;
            }
            if (maxi < 0)
            {
                maxi = ~maxi;
                if (maxi == 0 || maxi == grid.Length)
                    return DataCoverageResult.OutOfData;
            }
            return DataCoverageResult.DataWithUncertainty;
            //return DataCoverageResult.DataWithoutUncertainty;
        }
    }
}
