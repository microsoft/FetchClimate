using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Research.Science.FetchClimate2.WeightProviders
{
    public class StepFunctionInterpolation : IWeightProvider
    {
        public double[] GetWeights(double[] grid, double min, double max, out int start, out int stop, DoubleEpsComparer dec = null)
        {
            if (grid.Length < 2)
                throw new ArgumentException("Supplied grid to small. Less 2 elements");
            if (grid[0] > grid[grid.Length - 1])
                throw new ArgumentException("Grid is not ascending. StepFunctionWeightsProvider can't be used");
            if (min > max)
                throw new ArgumentException("Min should be less or equal to max");

            if (max > grid[grid.Length - 1] || min < grid[0])
            {
                start = stop = 0;
                return new double[0];
            }

            bool requestGreaterThanGrid = (min > grid[grid.Length - 1]);
            bool requestLowerThanGrid = (max < grid[0]);
            if (requestGreaterThanGrid || requestLowerThanGrid) //out of the grid
            {
                if (requestGreaterThanGrid)
                    start = stop = grid.Length - 1;
                else
                    start = stop = 0;
                return new double[0];
            }

            DoubleEpsComparer effDec = (dec == null) ? DoubleEpsComparer.Instance : dec;

            int imin = Array.BinarySearch(grid, min, effDec);
            bool leftBoundUnexact = false;
            bool rightBoundUnexact = false;
            if (imin < 0)
            {
                leftBoundUnexact = true;
                imin = ~imin;
            }

            int imax = Array.BinarySearch(grid, max, effDec);
            if (imax < 0)
            {
                rightBoundUnexact = true;
                imax = ~imax;
                imax--;
            }
            imax--;
            List<double> weightsList = new List<double>(16);
            start = imin;
            stop = imax;
            if (imin - imax > 1)
            {
                weightsList.Add(1.0);
                start = stop = imin - 1;
            }
            else
            {
                if (leftBoundUnexact)
                {
                    weightsList.Add(grid[imin] - min);
                    start--;
                }
                for (int i = imin; i <= imax; i++)
                    weightsList.Add(grid[i+1] - grid[i]);
                if (rightBoundUnexact)
                {
                    weightsList.Add(max - grid[imax + 1]);
                    stop++;
                }
            }
            return weightsList.ToArray();
        }


        public IndexBoundingBox GetBoundingBox(double[] grid, double min, double max, DoubleEpsComparer dec = null)
        {
            DoubleEpsComparer effDec = (dec == null) ? DoubleEpsComparer.Instance : dec;

            int imin = Array.BinarySearch(grid, min, effDec);
            if (imin < 0)
            {
                imin = (~imin) - 1;
                if (imin == -1)
                    return new IndexBoundingBox();
            }

            int imax = Array.BinarySearch(grid, max, effDec);
            if (imax < 0)
            {
                imax = ~imax;
                if (imax == grid.Length)
                    return new IndexBoundingBox();
                imax--;
            }
            else if (imax > imin)
                imax--;
            return new IndexBoundingBox { first = imin, last = imax };
        }       
    }
}
