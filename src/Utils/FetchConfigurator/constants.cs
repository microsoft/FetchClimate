using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    public class SharedConstants
    {
        //Users local storage path
        public static readonly string FcFolderPath;
        public static readonly string LocalConfigurationConnectionString;

        static SharedConstants()
        {
            FcFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FetchClimate2");

            LocalConfigurationConnectionString = String.Format(
                @"Data Source=(localdb)\v11.0;Integrated Security=True;Connect Timeout=15;Encrypt=False;TrustServerCertificate=False;database=FetchClimate2Configuration");
        }
    }
}
