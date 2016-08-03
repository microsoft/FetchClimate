using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Utils
{
    /// <summary>
    /// Cells are considered the same when its time segments are the same
    /// </summary>
    class HashBasedTimeSegmentOnlyIEquatibleGeoCell : IGeoCellEquatible
    {
        private static HashBasedTimeSegmentOnlyConverter converter = new HashBasedTimeSegmentOnlyConverter();
        private readonly long hash;
        private readonly int hashCode;

        public HashBasedTimeSegmentOnlyIEquatibleGeoCell(double latmin, double lonmin, double latmax, double lonmax, ITimeSegment time)
        {
            LatMax = latmax;
            LatMin = latmin;
            LonMax = lonmax;
            LonMin = lonmin;
            Time = time;
            
           
            int[] ints = new int[] { time.FirstDay, time.LastDay, time.FirstYear, time.LastYear, time.StartHour, time.StopHour };

            byte[] bytes = new byte[ sizeof(int) * ints.Length];

            int offset = 0;
           
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
            HashBasedTimeSegmentOnlyIEquatibleGeoCell igcte = other as HashBasedTimeSegmentOnlyIEquatibleGeoCell;
            if (igcte != null)
                igcte = new HashBasedTimeSegmentOnlyIEquatibleGeoCell(other.LatMin, other.LonMin, other.LatMax, other.LonMax, other.Time);
            return igcte.hash == hash;
        }

        public override bool Equals(object obj)
        {
            IGeoCellEquatible igc = obj as IGeoCellEquatible;
            if (igc != null)
                return igc.Equals(this);
            else
                return base.Equals(obj);
        }
    }

    public class HashBasedTimeSegmentOnlyConverter : IEquatibleGeoCellConverter
    {
        public IGeoCellEquatible Covert(IGeoCell cell)
        {
            return new HashBasedTimeSegmentOnlyIEquatibleGeoCell(cell.LatMin, cell.LonMin, cell.LatMax, cell.LonMax, cell.Time);
        }
    }
}
