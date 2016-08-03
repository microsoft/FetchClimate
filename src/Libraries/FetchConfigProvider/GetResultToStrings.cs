using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    public partial class GetEnvVariablesResult
    {
        public override string ToString()
        {
            return String.Format("{0}: {1} ({2})", _DisplayName, _Description, _Units);
        }
    }

    public partial class GetDataSourcesResult
    {
        public override string ToString()
        {
            return String.Format("[{0}] {1}", _ID, _Name);
        }

        public string ToLongString()
        {
            return String.Format("[{2}] {0}: {1}\n\t{3}\n\tURI: {4}\n\t{5}", _Name, _Description, _ID, _Copyright, _Uri, _RemoteID == null ? "Handler: " + _FullClrTypeName : "Remote name: " + _RemoteName);
        }
    }
}
