using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{

    public interface IFetchResponseWithProvenance : IFetchResponse
    {
        Array Provenance
        {
            get;
        }
    }

    public interface IFetchEngine 
    {
        /// <summary>Returns task resulting in data set uri with request & response</summary>
        Task<IFetchResponseWithProvenance> PerformRequestAsync(IFetchRequest request);        
    }
}
