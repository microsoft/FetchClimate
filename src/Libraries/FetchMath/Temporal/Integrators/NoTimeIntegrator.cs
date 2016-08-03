using Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Integrators.Temporal
{
    /// <summary>
    /// An integrator for the absence of time axis (time independent data)
    /// </summary>
    public class NoTimeIntegrator : ITimeAxisIntegrator
    {
        public IPs GetTempIPs(ITimeSegment t)
        {
            IPs integrationPoints = new IPs()
            {
                BoundingIndices = new IndexBoundingBox() { first = 0, last = 0 },
                Indices = new int[] { 0 },
                Weights = new double[] { 1.0 }
            };
            return integrationPoints;
        }

        public IndexBoundingBox GetBoundingBox(ITimeSegment t)
        {
            return new IndexBoundingBox() { first = 0, last = 0 };            
        }
    }

    public class NoTimeAvgProcessing : NoTimeIntegrator, ITimeAxisAvgProcessing
    {
        private double[] value = new double[] { 0.0 };

        public double[] getAproximationGrid(ITimeSegment timeSegment)
        {
            return value;
        }

        public double[] AxisValues
        {
            get { return value; }
        }

        public DataCoverageResult GetCoverage(ITimeSegment t)
        {
            return DataCoverageResult.DataWithUncertainty;
        }
    }
}
