using Microsoft.Research.Science.FetchClimate2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlersTests
{
    internal class RequestStubs : ICellRequest
    {
        public RequestStubs()
        { }

        public string VariableName
        {
            get;
            set;
        }

        public double LatMin
        {
            get;
            set;
        }

        public double LonMin
        {
            get;
            set;
        }

        public double LatMax
        {
            get;
            set;
        }

        public double LonMax
        {
            get;
            set;
        }

        public ITimeSegment Time
        {
            get;
            set;
        }

        public override bool Equals(object obj)
        {
            ICellRequest snd = obj as ICellRequest;
            if (snd == null)
                return base.Equals(obj);
            else
                return LatMax == snd.LatMax && LonMax == snd.LonMax && LatMin == snd.LatMin && LonMin == snd.LonMin && VariableName == snd.VariableName;
        }

        public override int GetHashCode()
        {
            return LatMin.GetHashCode() ^ (LatMax.GetHashCode() << 1) ^ (LonMin.GetHashCode()<<2) ^ (LonMax.GetHashCode() <<3) ^ VariableName.GetHashCode();
        }
    }

    public class FetchResponse : IFetchResponse
    {
        private readonly Array values;
        private readonly Array uncertainty;
        private readonly IFetchRequest request;

        public FetchResponse(IFetchRequest request, Array values, Array uncertainty)
        {
            this.request = request;
            this.values = values;
            this.uncertainty = uncertainty;
        }

        public Array Values
        {
            get { return values; }
        }

        public Array Uncertainty
        {
            get { return uncertainty; }
        }

        public IFetchRequest Request
        {
            get { return request; }
        }
    }

    public class FetchResponseWithProvenance : FetchResponse, IFetchResponseWithProvenance
    {
        private readonly Array provenance;

        public FetchResponseWithProvenance(IFetchRequest request, Array values, Array uncertainty, Array provenance) :
            base(request, values, uncertainty)
        {
            this.provenance = provenance;
        }

        public Array Provenance
        {
            get { return provenance; }
        }
    }
}
