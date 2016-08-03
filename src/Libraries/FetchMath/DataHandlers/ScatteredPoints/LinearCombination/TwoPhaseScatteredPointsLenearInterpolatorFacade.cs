using Microsoft.Research.Science.FetchClimate2.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints.LinearCombination
{    
    public interface ILinearWeightsFromContextProvider<TContext>
    {
        /// <summary>
        /// Returns a set of linear weights to use in linear combination for obtaining interpolated value
        /// </summary>
        /// <param name="interpolationContext"></param>
        /// <returns></returns>
        Task<LinearWeight[]> GetLinearWeigthsAsync(ICellRequest cell, TContext interpolationContext);
    }


    /// <summary>
    /// Splits the GetLinearWeights methods into context fetching from INodesMap and weights 
    /// </summary>
    public class TwoPhaseScatteredPointsLenearInterpolatorFacade<TContext> : IScatteredPointsLinearInterpolatorOnSphere
    {
        private readonly IAsyncMap<INodes, TContext> contextProvider;
        private readonly ILinearWeightsFromContextProvider<TContext> weightsProvider;

        public TwoPhaseScatteredPointsLenearInterpolatorFacade(IAsyncMap<INodes, TContext> contextProvider, ILinearWeightsFromContextProvider<TContext> weightsProvider)
        {
            this.contextProvider = contextProvider;
            this.weightsProvider = weightsProvider;
        }

        public async Task<LinearWeight[]> GetLinearWeigthsAsync(INodes nodes, ICellRequest cell)
        {
            TContext context = await contextProvider.GetAsync(nodes);
            return await weightsProvider.GetLinearWeigthsAsync(cell, context);
        }        
    }
}
