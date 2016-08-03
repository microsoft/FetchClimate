using Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints.LinearCombination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Utils
{
    public interface IAsyncMap<TArg,TRes>
    {
        Task<TRes> GetAsync(TArg cell);
    }

    public interface IAsyncMapFactory<TArg, TRes>
    {
        Task<IAsyncMap<TArg,TRes>> CreateAsync();
    }
}
