using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
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

    public class FetchResponseWithProvenance : FetchResponse , IFetchResponseWithProvenance
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
