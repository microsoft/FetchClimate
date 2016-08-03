using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators
{
    /// <summary>
    /// Check that the request contains variable name that is present in the storage
    /// Throws invalid operation exception if it is not found in the storage
    /// </summary>
    public class VariablePresenceCheckDecorator : IBatchUncertaintyEvaluator
    {
        private readonly IBatchUncertaintyEvaluator component;
        private readonly IDataStorageDefinition storageDef;

        public VariablePresenceCheckDecorator(IDataStorageDefinition storageDef, IBatchUncertaintyEvaluator component)
        {
            this.component = component;
            this.storageDef = storageDef;
        }

        public async Task<double[]> EvaluateCellsBatchAsync(IEnumerable<ICellRequest> cells)
        {
            ICellRequest first = cells.FirstOrDefault();
            if (first == null)
                return new double[0];
            else
            {
                string varName = first.VariableName;
                if (!storageDef.VariablesDimensions.ContainsKey(varName))
                    throw new InvalidOperationException(string.Format("Request to the variable \"{0}\" that is not found in the data storage. Check the variable name mapping in the FetchClimate configuration", varName));
                return await component.EvaluateCellsBatchAsync(cells);
            }
        }
    }
}
