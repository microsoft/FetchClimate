using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    public class VariableDetails
    {
        string display;
        public VariableDetails(GetEnvVariablesResult v, IEnumerable<GetDataSourcesForVariableResult> dd)
        {
            var s = new System.Text.StringBuilder(v.ToString());
            foreach (var d in dd)
            {
                s.AppendLine().AppendFormat("    [{0}] {1}", d.ID, d.Name);
            }
            display = s.ToString();
        }
        public override string ToString()
        {
            return display;
        }
    }
}
