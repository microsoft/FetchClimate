using Microsoft.Research.Science.FetchClimate2.Integrators.Temporal;
using Microsoft.Research.Science.FetchClimate2.TimeAxisStatisticsProcessing;
using Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators;
using Microsoft.Research.Science.FetchClimate2.WeightProviders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.Research.Science.FetchClimate2.TimeAxisAvgProcessing
{    
    /// <summary>
    /// Works with ascending axis. The values are offsets from some baseTime value
    /// </summary>
    /// <typeparam name="WeightsProvider"></typeparam>
    public class TimeAxisAvgFacade : ContinousTimeAxis, ITimeAxisAvgProcessing        
    {
        private readonly ITimeAxisProjection timeAxisProjection;
        private readonly IWeightProvider weightsProvider;
        protected readonly IDataCoverageEvaluator coverageEvaluator;

        public TimeAxisAvgFacade(Array axis,ITimeAxisProjection timeAxisProjection, IWeightProvider weightsProvider, IDataCoverageEvaluator coverageEvaluator)
            : base(axis)
        {
            this.timeAxisProjection = timeAxisProjection;
            this.weightsProvider = weightsProvider;
            this.coverageEvaluator = coverageEvaluator;
        }       

        public IPs GetTempIPs(ITimeSegment t)
        {
            var intervals = GetTimeIntervals(t);
            var projections = intervals.Select(a => timeAxisProjection.ProjectIntervalToTheAxis(a));
            List<int> indecesList = new List<int>(1024);
            List<double> weightsList = new List<double>(1024);
            int start, stop;
            double[] weights = null;
            double sumWeights = 0.0;
            foreach (var projection in projections)
            {
                weights = weightsProvider.GetWeights(grid, projection.Item1, projection.Item2, out start, out stop);
                if (weights.Length == 0)
                    return new IPs { Indices = new int[0], Weights = new double[0] };
                else
                {
                    weightsList.AddRange(weights);
                    indecesList.AddRange(Enumerable.Range(start, stop - start + 1));
                }
            }
            if (weights == null) throw new InvalidOperationException("ProjectIntervalToTheAxis returned no intervals");

            IPs integrationPoints;

            Dictionary<int, double> weightsDict = new Dictionary<int, double>(1024);
            int N = weightsList.Count;
            for (int i = 0; i < N; i++)
            {
                int idx = indecesList[i];
                double w = weightsList[i];
                if (weightsDict.ContainsKey(idx))
                    weightsDict[idx] += w;
                else
                    weightsDict.Add(idx, w);
                sumWeights += w;
            }
            int M = weightsDict.Count;
            double[] resultWeigths = new double[M];
            int[] resultIdx = new int[M];
            int iter = 0;
            double multipier = 1.0 / sumWeights;
            foreach (var item in weightsDict)
            {
                resultWeigths[iter] = item.Value * multipier;
                resultIdx[iter] = item.Key;
                iter++;
            }
            integrationPoints = new IPs { Indices = resultIdx, Weights = resultWeigths, BoundingIndices = new IndexBoundingBox { first = resultIdx.Min(), last = resultIdx.Max() } };
            
            Debug.Assert(Math.Abs(integrationPoints.Weights.Sum() - 1.0) < 1e-6, string.Format("the difference is {0}", Math.Abs(integrationPoints.Weights.Sum() - 1.0)));
            return integrationPoints;
        }

        public IndexBoundingBox GetBoundingBox(ITimeSegment t)
        {
            var intervals = GetTimeIntervals(t).Select(irvl => timeAxisProjection.ProjectIntervalToTheAxis(irvl)).ToArray();
            IndexBoundingBox boundingBox = new IndexBoundingBox();
            foreach (var interval in intervals)
            {
                var bb1 = weightsProvider.GetBoundingBox(grid, interval.Item1, interval.Item2);
                boundingBox = IndexBoundingBox.Union(boundingBox, bb1);
            }

            return boundingBox;
        }

       
        public double[] getAproximationGrid(ITimeSegment timeSegment)
        {
            //TODO: for now only a bounds of the region are returned as a approximation grid

            var doubleIntervals = GetTimeIntervals(timeSegment).Select(interval => timeAxisProjection.ProjectIntervalToTheAxis(interval)).ToArray();
            if (doubleIntervals.Length == 1 && doubleIntervals[0].Item1==doubleIntervals[0].Item2)
                return new double[] { doubleIntervals[0].Item1 };
            int N = doubleIntervals.Length * 2;
            int halfN = N / 2;
            double[] result = new double[N];
            for (int i = 0; i < halfN; i++)
            {
                result[i * 2] = doubleIntervals[i].Item1;
                result[i * 2 + 1] = doubleIntervals[i].Item2;
            }
            return result;
        }

        public double[] AxisValues
        {
            get { return grid; }
        }

        public DataCoverageResult GetCoverage(ITimeSegment t)
        {
            var doubleIntervals = GetTimeIntervals(t).Select(interval => timeAxisProjection.ProjectIntervalToTheAxis(interval)).ToArray();

            bool uncertatintyAvailable = true;

            foreach (var interval in doubleIntervals)
            {
                var coverage = coverageEvaluator.EvaluateInterval(grid, interval.Item1, interval.Item2);
                switch (coverage)
                {
                    case DataCoverageResult.OutOfData:
                        return DataCoverageResult.OutOfData;
                        break;
                    case DataCoverageResult.DataWithoutUncertainty:
                        uncertatintyAvailable = false;
                        break;
                    case DataCoverageResult.DataWithUncertainty:
                        //nothing here
                        break;
                    default: throw new NotSupportedException();
                }
            }
            return uncertatintyAvailable ? DataCoverageResult.DataWithUncertainty : DataCoverageResult.DataWithoutUncertainty;
        }
    }
}
