using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    /// <summary>
    /// Calculates the mode values for requested regions   
    /// </summary>
    /// <remarks>
    /// Helper component. Facade. Injects GridDefinitionAnalysis and ArrayMode into GridAggregator
    /// </remarks>
    public class GridModeAggregator : IBatchValueAggregator
    {
        private readonly IBatchValueAggregator component;
        private readonly GridDefinitionAnalysis metadata;

        public GridModeAggregator(IStorageContext storage, ITimeAxisStatisticsProcessing timeAxisIntegrator, ISpatGridModeCalculator latAxisIntegrator, ISpatGridModeCalculator lonAxisIntegrator, bool checkForMissingValues, string latAxisName = null, string lonAxisName = null)
        {
            this.metadata = new GridDefinitionAnalysis(storage.StorageDefinition, latAxisName, lonAxisName);
            IArrayAggregator meanValueAggregator = new ValueAggregators.ArrayMode(metadata,timeAxisIntegrator,latAxisIntegrator,lonAxisIntegrator,checkForMissingValues);
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

        public Task<double[]> AggregateCellsBatchAsync(IEnumerable<ICellRequest> cells)
        {
            return this.component.AggregateCellsBatchAsync(cells);
        }
    }
}
