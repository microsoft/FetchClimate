using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    public class DataRepresentationDictionary
    {
        Dictionary<string, double> scaleFactors = new Dictionary<string, double>();
        Dictionary<string, double> addOffsets = new Dictionary<string, double>();

        public Dictionary<string, double> ScaleFactors { get { return scaleFactors; } }
        public Dictionary<string, double> AddOffsets { get { return addOffsets; } }

        public DataRepresentationDictionary(IDataStorageDefinition definition)
        {
            foreach (var var in definition.VariablesMetadata)
            {
                string name = var.Key;
                if (var.Value.ContainsKey("scale_factor"))
                    scaleFactors.Add(name, Convert.ToDouble(var.Value["scale_factor"], CultureInfo.InvariantCulture));
                else
                    scaleFactors.Add(name, 1.0);
                if (var.Value.ContainsKey("add_offset"))
                    addOffsets.Add(name, Convert.ToDouble(var.Value["add_offset"], CultureInfo.InvariantCulture));
                else
                    addOffsets.Add(name, 0.0);
            }
        }

        public DataRepresentationDictionary()
        { }


        /// <summary>
        /// Applies scaleFactor/addOffset transformation
        /// </summary>
        /// <param name="value"></param>
        /// <param name="variable"></param>
        /// <returns></returns>
        public double TransformToUsableForm(double value, string variable)
        {
            return value * scaleFactors[variable] + addOffsets[variable];
        }
    }
}

