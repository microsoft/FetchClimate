using Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Microsoft.Research.Science.FetchClimate2
{
    public class EmbeddedResourceBitMaskProvider : IBitMaskProvider
    {
        DataMaskAnalyzer mask;

        public EmbeddedResourceBitMaskProvider(Type dataMaskAssemblyType, string dataMaskResourceName)
        {
            using (Stream stream = Assembly.GetAssembly(dataMaskAssemblyType).GetManifestResourceStream(dataMaskResourceName))
            {
                using (GZipStream dataStream = new GZipStream(stream, CompressionMode.Decompress, false))
                {
                    mask = new DataMaskAnalyzer(dataStream);
                }
            }
        }

        public bool HasData(double lat, double lon)
        {
            return mask.HasData(lat, lon);
        }

        public double GetDataPercentage(double latmin, double latmax, double lonmin, double lonmax)
        {
            return mask.GetDataPercentage(latmin,latmax,lonmin,lonmax);
        }
    }
}
