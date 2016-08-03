using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Research.Science.FetchClimate2
{    
    public interface IWeightProvider
    {
        /// <summary>
        /// For a given grid returns weights that can be used to calculate an average data value on [min,max] interval.
        /// If averaging cannot be done, e.g. the requested range is out of the grid, an empty array (length = 0) must be returned.
        /// </summary>
        /// <remarks>
        /// If data corresponding to the <paramref name="grid"/> is in a <c>data</c> array, then averaging can be done in the following way:
        /// <code>
        /// int start;
        /// int stop;
        /// double[] w = GetWeights(grid, min, max, out start, out stop);
        /// double average = 0.;
        /// for (int i = start; i&lt;=stop; ++i) average += data[i]*w[i-start];
        /// </code>
        /// </remarks>
        /// <param name="grid">Coordinates of known data values.</param>
        /// <param name="min">Start of the interval to averag over.</param>
        /// <param name="max">End of the interval to average over.</param>
        /// <param name="start">An index of <paramref name="grid"/> corresponding to the first (index 0) element of result.</param>
        /// <param name="stop">An index of <paramref name="grid"/> corresponding to the last (index length-1) element of result.</param>
        /// <param name="dec">Defines accuracy of <paramref name="grid"/> values.</param>
        /// <returns></returns>
        double[] GetWeights(double[] grid, double min, double max, out int start, out int stop, DoubleEpsComparer dec = null);
        IndexBoundingBox GetBoundingBox(double[] grid, double min, double max, DoubleEpsComparer dec = null);
    }
}
