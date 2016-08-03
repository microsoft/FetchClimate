using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{   
    /// <summary>
    /// Returns the information based on the data storage definition
    /// </summary>
    public class GridDefinitionAnalysis : IGridDataSetMetaData
    {
        struct DimensionsOrder
        {
            public int LatDimNum { get; set; }
            public int LonDimNum { get; set; }
            public int TimeDimNum { get; set; }
        }

        private MissingValuesDictionary missingValuesDictionary = null;
        private Dictionary<string, DimensionsOrder> dimensionOrderDictioary = null;
        private readonly IReadOnlyDictionary<string, int> variableRankDict;

        public int GetVariableRank(string variableName) {
            return variableRankDict[variableName];
        }


        public int GetLatitudeDim(string variableName)
        {
            return dimensionOrderDictioary[variableName].LatDimNum;
        }

        public int GetLongitudeDim(string variableName)
        {
            return dimensionOrderDictioary[variableName].LonDimNum;
        }

        public int GetTimeDim(string variableName)
        {
            return dimensionOrderDictioary[variableName].TimeDimNum;
        }

        public object GetMissingValue(string variableName)
        {
            object missingValue = null;
            if (missingValuesDictionary.ContainsKey(variableName))
                missingValue = missingValuesDictionary[variableName];
            return missingValue;
        }
        public GridDefinitionAnalysis(IDataStorageDefinition storageDef, string latArrayName = null, string lonArrayName = null)
        {
            if (string.IsNullOrEmpty(latArrayName))
                latArrayName = IntegratorsFactoryHelpers.AutodetectLatName(storageDef);
            if (string.IsNullOrEmpty(lonArrayName))
                lonArrayName = IntegratorsFactoryHelpers.AutodetectLonName(storageDef);

            missingValuesDictionary = new MissingValuesDictionary(storageDef);
            dimensionOrderDictioary = new Dictionary<string, DimensionsOrder>();
            Dictionary<string, int> ranks = new Dictionary<string, int>();
            variableRankDict = ranks;            

            var dataVarsNames = storageDef.VariablesDimensions //looking schema for 2D and 3D data arrays
                .Where(def =>
                    (def.Value.Contains(storageDef.VariablesDimensions[latArrayName][0]) &&
                    def.Value.Contains(storageDef.VariablesDimensions[lonArrayName][0]) && (def.Key != latArrayName) && (def.Key != lonArrayName)))
                .Select(def => def.Key).ToArray();

            //analyzing data variables            
            foreach (var dataVarName in dataVarsNames)
            {
                //analyzing dimensions
                dimensionOrderDictioary[dataVarName] = DetermineDimensionsOrder(storageDef, latArrayName, lonArrayName, dataVarName);
                ranks[dataVarName] = storageDef.VariablesDimensions[dataVarName].Length;
            }
        }

        public void SetMissingValue(string variable, object missingValue)
        {
            missingValuesDictionary[variable] = missingValue;
        }

        /// <summary>
        /// Determines the dimensions order for the data array (2D or 3D)
        /// </summary>
        /// <param name="storageDefinition">a storage definition to check and to extract the scheme from</param>
        /// <param name="latName">The name of the array containing latitude values</param>
        /// <param name="lonName">The name of the array containing longitude values</param>
        /// <param name="dataName">The name of the data array to determine dimension order for</param>
        /// <returns></returns>
        private static DimensionsOrder DetermineDimensionsOrder(IDataStorageDefinition storageDefinition, string latName, string lonName, string dataName)
        {
            //checking persistence of lat and lon arrays
            if (!storageDefinition.VariablesDimensions.ContainsKey(latName))
                throw new ArgumentException("Latitude array (\"" + latName + "\") is not found");
            if (!storageDefinition.VariablesDimensions.ContainsKey(lonName))
                throw new ArgumentException("Longitude array (\"" + lonName + "\") is not found");

            if (storageDefinition.VariablesDimensions[latName].Length > 1)
                throw new ArgumentException("Latitude array (\"" + latName + "\") is not an axis. It is multidimensional. One dimensional array is expected");
            if (storageDefinition.VariablesDimensions[lonName].Length > 1)
                throw new ArgumentException("Latitude array (\"" + lonName + "\") is not an axis. It is multidimensional. One dimensional array is expected");

            string latDim = storageDefinition.VariablesDimensions[latName][0];
            string lonDim = storageDefinition.VariablesDimensions[lonName][0];

            DimensionsOrder order = new DimensionsOrder();
            order.TimeDimNum = -1; //if there is no time dim

            string[] dims = storageDefinition.VariablesDimensions[dataName];
            for (int i = 0; i < dims.Length; i++)
            {
                if (dims[i] == latDim)
                    order.LatDimNum = i;
                if (dims[i] == lonDim)
                    order.LonDimNum = i;
                if (dims[i] != latDim && dims[i] != lonDim)
                    order.TimeDimNum = i;
            }
            return order;
        }


    }
}
