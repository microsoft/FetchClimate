using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Utils
{
    class HashBasedIEquatibleGeoCell : IGeoCellEquatible
    {
        private static HashBasedConverter converter = new HashBasedConverter();
        private readonly long hash;
        private readonly int hashCode;

        public HashBasedIEquatibleGeoCell(double latmin, double lonmin, double latmax, double lonmax, ITimeSegment time)
        {
            LatMax = latmax;
            LatMin = latmin;
            LonMax = lonmax;
            LonMin = lonmin;
            Time = time;

            double[] doubles = new double[] { latmin, latmax, lonmin, lonmax };
            int[] ints = new int[] { time.FirstDay, time.LastDay, time.FirstYear, time.LastYear, time.StartHour, time.StopHour };

            byte[] bytes = new byte[sizeof(double) * doubles.Length + sizeof(int) * ints.Length];

            int offset = 0;

            for (int i = 0; i < doubles.Length; i++)
            {
                byte[] b = BitConverter.GetBytes(doubles[i]);
                Buffer.BlockCopy(b, 0, bytes, offset, sizeof(double));
                offset += sizeof(double);
            }

            for (int i = 0; i < ints.Length; i++)
            {
                byte[] b = BitConverter.GetBytes(ints[i]);
                Buffer.BlockCopy(b, 0, bytes, offset, sizeof(int));
                offset += sizeof(int);
            }

            hash = SHA1Hash.HashAsync(bytes).Result;
            hashCode = BitConverter.ToInt32(BitConverter.GetBytes(hash), 0);
        }

        public double LatMin { get; private set; }
        public double LatMax { get; private set; }
        public double LonMin { get; private set; }
        public double LonMax { get; private set; }
        public ITimeSegment Time { get; private set; }

        public override int GetHashCode()
        {
            return hashCode;
        }

        bool IEquatable<IGeoCell>.Equals(IGeoCell other)
        {
            HashBasedIEquatibleGeoCell igcte = other as HashBasedIEquatibleGeoCell;
            if (igcte != null)
                igcte = new HashBasedIEquatibleGeoCell(other.LatMin, other.LonMin, other.LatMax, other.LonMax, other.Time);
            return igcte.hash == hash;
        }

        public override bool Equals(object obj)
        {
            IGeoCellEquatible tge = obj as IGeoCellEquatible;
            if (tge != null)
                return tge.Equals(this);
            else
                return base.Equals(obj);
        }
    }

    public class HashBasedConverter : IEquatibleGeoCellConverter
    {
        public IGeoCellEquatible Covert(IGeoCell cell)
        {
            return new HashBasedIEquatibleGeoCell(cell.LatMin, cell.LonMin, cell.LatMax, cell.LonMax, cell.Time);
        }
    }
}
