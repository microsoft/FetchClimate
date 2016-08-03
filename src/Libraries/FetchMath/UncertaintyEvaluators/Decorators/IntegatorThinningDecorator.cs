using Microsoft.Research.Science.FetchClimate2.Integrators.Spatial;
using Microsoft.Research.Science.FetchClimate2.Integrators.Temporal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators
{    
    /// <summary>
    /// Performs thinning on the returned IPs to fullfil count constraint.   
    /// Warning. This is optimization through approximation. Useful in uncertainty estimation. But inacurate and can not be used in end value calculations
    /// </summary>
    public class SpatGridIntegatorThinningDecorator : IGridAxisAvgProcessing
    {
        private readonly IGridAxisAvgProcessing component;
        private readonly int countConstraint;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="component"></param>
        /// <param name="countConstraint">The maximum allowed number of IPs to be returned</param>
        public SpatGridIntegatorThinningDecorator(IGridAxisAvgProcessing component, int countConstraint = 5)
        {
            this.component = component;
            this.countConstraint = countConstraint;

            if (countConstraint < 2)
                throw new ArgumentException("Minimum allowed countConstreint is 2");
        }

        public IPs GetIPsForPoint(double coord)
        {
            IPs componentResult = component.GetIPsForPoint(coord);
            var coerced =  ThinningUtils.Thinning(componentResult, countConstraint);
            System.Diagnostics.Debug.Assert(Math.Abs(coerced.Weights.Sum()-1.0)<1e-12);
            return coerced;

        }

        public IPs GetIPsForCell(double min, double max)
        {
            IPs componentResult = component.GetIPsForCell(min, max);
            var coerced = ThinningUtils.Thinning(componentResult, countConstraint);
            System.Diagnostics.Debug.Assert(Math.Abs(coerced.Weights.Sum() - 1.0) < 1e-12);
            return coerced;
        }



        public IndexBoundingBox GetBoundingBox(double coord)
        {
            return component.GetBoundingBox(coord);
        }

        public IndexBoundingBox GetBoundingBox(double min, double max)
        {
            return component.GetBoundingBox(min, max);
        }

        public double[] AxisValues
        {
            get { return component.AxisValues; }
        }

        public DataCoverageResult GetCoverage(double coord)
        {
            return component.GetCoverage(coord);
        }

        public DataCoverageResult GetCoverage(double min, double max)
        {
            return component.GetCoverage(min, max);
        }
    }

    /// <summary>
    /// Performs thinning on the returned IPs to fullfil count constraint.   
    /// Warning. This is optimization through approximation. Useful in uncertainty estimation. But inacurate and can not be used in end value calculations
    /// </summary>
    public class TimeAxisIntegratorThinningDecorator : ITimeAxisAvgProcessing
    {
        private readonly int countConstraint;
        private readonly ITimeAxisAvgProcessing component;

        public TimeAxisIntegratorThinningDecorator(ITimeAxisAvgProcessing component, int countConstraint = 100)
        {
            this.component = component;
            this.countConstraint = countConstraint;

            if (countConstraint < 2)
                throw new ArgumentException("Minimum allowed countConstreint is 2");
        }

        public double[] getAproximationGrid(ITimeSegment timeSegment)
        {
            return component.getAproximationGrid(timeSegment);
        }

        public double[] AxisValues
        {
            get { return component.AxisValues; }
        }

        public IndexBoundingBox GetBoundingBox(ITimeSegment t)
        {
            return component.GetBoundingBox(t);
        }

        public IPs GetTempIPs(ITimeSegment t)
        {
            IPs componentResult = component.GetTempIPs(t);
            var coerced = ThinningUtils.Thinning(componentResult, countConstraint);
            double sum = coerced.Weights.Sum();
            System.Diagnostics.Debug.Assert(Math.Abs(sum - 1.0) < 1e-7);
            return coerced;
        }

        public DataCoverageResult GetCoverage(ITimeSegment t)
        {
            return component.GetCoverage(t);
        }
    }

    public class ThinningUtils
    {
        internal static IPs ThinByTwo(IPs original, bool isLeftThinning)
        {
            int prevIterationCount = original.Indices.Length;
            int currentIterationCount = ((prevIterationCount - 2) / 2) + 2;

            IPs currentIPs = new IPs() { BoundingIndices = original.BoundingIndices, Indices = new int[currentIterationCount], Weights = new double[currentIterationCount] };
            if (prevIterationCount % 2 == 1)
            {
                for (int i = 0; i < currentIterationCount - 2; i++)
                {
                    int origIdx = 2 + i * 2;
                    currentIPs.Indices[1+i] = original.Indices[origIdx];
                    currentIPs.Weights[1+i] = (original.Weights[origIdx - 1] + original.Weights[origIdx + 1]) * 0.5 + original.Weights[origIdx];
                }

                currentIPs.Indices[0] = original.Indices[0];
                currentIPs.Weights[0] = original.Weights[0] + original.Weights[1] * 0.5;
                int lastIdx = currentIterationCount - 1;
                currentIPs.Indices[lastIdx] = original.Indices[prevIterationCount - 1];
                currentIPs.Weights[lastIdx] = original.Weights[prevIterationCount - 1] + original.Weights[prevIterationCount - 2] * 0.5;
            }
            else
            {
                if (isLeftThinning)
                {
                    for (int i = 0; i < currentIterationCount - 2; i++)
                    {
                        int origIdx = 2 + i * 2;
                        currentIPs.Indices[1+i] = original.Indices[origIdx];
                        currentIPs.Weights[1+i] = original.Weights[origIdx] + (0.5 * original.Weights[origIdx - 1]);
                        if (i != currentIterationCount - 3)
                            currentIPs.Weights[1+i] += (original.Weights[origIdx + 1] * 0.5);
                    }
                    currentIPs.Indices[0] = original.Indices[0];
                    currentIPs.Weights[0] = original.Weights[0];
                    if (currentIterationCount>2)
                        currentIPs.Weights[0]+=original.Weights[1] * 0.5;
                    int lastIdx = currentIterationCount - 1;
                    currentIPs.Indices[lastIdx] = original.Indices[prevIterationCount - 1];
                    currentIPs.Weights[lastIdx] = original.Weights[prevIterationCount - 1];
                }
                else
                {
                    for (int i = 0; i < currentIterationCount - 2; i++)
                    {
                        int origIdx = 1 + i * 2;
                        currentIPs.Indices[1+i] = original.Indices[origIdx];
                        currentIPs.Weights[1+i] = original.Weights[origIdx] + (0.5 * original.Weights[origIdx + 1]);
                        if (i != 0)
                            currentIPs.Weights[1+i] += (original.Weights[origIdx - 1] * 0.5);
                    }
                    currentIPs.Indices[0] = original.Indices[0];
                    currentIPs.Weights[0] = original.Weights[0];
                    int lastIdx = currentIterationCount - 1;
                    currentIPs.Indices[lastIdx] = original.Indices[prevIterationCount - 1];                    
                    currentIPs.Weights[lastIdx] = original.Weights[prevIterationCount - 1];
                    if (currentIterationCount > 2)
                        currentIPs.Weights[lastIdx] += original.Weights[prevIterationCount - 2] * 0.5;
                }
            }
            return currentIPs;
        }

        internal static IPs Thinning(IPs original, int countTarget)
        {
            int prevIterationCount = original.Indices.Length;
            if (prevIterationCount <= countTarget)
                return original;
            IPs prevIPs = original;
            bool isLeftThinning = true;
            while (prevIterationCount > countTarget)
            {
                prevIPs = ThinByTwo(prevIPs, isLeftThinning);
                prevIterationCount = prevIPs.Indices.Length;
                isLeftThinning = !isLeftThinning;
            }
            return prevIPs;
        }
    }
}
