using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Research.Science.FetchClimate2.DataMaskProviders
{
    /// <summary>
    /// serves the request that coveres the data region. E.g. Ignores the additional areas requested by user which do not corespond to any data
    /// </summary>
    public class StepFunctionDataMaskProvider : IDataMaskProvider
    {
        public int[] GetIndices(double[] grid, double min, double max, DoubleEpsComparer dec = null)
        {
            IndexBoundingBox bb = GetBoundingBox(grid, min, max,dec);
            if (bb.IsSingular)
                return new int[0];
            else
            {
                int first = bb.first;
                return Enumerable.Range(first, bb.last - first + 1).ToArray();
            }
        }


        public IndexBoundingBox GetBoundingBox(double[] grid, double min, double max, DoubleEpsComparer dec = null)
        {
            if (grid.Length < 2)
                throw new ArgumentException("Supplied grid to small. Less 2 elements");
            if (grid[0] > grid[grid.Length - 1])
                throw new ArgumentException("Grid is not ascending. StepFunctionWeightsProvider can't be used");
            if (min > max)
                throw new ArgumentException("Min should be less or equal to max");

            int leftBound = Array.BinarySearch(grid, min);
            int rightBound = Array.BinarySearch(grid, max);
            int leftIdx, rightIdx;
            bool exactLeftBound = leftBound >= 0;
            if (exactLeftBound)            
                leftIdx = leftBound;                            
            else
                leftIdx = ~leftBound - 1;
            if (rightBound >= 0)
                rightIdx = rightBound - 1;
            else
                rightIdx = ~rightBound - 1;
            if (
                rightIdx < leftIdx ||
                rightIdx == -1 || //entirly to the left of data
                (leftIdx == grid.Length - 1 && !exactLeftBound)) //entirly to the right of data
                return IndexBoundingBox.Singular;
            else
            {
                leftIdx = Math.Max(0, leftIdx); //in case of request coveres additional interval not covered by data. it is stell valid
                return new IndexBoundingBox() { first = leftIdx, last = rightIdx };
            }
        }       
    }
}
