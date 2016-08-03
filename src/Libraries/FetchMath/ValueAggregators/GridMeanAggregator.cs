using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.ValueAggregators
{
    /// <summary>
    /// Calculates the mean values for requested regions   
    /// </summary>
    /// <remarks>
    /// Helper component. Facade. Injects GridDefinitionAnalysis and ArrayIntegrator into GridAggregator
    /// </remarks>
    public class GridMeanAggregator : IBatchValueAggregator
    {
        private readonly IBatchValueAggregator component;
        private readonly GridDefinitionAnalysis metadata;

        public GridMeanAggregator(IStorageContext storage, ITimeAxisIntegrator timeAxisIntegrator, ISpatGridIntegrator latAxisIntegrator, ISpatGridIntegrator lonAxisIntegrator,bool checkForMissingValues,string latAxisName=null,string lonAxisName=null)
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

        public Task<double[]> AggregateCellsBatchAsync(IEnumerable<ICellRequest> cells)
        {
            return this.component.AggregateCellsBatchAsync(cells);
        }
    }
}
