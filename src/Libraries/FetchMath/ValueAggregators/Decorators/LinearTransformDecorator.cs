using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.ValueAggregators
{
    public class LinearTransformDecorator : IBatchValueAggregator
    {
        private readonly IBatchValueAggregator component;
        private readonly DataRepresentationDictionary dataRepresentationDictionary;
        private readonly Dictionary<string, Func<double, double>> additionalTransformsDict = new Dictionary<string, Func<double, double>>();

        public LinearTransformDecorator(IStorageContext context, IBatchValueAggregator component)
        {
            this.component = component;
            dataRepresentationDictionary = new DataRepresentationDictionary(context.StorageDefinition);
        }

        /// <summary>
        /// Sets the transform that is applied to the values in addition to (after) applying scale_factor/add_offset that is specified in the data
        /// </summary>
        /// <param name="variableName"></param>
        /// <param name="transform"></param>
        public void SetAdditionalTranform(string variableName, Func<double, double> transform)
        {
            additionalTransformsDict[variableName] = transform;
        }

        public async Task<double[]> AggregateCellsBatchAsync(IEnumerable<ICellRequest> cells)
        {
            ICellRequest first = cells.FirstOrDefault();
            if (first == null)
                return new double[0];
            else
            {
                string variableName = first.VariableName;
                Func<double, double> backStorageTransform = v => dataRepresentationDictionary.TransformToUsableForm(v, variableName);

                double[] componentResult = await component.AggregateCellsBatchAsync(cells);

                double[] backStorageTransformApplied = componentResult.Select(v => backStorageTransform(v)).ToArray();

                Func<double, double> additionalTransform = null;

                if (!additionalTransformsDict.TryGetValue(variableName, out additionalTransform))
                    return backStorageTransformApplied;
                else
                    return backStorageTransformApplied.Select(val => additionalTransform(val)).ToArray();
            }
        }
    }
}
