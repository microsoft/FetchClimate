using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{    
    public interface IDataCoverageEvaluator
    {
        DataCoverageResult EvaluateInterval(double[] grid, double min, double max);
    }
}
