using Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints.LinearCombination;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Utils
{
    public interface IEquatableConverter<T>
    {
        IEquatable<T> Covert(T obj);
    }

    public class AsyncMapCacheDecorator<TArg, TRes> : IAsyncMap<TArg, TRes>
    {
        private readonly ConcurrentDictionary<System.IEquatable<TArg>, AsyncLazy<TRes>> cache = new ConcurrentDictionary<IEquatable<TArg>, AsyncLazy<TRes>>();
        private readonly IEquatableConverter<TArg> converter;
        private readonly IAsyncMap<TArg, TRes> component;

        public AsyncMapCacheDecorator(IEquatableConverter<TArg> converter, IAsyncMap<TArg, TRes> component)
        {
            if (converter == null)
                throw new ArgumentNullException("converter");
            if (component == null)
                throw new ArgumentNullException("component");
            this.converter = converter;
            this.component = component;
        }

        public async Task<TRes> GetAsync(TArg obj)
        {
            IEquatable<TArg> ine = obj as IEquatable<TArg>;
            if (ine == null)
                ine = converter.Covert(obj);

            var lazyResult = cache.GetOrAdd(ine, new AsyncLazy<TRes>(async () =>
            {
                TRes newResult = await component.GetAsync(obj);
                return newResult;
            }));

            var result = await lazyResult.GetValueAsync();

            return result;
        }
    }
}
