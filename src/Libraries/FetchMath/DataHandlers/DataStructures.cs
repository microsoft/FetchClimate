using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{    
    public interface IGeoCell
    {
        double LatMin {get;}
        double LonMin {get;}
        double LatMax {get;}
        double LonMax {get;}
        ITimeSegment Time {get;}
    }

    public interface ICellRequest : IGeoCell
    {
        string VariableName { get; }
    }

    internal class GeoCell : IGeoCell
    {
        //public GeoCell(double latMin, double latMax, double lonMin, double lonMax, ITimeSegment timeSegment)
        //{
        //    this.latMin = latMin;
        //    this.latMax = latMax;
        //    this.lonMin = lonMin;
        //    this.lonMax = lonMax;
        //    this.time = timeSegment;
        //}

        public double LatMin;
        public double LonMin;
        public double LatMax;
        public double LonMax;
        public ITimeSegment Time;

        double IGeoCell.LatMin
        {
            get { return LatMin; }
        }

        double IGeoCell.LonMin
        {
            get { return LonMin; }
        }

        double IGeoCell.LatMax
        {
            get { return LatMax; }
        }

        double IGeoCell.LonMax
        {
            get { return LonMax; }
        }

        ITimeSegment IGeoCell.Time
        {
            get { return Time; }
        }

        public override bool Equals(object obj)
        {
            IGeoCell snd = obj as IGeoCell;
            if (snd != null)
                return (LatMin == snd.LatMin) && (LatMax == snd.LatMax) && (LonMin == snd.LonMin) && (LonMax == snd.LonMax) && (Time.Equals(snd.Time));
            else
                return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return (LatMin.GetHashCode() ^ (LatMax.GetHashCode() << 1) ^ (LonMin.GetHashCode() << 2) ^ (LonMax.GetHashCode() << 3) ^ Time.GetHashCode());
        }
    }

    internal class CellRequest : GeoCell, ICellRequest
    {
        public readonly string VariableName;

        string ICellRequest.VariableName
        {
            get { return VariableName; }            
        }

        public override bool Equals(object obj)
        {
            ICellRequest snd = obj as ICellRequest;
            if (snd != null)
                return VariableName == snd.VariableName && base.Equals(obj);
            else
                return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ VariableName.GetHashCode();
        }
    }

    internal class NameAnnotatedGeoCell : ICellRequest
    {
        private readonly IGeoCell geoCell;
        private readonly string variableName;

        public NameAnnotatedGeoCell(IGeoCell geoCell, string variableName)
        {
            this.geoCell = geoCell;
            this.variableName = variableName;
        }

        public string VariableName
        {
            get { return variableName; }
        }

        public double LatMin
        {
            get { return geoCell.LatMin; }
        }

        public double LonMin
        {
            get { return geoCell.LonMin; }
        }

        public double LatMax
        {
            get { return geoCell.LatMax; }
        }

        public double LonMax
        {
            get { return geoCell.LonMax; }
        }

        public ITimeSegment Time
        {
            get { return geoCell.Time; }
        }

        public override bool Equals(object obj)
        {
            NameAnnotatedGeoCell snd = obj as NameAnnotatedGeoCell;
            if (snd != null)
                return geoCell.Equals(snd) && (VariableName == snd.VariableName);
            else
                return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return geoCell.GetHashCode() ^ variableName.GetHashCode();
        }
    }
}
