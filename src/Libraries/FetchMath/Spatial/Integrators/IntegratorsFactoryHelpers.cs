using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    public class IntegratorsFactoryHelpers
    {
        /// <summary>
        /// Tries to find an array with one of the specified possible names or with possible substring. Returns null if nothing found
        /// </summary>
        /// <param name="storageDefinition">a storage definition to find the axes names in</param>
        /// <param name="latName">an out parameter to output found latitude array name</param>
        /// <param name="lonName">an out parameter to output found longitude array name</param>
        /// <returns></returns>
        private static string DetectArrayName(IDataStorageDefinition storageDefinition, string[] exactNamesToFind, string[] namePartsToFind)
        {
            var oneDimVars = storageDefinition.VariablesDimensions.Where(v => v.Value.Length == 1).Select(v => v.Key);

            var foundName = oneDimVars.FirstOrDefault(v => exactNamesToFind.Contains(v)); //exact match
            if (foundName == null)
                foundName = oneDimVars.FirstOrDefault(v => namePartsToFind.Any(part => v.Contains(part))); //substring only
            return foundName;
        }

        static readonly string[] possibleLatNames = new string[] { "Lat", "Lats", "LAT", "LATS", "lat", "lats", "Latitude", "latitude" };
        static readonly string[] possibleLonNames = new string[] { "Lon", "Lons", "LON", "LONS", "lon", "lons", "Longitude", "longitude" };

        public static string AutodetectLatName(IDataStorageDefinition storageDefinition)
        {
            var latArrayName = DetectArrayName(storageDefinition,
                    possibleLatNames,
                    possibleLatNames);
            if (string.IsNullOrEmpty(latArrayName))
                throw new InvalidOperationException("can't auto detect latitude array name");
            return latArrayName;
        }

        public static string AutodetectLonName(IDataStorageDefinition storageDefinition)
        {
            var lonArrayName = DetectArrayName(storageDefinition,
                    possibleLonNames,
                    possibleLonNames);
            if (string.IsNullOrEmpty(lonArrayName))
                throw new InvalidOperationException("can't auto detect longitude array name");
            return lonArrayName;
        }

        public static Tuple<string, string> AutodetectLatLonNames(IDataStorageDefinition storageDefinition)
        {
            for (int i = 0; i < possibleLatNames.Length; i++)
            {
                string[] latDim;
                string[] lonDim;
                if (storageDefinition.VariablesDimensions.TryGetValue(possibleLatNames[i], out latDim) &&
                   storageDefinition.VariablesDimensions.TryGetValue(possibleLonNames[i], out lonDim) &&
                   latDim.Length == 1 && lonDim.Length == 1 && latDim[0] == lonDim[0])
                    return new Tuple<string, string>(possibleLatNames[i], possibleLonNames[i]);
            }
            throw new InvalidOperationException("Cannot auto detect pair of latitude and longitude variables");
        }

    }
}
