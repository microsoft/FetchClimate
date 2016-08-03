using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Research.Science.FetchClimate2;
using Microsoft.Research.Science.FetchClimate2.ValueAggregators;

namespace FutureClimate
{
    /// <summary>
    /// Calculates the mean values for requested regions   
    /// </summary>
    /// <remarks>
    /// Helper component. Facade. Injects GridDefinitionAnalysis and ArrayIntegrator into GridAggregator
    /// </remarks>
    public class PercentileGridAggregator : IBatchUncertaintyEvaluator
    {
        private readonly IBatchValueAggregator component;
        private readonly GridDefinitionAnalysis metadata;

        public PercentileGridAggregator(IStorageContext storage, ITimeAxisIntegrator timeAxisIntegrator, ISpatGridIntegrator latAxisIntegrator, ISpatGridIntegrator lonAxisIntegrator,bool checkForMissingValues,string latAxisName=null,string lonAxisName=null)
        {
            this.metadata = new GridDefinitionAnalysis(storage.StorageDefinition, latAxisName, lonAxisName);
            IArrayAggregator meanValueAggregator = new ArrayMean(metadata,timeAxisIntegrator,latAxisIntegrator,lonAxisIntegrator,checkForMissingValues);
            this.component = new GridAggregator(storage, metadata, meanValueAggregator, timeAxisIntegrator, latAxisIntegrator, lonAxisIntegrator);
        }

        /// <summary>
        /// Sets overrides the missing value specified in the data set
        /// </summary>
        /// <param name="variableName"></param>
        /// <param name="value"></param>
        public void SetMissingValue(string variableName, object value)
        {
            this.metadata.SetMissingValue(variableName, value);
        }

        public async Task<double[]> EvaluateCellsBatchAsync(IEnumerable<ICellRequest> cells)
        {
            var cells5 = cells.Select(cell => new SuffixCell("P5", cell));
            var cells95 = cells.Select(cell => new SuffixCell("P95", cell));
            var pp = await Task.WhenAll(
                this.component.AggregateCellsBatchAsync(cells5),
                this.component.AggregateCellsBatchAsync(cells95));
            return pp[0].Zip(pp[1], (p5,p95) => 0.25 * (p95 - p5)).ToArray();
        }
    }

    /// <summary>
    /// Decorator of ICellRequest that adds suffix to the decorated variable name
    /// </summary>
    class SuffixCell : ICellRequest
    {
        private string _suffix;
        private ICellRequest _cell;
        public SuffixCell(string suffix, ICellRequest cell) { _suffix = suffix; _cell = cell; }
        public string VariableName
        {
            get { return _cell.VariableName + _suffix; }
        }

        public double LatMin
        {
            get { return _cell.LatMin; }
        }

        public double LonMin
        {
            get { return _cell.LonMin; }
        }

        public double LatMax
        {
            get { return _cell.LatMax; }
        }

        public double LonMax
        {
            get { return _cell.LonMax; }
        }

        public ITimeSegment Time
        {
            get { return _cell.Time; }
        }
    }

}
