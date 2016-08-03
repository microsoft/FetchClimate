using Microsoft.Research.Science.FetchClimate2.Integrators.Spatial;
using Microsoft.Research.Science.FetchClimate2.ValueAggregators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    public class CoveredPointsStatistics : GeoAxis, ISpatGridModeCalculator
    {
        private readonly IDataMaskProvider dataMaskProvider = new DataMaskProviders.PointValuesDataMaskProvider();
        public CoveredPointsStatistics(Array axis)
            : base(axis)
        { }


        public IndexBoundingBox GetBoundingBox(double coord)
        {
            return GetBoundingBox(coord,coord);
        }

        public IndexBoundingBox GetBoundingBox(double min, double max)
        {
            IndexBoundingBox sortedIBB = dataMaskProvider.GetBoundingBox(grid,min, max);            
            if (areLatsInverted && !sortedIBB.IsSingular)
            {
                int len = grid.Length;
                return new IndexBoundingBox() { first = backIndexOffset - sortedIBB.last, last = backIndexOffset - sortedIBB.first};
            }
            else
                return sortedIBB;
        }

        public int[] GetDataIndices(double lowerBound, double upperBound)
        {
            var res = dataMaskProvider.GetIndices(grid, lowerBound, upperBound);
            if (areLatsInverted)
            {
                int len = res.Length;
                for (int i = 0; i < len; i++)
                {
                    res[i] = backIndexOffset - res[i];
                }
            }
            return res;
        }

        public DataCoverageResult GetCoverage(double coord)
        {
            return GetCoverage(coord, coord);        
        }

        public DataCoverageResult GetCoverage(double min, double max)
        {
            var test = GetBoundingBox(min, max);
            if (test.IsSingular)
                return DataCoverageResult.OutOfData;
            else
                return DataCoverageResult.DataWithoutUncertainty;
        }
    }
}
