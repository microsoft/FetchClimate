using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Utils
{
    /// <summary>
    /// Transforms a comonent into a hash based caching factory of components.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CellRequestMapCachingFactory<T> : ICellRequestMapFactory<T>
    {
        private readonly ICellRequestMap<T> component;
        private readonly IEquatibleGeoCellConverter converter;

        /// <summary>
        /// Transforms a comonent into a caching factory of components 
        /// </summary>
        /// <param name="component">Component to decorate with hash based caching</param>
        public CellRequestMapCachingFactory(ICellRequestMap<T> component, IEquatibleGeoCellConverter converter)
        {
            this.component = component;
            this.converter = converter;
        }

        public async Task<ICellRequestMap<T>> CreateAsync()
        {
            return new CellRequestMapCacheDecorator<T>(converter, component);
        }
    }
}
