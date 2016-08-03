using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Research.Science.FetchClimate2.Integrators;
using Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators;

namespace Microsoft.Research.Science.FetchClimate2.Integrators.Spatial
{
    public class GeoAxis {
        /// <summary>
        /// sorted axis values
        /// </summary>
        protected readonly double[] grid; //always sorted
        /// <summary>
        /// raw data axis values (possibly descending)
        /// </summary>
        protected readonly double[] axisValues; //ordered as ra data
        protected readonly int dataLen;
        protected readonly int backIndexOffset; //used for recalulating index of descending axis

        protected readonly bool areLatsInverted;

        public GeoAxis(Array axis)
        {
            Type dataType = axis.GetType().GetElementType();
            if (dataType == typeof(float))
            {
                float[] floats = (float[])axis;

                dataLen = floats.Length;
                backIndexOffset = dataLen - 1;

                grid = new double[dataLen];
                axisValues = new double[dataLen];

                //descending axis
                if (floats[0] > floats[1])
                {
                    areLatsInverted = true;

                    for (int i = 0; i < dataLen; i++)
                        grid[backIndexOffset - i] = floats[i];
                }//ascending axes
                else
                {
                    areLatsInverted = false;
                    for (int i = 0; i < dataLen; i++)
                        grid[i] = floats[i];
                }

                for (int i = 0; i < dataLen; i++)
                    axisValues[i] = floats[i]; //axis Values are ordered as axis orded regardless on asc or desc axis
            }
            else if (dataType == typeof(double))
            {
                grid = ((double[])axis).ToArray();
                axisValues = ((double[])grid).ToArray();;

                dataLen = grid.Length;
                backIndexOffset = dataLen - 1;

                //descending axis
                if (grid[0] > grid[1])
                {
                    areLatsInverted = true;

                    double a;
                    //reversing axis
                    int backIndex;
                    for (int i = 0; i < dataLen / 2; i++)
                    {
                        backIndex = dataLen - 1 - i;
                        a = grid[i];
                        grid[i] = grid[backIndex];
                        grid[backIndex] = a;
                    }
                }
                else
                    areLatsInverted = false;
            }
            else
                throw new NotSupportedException("Used aggregator supports only float and double axis formats");
        }

        public double[] AxisValues
        {
            get { return axisValues; }
        }
    }

    /// <summary>
    /// Integration along the array element (supports ascending and descending array of float or double types)
    /// </summary>
    public class GridIntegratorFacade : GeoAxis, IGridAxisAvgProcessing        
    {
        public GridIntegratorFacade(Array axis, IWeightProvider weightsProvider, IDataCoverageEvaluator coverageEvaluator)
            : base(axis)
        {
            this.coverageEvaluator = coverageEvaluator;
            this.weightsProvider = weightsProvider;
        }

        protected readonly IDataCoverageEvaluator coverageEvaluator;

        private IWeightProvider weightsProvider;        

        public IPs GetIPsForPoint(double coord)
        {
            return GetIPsForCell(coord, coord);
        }

        public IPs GetIPsForCell(double min, double max)
        {
            int start, stop;

            double[] weights = weightsProvider.GetWeights(grid, min, max, out start, out stop);
            IPs integrationPoints = BuildIPs(start, weights);

            System.Diagnostics.Debug.Assert(Math.Abs(integrationPoints.Weights.Sum() - 1.0) < 1e-12);

            return integrationPoints;
        }

        private IPs BuildIPs(int start, double[] weights)
        {
            int len = weights.Length;
            int[] indeces = new int[len];
            int index, maxIndex = int.MinValue, minIndex = int.MaxValue;
            if (areLatsInverted)
                for (int i = 0; i < len; i++)
                {
                    index = backIndexOffset - start - i;
                    if (index > maxIndex)
                        maxIndex = index;
                    if (index < minIndex)
                        minIndex = index;
                    indeces[i] = index;
                }
            else
                for (int i = 0; i < len; i++)
                {
                    index = start + i;
                    if (index > maxIndex)
                        maxIndex = index;
                    if (index < minIndex)
                        minIndex = index;
                    indeces[i] = index;
                }
            return new IPs { Indices = indeces, Weights = weights, BoundingIndices = new IndexBoundingBox { first = minIndex, last = maxIndex } };
        }


        public IndexBoundingBox GetBoundingBox(double coord)
        {
            return GetBoundingBox(coord, coord);
        }

        public IndexBoundingBox GetBoundingBox(double min, double max)
        {
            IndexBoundingBox boundingBox = weightsProvider.GetBoundingBox(grid, min, max);
            if (areLatsInverted && !boundingBox.IsSingular)
                boundingBox = new IndexBoundingBox { first = backIndexOffset - boundingBox.last, last = backIndexOffset - boundingBox.first };
            return boundingBox;
        }


        public DataCoverageResult GetCoverage(double coord)
        {
            return coverageEvaluator.EvaluateInterval(grid, coord, coord);
        }

        public DataCoverageResult GetCoverage(double min, double max)
        {
            return coverageEvaluator.EvaluateInterval(grid, min, max);
        }
    }
}
