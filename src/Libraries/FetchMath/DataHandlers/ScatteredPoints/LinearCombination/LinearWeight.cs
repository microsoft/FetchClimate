using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints.LinearCombination
{
    public struct LinearWeight
    {
        public LinearWeight(int dataIndex, double weight)
        {
            Weight = weight;
            DataIndex = dataIndex;
        }

        /// <summary>
        /// A normailzed weight that can be used in linear combination to get interpolated value
        /// </summary>
        public double Weight;

        /// <summary>
        /// An index of the data array, data for which can be retrived to use in linear combination (with corresponding weight) to get interpolated value
        /// </summary>
        public int DataIndex;
    }
    
}
