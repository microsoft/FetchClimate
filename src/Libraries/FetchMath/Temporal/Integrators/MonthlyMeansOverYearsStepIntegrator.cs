using Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.Research.Science.FetchClimate2.TimeAxisAvgProcessing
{
    /// <summary>
    /// e.g. for worldclim 1.4, cru cl 2.0
    /// </summary>
    public class MonthlyMeansOverYearsStepIntegratorFacade : ITimeAxisAvgProcessing
    {
        static IWeightProvider weightsProvider = new WeightProviders.StepFunctionInterpolation();
        static IDataCoverageEvaluator coverageEvaluator = new DataCoverageEvaluators.ContinousMeansCoverageEvaluator();
        static double[] axis = Enumerable.Range(0, 24).Select(a => (double)a).ToArray();

        public IPs GetTempIPs(ITimeSegment t)
        {
            Debug.Assert(t.FirstYear != t.LastYear || t.FirstDay <= t.LastDay);

            bool isLeap = false;

            if (t.FirstYear == t.LastYear && DateTime.IsLeapYear(t.FirstYear))
                isLeap = true;


            int lastDay = t.LastDay;
            if (t.LastDay < t.FirstDay) //handling new year overlap
                lastDay += 365;

            double startProjection = DaysOfYearConversions.ProjectFirstDay(t.FirstDay, isLeap);
            double endProjection = DaysOfYearConversions.ProjectLastDay(lastDay, isLeap);

            var r = coverageEvaluator.EvaluateInterval(axis, startProjection, endProjection);
            Debug.Assert(r != DataCoverageResult.OutOfData);

            int startIndex, stopIndex;
            double[] weights = weightsProvider.GetWeights(axis, startProjection, endProjection, out startIndex, out stopIndex);

            double[] accumulatedWeights = new double[12];
            for (int i = startIndex; i <= stopIndex; i++)
                accumulatedWeights[i % 12] += weights[i - startIndex];
            List<double> weightsL = new List<double>(12);
            List<int> indecesL = new List<int>(12);

            for (int i = 0; i < 12; i++)
                if (accumulatedWeights[i] != 0.0) //both number and NAN will cause generation of IP
                {
                    indecesL.Add(i);
                weightsL.Add(accumulatedWeights[i]);
                }

            IPs integrationPoints = new IPs { Indices = indecesL.ToArray(), Weights = weightsL.ToArray(), BoundingIndices = new IndexBoundingBox { first = indecesL[0], last = indecesL[indecesL.Count - 1] } };
            return integrationPoints;
        }

        public IndexBoundingBox GetBoundingBox(ITimeSegment t)
        {
            Debug.Assert(t.FirstYear != t.LastYear || t.FirstDay <= t.LastDay);
            int lastDay = t.LastDay;

            bool isLeapYear = (t.FirstYear == t.LastYear && DateTime.IsLeapYear(t.FirstYear));
            var p1 = DaysOfYearConversions.ProjectFirstDay(t.FirstDay, isLeapYear);
            var p2 = DaysOfYearConversions.ProjectLastDay(t.LastDay, isLeapYear);
            
            IndexBoundingBox bb;

            if (lastDay < t.FirstDay)
                bb = new IndexBoundingBox() { first = 0, last = 11 }; //new year overlap
            else
                bb = new IndexBoundingBox() {
                    first = (int)Math.Floor(p1),
                    last = (int)Math.Floor(p2 - 1e-2) //the shift is less than "1 day" is needed to handle exact end of the month
                };

            return bb;
        }

        public double[] getAproximationGrid(ITimeSegment timeSegment)
        {
            IPs ips = GetTempIPs(timeSegment);
            double[] result = ips.Indices.Select(idx => axis[idx]).ToArray();
            return result;
        }

        public double[] AxisValues
        {
            get { return axis.Take(12).ToArray(); }
        }

        public DataCoverageResult GetCoverage(ITimeSegment t)
        {
            double tolerance = 1e-5;

            IPs ips = GetTempIPs(t);
            if (ips.Weights.All(e => Math.Abs(e-ips.Weights[0])<tolerance))
                return DataCoverageResult.DataWithUncertainty;
            else
                return DataCoverageResult.DataWithoutUncertainty;
        }
    }
}
