using System;
using System.Linq;
using System.Collections.Generic;

namespace Microsoft.Research.Science.FetchClimate2.WeightProviders
{
    public class LinearInterpolation : IWeightProvider
    {
        public LinearInterpolation() { }
        //the method is not static as the class is used in typed parmeters
        public double[] GetWeights(double[] grid, double min, double max, out int start, out int stop, DoubleEpsComparer dec = null)
        {
            if (DoubleEpsComparer.Instance.Compare(min, max) > 0)
                throw new ArgumentException("Min should be less or equal to max");
            
            if (grid[0] > grid[grid.Length - 1])
                throw new ArgumentException("Grid is not ascending. LinearWeightsProvider can't be used");

            if (grid.Length < 2)
            {
                if (grid.Length == 1 && (Math.Abs(max - min) < 0.0000000000001) && (Math.Abs(grid[0] - min) < 0.00001))
                {
                    start = 0;
                    stop = 0;
                    return new double[] { 1.0 };
                }
                else
                    throw new ArgumentException("Grid length must be at least 2");
            }

            DoubleEpsComparer effDec = (dec == null) ? DoubleEpsComparer.Instance : dec;

            if (effDec.Compare(min, grid[0]) < 0)
            {
                start = stop = 0;
                return new double[0];                
            }
            if (effDec.Compare(max, grid[grid.Length - 1]) > 0)
            //    max = grid[grid.Length - 1];
            {
                start = stop = 0;
                return new double[0];
            }

            bool leftBoundUnexact = false, rightBoundUnexact = false;

            int imin = Array.BinarySearch<double>(grid, (double)min, effDec);
            if (imin < 0)
            {
                imin = ~imin - 1;
                leftBoundUnexact = true;
            }

            int imax = Array.BinarySearch<double>(grid, imin, grid.Length - imin, (double)max, effDec);
            if (imax < 0)
            {
                imax = ~imax;
                rightBoundUnexact = true;
            }
            System.Diagnostics.Debug.Assert(imax < grid.Length);


            double[] w = new double[imax - imin + 1];
            w.Initialize();

            if (imax == imin)
            {
                w[0] = 1.0;
                start = stop = imin;
            }
            else if (imax == imin + 1) //inside single grid interval
            {
                double x_k = grid[imin];
                double x_m = grid[imax];

                double k;

                double x_m_k = x_m - x_k;

                k = 0.5 * (min + max - 2.0 * x_k) / x_m_k; //both for min=max and non-zero length intervals
                w[1] = k;
                w[0] = 1 - k;
            }
            else
            {
                double divider = (max - min) * 2.0;
                double multiplier = 1.0 / divider;

                int solidIntervals_imin = leftBoundUnexact ? imin + 1 : imin;
                int solidIntervals_imax = rightBoundUnexact ? imax - 1 : imax;
                int solid_imin_output_idx = leftBoundUnexact ? 1 : 0;
                int solid_imax_output_idx = rightBoundUnexact ? w.Length - 2 : w.Length - 1;
                if (solidIntervals_imax > solidIntervals_imin) //full covered intervals
                {
                    w[solid_imin_output_idx] = (grid[solidIntervals_imin + 1] - grid[solidIntervals_imin]) * multiplier;

                    for (int i = 1; i < solidIntervals_imax - solidIntervals_imin; i++)
                        w[solid_imin_output_idx + i] = (grid[solidIntervals_imin + i + 1] - grid[solidIntervals_imin + i - 1]) * multiplier;

                    w[solid_imax_output_idx] = (grid[solidIntervals_imax] - grid[solidIntervals_imax - 1]) * multiplier;
                }
                if (leftBoundUnexact) //partially covered intervals, if present
                {
                    double x_0 = grid[imin + 1];
                    double x_a = grid[imin];
                    double x_l = min;

                    double k = (x_l + x_0 - 2.0 * x_a) / (x_0 - x_a);

                    double width = x_0 - x_l;

                    w[0] = (2.0 - k) * width* multiplier;
                    w[1] += k * width * multiplier;
                }
                if (rightBoundUnexact)
                {
                    double x_r = max;
                    double x_z = grid[imax];
                    double x_n = grid[imax - 1];

                    double k = (x_n + x_r - 2.0 * x_n) / (x_z - x_n);

                    double width = x_r - x_n;

                    w[solid_imax_output_idx] += (2.0 - k) * width * multiplier;
                    w[solid_imax_output_idx + 1] = k * width * multiplier;
                }
            }

            System.Diagnostics.Debug.Assert(Math.Abs(w.Sum() - 1.0) < 1e-3); // test for unbiasness

            start = imin;
            stop = imax;
            return w;
        }


        public IndexBoundingBox GetBoundingBox(double[] grid, double min, double max, DoubleEpsComparer dec = null)
        {
            DoubleEpsComparer effDec = (dec == null) ? DoubleEpsComparer.Instance : dec;

            bool isLeftUnexact = false;
            int imin = Array.BinarySearch(grid, min, effDec);
            if (imin < 0)
            {
                imin = (~imin) - 1;
                isLeftUnexact = true;
            }

            bool isRightUnexact = false;
            int imax = Array.BinarySearch(grid, max, effDec);
            if (imax < 0)
            {
                imax = ~imax;
                isRightUnexact = true;
            }

            //if ((imin == -1 && imax == 0 && isLeftUnexact && isRightUnexact) || //interval is lower than the data
            //    (imin == grid.Length - 1 && imax == grid.Length && isLeftUnexact && isRightUnexact))  //is higher than the data
            //    return new IndexBoundingBox();

            if (imin == -1 && isLeftUnexact)
                return new IndexBoundingBox(); //interval crosses the lower bound of the grid
            if (imax == grid.Length && isRightUnexact)
                return new IndexBoundingBox();// interval crosses upper bound of the grid

            return new IndexBoundingBox { first = imin, last = imax };
        }
    }

}