using Microsoft.Research.Science.FetchClimate2.DataMaskProviders;
using Microsoft.Research.Science.FetchClimate2.Integrators;
using Microsoft.Research.Science.FetchClimate2.Integrators.Temporal;
using Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators;
using Microsoft.Research.Science.FetchClimate2.ValueAggregators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.Research.Science.FetchClimate2.TimeAxisStatisticsProcessing
{
    /// <summary>
    /// Works with ascending axis. The values are offsets from some baseTime value
    /// </summary>
    /// <typeparam name="WeightsProvider"></typeparam>
    public class TimeAxisValueGrouppingFacade : ContinousTimeAxis, ITimeAxisStatisticsProcessing
    {
        private readonly ITimeAxisProjection timeAxisProjection;
        private readonly IDataMaskProvider dataMaskProvider;
        protected readonly IDataCoverageEvaluator coverageEvaluator;

        public TimeAxisValueGrouppingFacade(Array axis, ITimeAxisProjection timeAxisProjection, IDataMaskProvider dataMaskProvider, IDataCoverageEvaluator coverageEvaluator)
            : base(axis)
        {
            this.timeAxisProjection = timeAxisProjection;
            this.dataMaskProvider = dataMaskProvider;
            this.coverageEvaluator = coverageEvaluator;
        }

        public int[] GetDataIndices(ITimeSegment t)
        {
            var intervals = GetTimeIntervals(t);
            var projections = intervals.Select(a => timeAxisProjection.ProjectIntervalToTheAxis(a));
            HashSet<int> indices = new HashSet<int>();            
            foreach (var projection in projections)
            {
                int[] indices1 = dataMaskProvider.GetIndices(grid, projection.Item1, projection.Item2);
                int len = indices1.Length;
                for (int i = 0; i < len; i++)			
                    indices.Add(indices1[i]);			
            }

            return indices.ToArray();
        }

        public IndexBoundingBox GetBoundingBox(ITimeSegment t)
        {
            var intervals = GetTimeIntervals(t).Select(irvl => timeAxisProjection.ProjectIntervalToTheAxis(irvl)).ToArray();
            IndexBoundingBox boundingBox = new IndexBoundingBox();
            foreach (var interval in intervals)
            {
                var bb1 = dataMaskProvider.GetBoundingBox(grid, interval.Item1, interval.Item2);
                boundingBox = IndexBoundingBox.Union(boundingBox, bb1);
            }

            return boundingBox;
        }

        public DataCoverageResult GetCoverage(ITimeSegment t)
        {
            var test = GetBoundingBox(t);
            if (test.IsSingular)
                return DataCoverageResult.OutOfData;
            else
                return DataCoverageResult.DataWithoutUncertainty;
        }
    }
}
