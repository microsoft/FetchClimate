using System;
using System.Linq;
using System.Collections.Generic;

namespace Microsoft.Research.Science.FetchClimate2.DataMaskProviders
{
    public class PointValuesDataMaskProvider : IDataMaskProvider
    {
        public PointValuesDataMaskProvider() { }

        public IndexBoundingBox GetBoundingBox(double[] grid, double min, double max, DoubleEpsComparer dec = null)
        {
            int leftBound,rightBound;
            int leftIdx = Array.BinarySearch(grid,min);
            int rightIdx = Array.BinarySearch(grid,max);
            if(leftIdx>=0)
                leftBound = leftIdx;
            else           
                leftBound = ~ leftIdx;
            if(rightIdx>=0)
                rightBound = rightIdx;
            else
                rightBound = ~ rightIdx -1;
            if (rightBound<leftBound)
                return IndexBoundingBox.Singular;
            else
                return new IndexBoundingBox() { first= leftBound, last = rightBound};
        }

        public int[] GetIndices(double[] grid, double lowerBound, double upperBound, DoubleEpsComparer dec = null)
        {
            IndexBoundingBox bb = GetBoundingBox(grid,lowerBound,upperBound);
            if (bb.IsSingular)
                return new int[0];
            else
            {
                int first = bb.first;
                return Enumerable.Range(first, bb.last - first + 1).ToArray();
            }
        }
    }

}