using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    /// <summary>
    /// The join of ITimeAxisItegrator, ITimeCoverageProvider and ITimeAxisLocator
    /// </summary>
    public interface ITimeAxisAvgProcessing : ITimeAxisLocator, ITimeAxisIntegrator, ITimeCoverageProvider { }
}
