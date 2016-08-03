using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints.TimeSeries
{
    /// <summary>
    /// returns all knows stations
    /// </summary>    
    public class AllStationsStationLocator : IStationLocator
    {        
        readonly int[] results;

        public AllStationsStationLocator(int stationCount)
        {
            results = Enumerable.Range(0, stationCount).ToArray();
        }

        public int[] GetRelevantStationsIndices(IGeoCell cell)
        {
            return results;
        }
    }
}
