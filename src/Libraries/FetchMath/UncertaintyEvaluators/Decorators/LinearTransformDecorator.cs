using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators
{
    /// <summary>
    /// Applies the transform to the results produced by the component (transform set through SetTranform)
    /// </summary>
    public class LinearTransformDecorator : IBatchUncertaintyEvaluator
    {
        private IBatchUncertaintyEvaluator component;
        protected readonly Dictionary<string, Func<double, double>> transformsDict = new Dictionary<string, Func<double, double>>();

        public LinearTransformDecorator(IBatchUncertaintyEvaluator component)
        {
            this.component = component;
        }

        /// <summary>
        /// Sets the transform that is applied to the values
        /// </summary>
        /// <param name="variableName"></param>
        /// <param name="transform"></param>
        public void SetTranform(string variableName, Func<double, double> transform)
        {
            transformsDict[variableName] = transform;
        }

        public async Task<double[]> EvaluateCellsBatchAsync(IEnumerable<ICellRequest> cells)
        {
            ICellRequest first = cells.FirstOrDefault();
            if (first == null)
                return new double[0];
            else
            {
                var name = first.VariableName;
                double[] componentResult = await component.EvaluateCellsBatchAsync(cells);

                Func<double, double> transform = null;

                if (!transformsDict.TryGetValue(name, out transform))
                    return componentResult;
                else
                    return componentResult.Select(val => transform(val)).ToArray();
            }

        }
    }
}
