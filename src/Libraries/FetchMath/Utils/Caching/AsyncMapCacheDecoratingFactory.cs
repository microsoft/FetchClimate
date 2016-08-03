using Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints.LinearCombination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Utils
{
    /// <summary>
    /// Decorates a component with caching decorator.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AsyncMapCacheDecoratingFactory<TArg, TRes> : IAsyncMapFactory<TArg, TRes>
    {
        private readonly IAsyncMap<TArg, TRes> component;
        private readonly IEquatableConverter<TArg> converter;

        
        /// <summary>
        /// Transforms a comonent into a caching factory of components 
        /// </summary>
        /// <param name="component">Component to decorate with hash based caching</param>
        public AsyncMapCacheDecoratingFactory(IEquatableConverter<TArg> converter,IAsyncMap<TArg, TRes> component)
        {
            if (converter == null)
                throw new ArgumentNullException("converter");
            if (component == null)
                throw new ArgumentNullException("component");
            this.component = component;
            this.converter = converter;
        }

        public async Task<IAsyncMap<TArg, TRes>> CreateAsync()
        {
            return new AsyncMapCacheDecorator<TArg, TRes>(converter, component);
        }
    }
}
