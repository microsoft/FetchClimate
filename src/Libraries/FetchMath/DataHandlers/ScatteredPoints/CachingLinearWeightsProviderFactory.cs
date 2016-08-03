using Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints.LinearCombination;
using Microsoft.Research.Science.FetchClimate2.Utils;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints
{
    /// <summary>
    /// This class performs partial dependency injection.
    /// It assembles a series of converters and caching interpolation context decorator
    /// </summary>
    public class CachingLinearWeightsProviderFactory<TContext> : IScatteredPointsLinearInterpolatorOnSphereFactory
    {
        private readonly IScatteredPointContextBasedLinearWeightProviderOnSphere<TContext> weightsProvider;
        private readonly AsyncMapCacheDecoratingFactory<INodes,TContext> contextProvidingFactory;

        private static HashBasedEquatibleINodesConverter converter = new HashBasedEquatibleINodesConverter();


        public CachingLinearWeightsProviderFactory(IScatteredPointContextBasedLinearWeightProviderOnSphere<TContext> weightsProvider, IAsyncMap<INodes,TContext> contextProvider)
        {
            this.weightsProvider = weightsProvider;
            this.contextProvidingFactory = new AsyncMapCacheDecoratingFactory<INodes,TContext>(converter,contextProvider);
        }

        public async Task<IScatteredPointsLinearInterpolatorOnSphere> CreateAsync()
        {
            var cachingDecorator = await contextProvidingFactory.CreateAsync();
            var adapter = new CellRequestToPointsAdapter<TContext>(weightsProvider);
            var facade = new TwoPhaseScatteredPointsLenearInterpolatorFacade<TContext>(cachingDecorator, adapter);
            return facade;
        }
    }

    /// <summary>
    /// This class performs partial dependency injection.
    /// It assembles a series of converters and caching interpolation context decorator. Cache is the not cleared (it persists for different returned objects)
    /// </summary>
    public class CachingLinearWeightsProviderFactory2<TContext> : IScatteredPointsLinearInterpolatorOnSphereFactory
    {        
        private readonly AsyncLazy<IScatteredPointsLinearInterpolatorOnSphere> result;

        public CachingLinearWeightsProviderFactory2(IScatteredPointContextBasedLinearWeightProviderOnSphere<TContext> weightsProvider, IAsyncMap<INodes, TContext> contextProvider)
        {
            result = new AsyncLazy<IScatteredPointsLinearInterpolatorOnSphere>(async () =>
            {
                var contextProvidingFactory = new AsyncMapCacheDecoratingFactory<INodes, TContext>(new HashBasedEquatibleINodesConverter(), contextProvider);
                var cachingDecorator = await contextProvidingFactory.CreateAsync();
                var adapter = new CellRequestToPointsAdapter<TContext>(weightsProvider);
                var facade = new TwoPhaseScatteredPointsLenearInterpolatorFacade<TContext>(cachingDecorator, adapter);
                return facade;
            });
        }
        

        public Task<IScatteredPointsLinearInterpolatorOnSphere> CreateAsync()
        {
            return result.GetValueAsync();
        }
    }
}
