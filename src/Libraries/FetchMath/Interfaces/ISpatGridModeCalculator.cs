using Microsoft.Research.Science.FetchClimate2.ValueAggregators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Research.Science.FetchClimate2
{

    public interface ISpatGridModeCalculator : ISpatGridBoundingBoxCalculator, ISpatGridDataMaskProvider, IGridCoverageProvider { }
}
