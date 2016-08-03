using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    public static class TimeAxisAutodetection
    {
        private static readonly string[] possibleTimeVarNames = new string[] { "hours", "hour", "days", "day", "seconds", "second", "minutes", "minute", "year", "years", "time", "t", "month", "months" };
        private static readonly string[] possibleComplexNames = null;

        static TimeAxisAutodetection()
        {
            possibleComplexNames = possibleTimeVarNames.Where(n => n != "t").ToArray();
        }

        /// <summary>
        /// Returns null if not found
        /// </summary>
        /// <param name="storageDefinition"></param>
        /// <returns></returns>
        public static string GetTimeDimension(IDataStorageDefinition storageDefinition)
        {
            var dict = storageDefinition.VariablesMetadata;
            var dimDict = storageDefinition.VariablesDimensions;
            string timeDimName = storageDefinition.VariablesDimensions.Where(pair => possibleTimeVarNames.Contains(pair.Key.ToLower()) && pair.Value.Length == 1).Select(pair => pair.Value[0]).FirstOrDefault();

            if (timeDimName == null) //checking dimension name itself
                timeDimName = storageDefinition.DimensionsLengths.Keys.FirstOrDefault(name => possibleTimeVarNames.Contains(name.ToLower()));

            if (timeDimName == null) //checking the var name contains a possible name as a part
                timeDimName = storageDefinition.VariablesDimensions.Where(pair => possibleComplexNames.Any(posName => pair.Key.ToLower().Contains(posName)) && pair.Value.Length == 1).Select(pair => pair.Value[0]).FirstOrDefault();

            if (timeDimName == null) //checking the var dimension contains a possible name as a part
                timeDimName = storageDefinition.DimensionsLengths.Keys.FirstOrDefault(name => possibleComplexNames.Any(posName => name.ToLower().Contains(posName)));

            return timeDimName;
        }

        /// <summary>
        /// returns null if not found
        /// </summary>
        /// <param name="storageDefinition"></param>
        /// <returns></returns>
        public static string GetTimeVariableName(IDataStorageDefinition storageDefinition)
        {
            var timeDimName = GetTimeDimension(storageDefinition);
            var foundVars = storageDefinition.VariablesDimensions.Where(p => p.Value.Length == 1 && p.Value[0] == timeDimName).ToArray();
            if (foundVars.Length == 0)
                return null;
            else
                return foundVars[0].Key;
        }

        public static async Task<Array> GetTimeAxisAsync(IStorageContext context)
        {
            string varName = GetTimeVariableName(context.StorageDefinition);
            if(string.IsNullOrEmpty(varName))
                throw new InvalidOperationException("Can't auto detect time axis");
            return await context.GetDataAsync(varName);
        }
    }
}
