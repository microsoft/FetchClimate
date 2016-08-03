using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Utils
{
    public interface IGeoCellEquatible : IGeoCell, System.IEquatable<IGeoCell>
    { }

    /// <summary>
    /// Converts IGeoCell into IEquatibleGeoCell
    /// </summary>
    public interface IEquatibleGeoCellConverter
    {
        IGeoCellEquatible Covert(IGeoCell nodes);
    }

    public class CellRequestMapCacheDecorator<T> : ICellRequestMap<T>
    {
        private readonly ConcurrentDictionary<System.IEquatable<IGeoCell>, AsyncLazy<T>> cache = new ConcurrentDictionary<IEquatable<IGeoCell>, AsyncLazy<T>>();
        private readonly IEquatibleGeoCellConverter converter;
        private readonly ICellRequestMap<T> component;

        public CellRequestMapCacheDecorator(IEquatibleGeoCellConverter converter, ICellRequestMap<T> component)
        {
            this.converter = converter;
            this.component = component;
        }

        public async Task<T> GetAsync(ICellRequest nodes)
        {
            IGeoCellEquatible ine = nodes as IGeoCellEquatible;
            if (ine == null)
                ine = converter.Covert(nodes);

            var lazyResult = cache.GetOrAdd(ine, new AsyncLazy<T>(async () =>
            {
                T newResult = await component.GetAsync(nodes);
                return newResult;
            }));

            var result = await lazyResult.GetValueAsync();

            return result;
        }
    }
}
