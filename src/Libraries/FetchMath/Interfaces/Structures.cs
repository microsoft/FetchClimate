using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{           
    /// <summary>
    /// Integration points. The mean value for some region can be calculated by summing all of the weights multiplied by the function value at corresponding points and then dividing the sum by the sum of weights. e.g. (Sum i weight[i]*data[i])/(sum i weight[i])
    /// </summary>
    public class IPs
    {
        /// <summary>
        /// A weights to use during mean values calculation. e.g. (Sum i weight[i]*data[i])
        /// </summary>
        public double[] Weights;

        /// <summary>
        /// A corresponding indeces to get the data for. e.g (Sum i weight[i]*data[i])
        /// </summary>
        public int[] Indices;

        /// <summary>
        /// A bounding box for indeces needed to calculate the mean value
        /// </summary>
        public IndexBoundingBox BoundingIndices;
    }
}
