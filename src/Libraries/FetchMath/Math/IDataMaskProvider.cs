using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Research.Science.FetchClimate2
{    
    /// <summary>
    /// used to highlight the position of data elements corresponding to user requsted area (e.g. for mode, median etc.)
    /// </summary>
    public interface IDataMaskProvider
    {
        /// <summary>
        /// If the requested range is out of the grid, empty array (length = 0) must be returned
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <param name="dec"></param>
        /// <returns></returns>
        int[] GetIndices(double[] grid, double min, double max, DoubleEpsComparer dec = null);
        IndexBoundingBox GetBoundingBox(double[] grid, double min, double max, DoubleEpsComparer dec = null);
    }
}
