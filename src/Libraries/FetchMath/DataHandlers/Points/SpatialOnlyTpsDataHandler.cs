using Microsoft.Research.Science.FetchClimate2.Integrators.Temporal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.DataHandlers
{
    public class SpatialOnlyTpsDataHandler : EuclideanTpsDataHandler
    {        
        public SpatialOnlyTpsDataHandler(IStorageContext context)
            : base(context,true, ObservationProviders.NearestN_NoTimeOP.ConstructAsync(context).Result)
        { }
    }
}
