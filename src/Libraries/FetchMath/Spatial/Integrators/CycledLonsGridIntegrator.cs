using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Research.Science.FetchClimate2.WeightProviders;

namespace Microsoft.Research.Science.FetchClimate2.Integrators.Spatial
{
    public class CycledLonsAvgFacade : IGridAxisAvgProcessing        
    {
        private readonly double[] initialData;
        private readonly double[] extendedGrid;
        private readonly int initialLen;
        private readonly bool areBoundingValuesTheSame;
        private readonly IDataCoverageEvaluator coverageEvaluator;
        private readonly IWeightProvider weightsProvider;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="areBoundingValuesTheSame">Indicates that the first and the last elements of the the axis are the same point (as the axis is cycled along longated)</param>
        public CycledLonsAvgFacade(Array axis,  IWeightProvider weightsProvider, IDataCoverageEvaluator coverageEvaluator,bool areBoundingValuesTheSame = false)
        {
            this.coverageEvaluator = coverageEvaluator;
            this.weightsProvider = weightsProvider;
            this.areBoundingValuesTheSame = areBoundingValuesTheSame;
            Type dataType = axis.GetType().GetElementType();

            if (dataType == typeof(double))
            {
                initialData = (double[])axis;
            }
            else if (dataType == typeof(float))
            {
                initialData = ((float[])axis).Select(a => (double)a).ToArray();
            }
            else
                throw new ArgumentException("Aggregator only supports axis of double and float types");
            initialLen = initialData.Length;
            int extendedLength = initialLen * 3;
            if (areBoundingValuesTheSame)
                extendedLength -= 2;
            extendedGrid = new double[extendedLength];

            Buffer.BlockCopy(initialData, 0, extendedGrid, 0, initialLen * sizeof(double));
            int doubledLen = initialLen * 2;
            int offset = 0;
            if (areBoundingValuesTheSame)
            {
                doubledLen -= 2;
                offset = 1;
            }
            for (int i = 0; i < doubledLen; i++)
                extendedGrid[initialLen + i] = extendedGrid[i + offset] + 360.0;
        }

        public IPs GetIPsForCell(double min, double max)
        {
            double minData = extendedGrid[0];
            if (max < minData || min < minData)
            {
                max += 360.0;
                min += 360.0;
            }
            if (min > max)
                max += 360.0;
            int start, stop;


            double[] weights = weightsProvider.GetWeights(extendedGrid, min, max, out start, out stop);
            IPs integrationPoints = BuildIPs(start, weights);

            return integrationPoints;
        }

        /// <summary>
        /// Accounts for extended virtual grid indices
        /// </summary>
        /// <param name="start"></param>
        /// <param name="weights"></param>
        /// <returns></returns>
        private IPs BuildIPs(int start, double[] weights)
        {
            int weightsCount = weights.Length;
            int[] indeces = new int[weightsCount];
            int index, maxIndex = int.MinValue, minIndex = int.MaxValue;
            if (areBoundingValuesTheSame)
                for (int i = 0; i < weightsCount; i++)
                {
                    index = i + start;
                    if (index > initialLen - 1)
                    {
                        index -= initialLen;
                        index = index % (initialLen - 1) + 1;
                    }

                    if (index > maxIndex)
                        maxIndex = index;
                    if (index < minIndex)
                        minIndex = index;
                    indeces[i] = index;
                }
            else
                for (int i = 0; i < weightsCount; i++)
                {
                    index = (start + i) % initialLen;
                    if (index > maxIndex)
                        maxIndex = index;
                    if (index < minIndex)
                        minIndex = index;
                    indeces[i] = index;
                }
            return new IPs { Weights = weights, Indices = indeces, BoundingIndices = new IndexBoundingBox { first = minIndex, last = maxIndex } };
        }

        public IndexBoundingBox GetBoundingBox(double coord)
        {
            int first, last;
            if (coord < extendedGrid[0])
                coord += 360.0;

            IndexBoundingBox bb;
            int ind = Array.BinarySearch(extendedGrid, coord);
            if (ind >= 0)
            {
                ind = ind % initialLen;
                bb = new IndexBoundingBox() { first = ind, last = ind };
            }
            else
            {
                ind = ~ind;
                first = (ind - 1) % initialLen;
                last = ind % initialLen;
                if (last < first)
                    bb = new IndexBoundingBox() { first = 0, last = initialLen - 1 };
                else
                    bb = new IndexBoundingBox() { first = first, last = last };
            }
            return bb;
        }

        public IndexBoundingBox GetBoundingBox(double min, double max)
        {
            double minData = extendedGrid[0];
            if (max < minData || min < minData)
            {
                max += 360.0;
                min += 360.0;
            }
            if (min > max)
                max += 360.0;


            int minInd = Array.BinarySearch(extendedGrid, min);
            if (minInd < 0)
                minInd = ~minInd - 1;


            int maxInd = Array.BinarySearch(extendedGrid, max);
            if (maxInd < 0)
                maxInd = ~maxInd;

            bool wholeGlobe = maxInd - minInd == 360;

            minInd = minInd % initialLen;
            maxInd = maxInd % initialLen;

            IndexBoundingBox bb;

            if (maxInd < minInd || wholeGlobe)
                bb = new IndexBoundingBox() { first = 0, last = initialLen - 1 };
            else
                bb = new IndexBoundingBox() { first = minInd, last = maxInd };


            return bb;
        }

        public double[] AxisValues
        {
            get { return initialData; }
        }

        public IPs GetIPsForPoint(double coord)
        {
            if (coord < extendedGrid[0])
                coord += 360.0;
            int start, stop;

            double[] weights = weightsProvider.GetWeights(extendedGrid, coord, coord, out start, out stop);
            IPs integrationPoints = BuildIPs(start, weights);

            return integrationPoints;
        }


        public DataCoverageResult GetCoverage(double coord)
        {
            if (coord < extendedGrid[0])
                coord += 360.0;
            return coverageEvaluator.EvaluateInterval(extendedGrid, coord, coord);
        }

        public DataCoverageResult GetCoverage(double min, double max)
        {
            double minData = extendedGrid[0];
            if (max < minData || min < minData)
            {
                max += 360.0;
                min += 360.0;
            }
            if (min > max)
                max += 360.0;
            return coverageEvaluator.EvaluateInterval(extendedGrid, min, max);
        }
    }
}
