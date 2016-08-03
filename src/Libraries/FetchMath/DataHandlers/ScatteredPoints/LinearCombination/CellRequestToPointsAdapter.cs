using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints.LinearCombination
{
    public interface IScatteredPointContextBasedLinearWeightProviderOnSphere<TContext>
    {
        /// <summary>
        /// Returns a set of linear weights to use in linear combination for obtaining interpolated value
        /// </summary>
        /// <param name="interpolationContext"></param>
        /// <returns></returns>
        Task<LinearWeight[]> GetLinearWeigthsAsync(double lat, double lon, TContext interpolationContext);
    }

    /// <summary>
    /// Adapter that converts GeoCellTuple requests into a series of (lat,lon) requests
    /// (Approximates a cell as a grid)
    /// </summary>
    public class CellRequestToPointsAdapter<TContext> : ILinearWeightsFromContextProvider<TContext>
    {
        readonly int cellDivisionNum;

        private readonly IScatteredPointContextBasedLinearWeightProviderOnSphere<TContext> component;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="component"></param>
        /// <param name="cellDivisionNum">the cell will be approximated as cellDivisionNum*cellDivisionNum regular grid</param>
        public CellRequestToPointsAdapter(IScatteredPointContextBasedLinearWeightProviderOnSphere<TContext> component, int cellDivisionNum = 2)
        {
            this.component = component;
            this.cellDivisionNum = cellDivisionNum;
        }

        public async Task<LinearWeight[]> GetLinearWeigthsAsync(ICellRequest cell, TContext interpolationContext)
        {
            
            if (cell.LatMin == cell.LatMax && cell.LonMin == cell.LonMax)
            {
                return await component.GetLinearWeigthsAsync(cell.LatMin, cell.LonMin, interpolationContext);
            }
            else
            {
                double latStep = (cell.LatMax - cell.LatMin) / (cellDivisionNum - 1);
                double lonStep = (cell.LonMax - cell.LonMin) / (cellDivisionNum - 1);

                Dictionary<int,double> weigthsDict = new Dictionary<int,double>();                

                for (int i = 0; i < cellDivisionNum; i++)
                    for (int j = 0; j < cellDivisionNum; j++)
                    {
                        var weights = await component.GetLinearWeigthsAsync(cell.LatMin + i * latStep, cell.LonMin + j * lonStep, interpolationContext);
                        int K = weights.Length;
                        for (int k = 0; k < K; k++)
                        {
                            int idx = weights[k].DataIndex;
                            if (weigthsDict.ContainsKey(idx))                            
                                weigthsDict[idx] += weights[k].Weight;                                                            
                            else                            
                                weigthsDict.Add(idx, weights[k].Weight);                                
                        }
                    }

                double M = cellDivisionNum * cellDivisionNum;

                var result = weigthsDict.Select(kvp => 
                    {                        
                        return new LinearWeight(kvp.Key, kvp.Value / M);
                    }).ToArray();

                System.Diagnostics.Debug.Assert(Math.Abs(result.Sum(e => e.Weight) - 1.0)<1e-6);//test for unbiasness, requires component.GetLinearWeigths to be unbiased

                return result;
            }
        }        
    }
}
