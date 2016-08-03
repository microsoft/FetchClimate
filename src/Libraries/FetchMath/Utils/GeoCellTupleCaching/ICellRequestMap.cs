using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Utils
{
    public interface ICellRequestMap<T>
    {
        Task<T> GetAsync(ICellRequest cell);
    }

    public interface ICellRequestMapFactory<T>
    {
        Task<ICellRequestMap<T>> CreateAsync();
    }
}
