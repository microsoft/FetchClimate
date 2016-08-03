using Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints.LinearCombination;
using Microsoft.Research.Science.FetchClimate2.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints
{
    public interface IScatteredPointsLinearInterpolatorOnSphere
    {
        /// <summary>
        /// Returns a set of linear weights to use in linear combination for obtaining interpolated value
        /// </summary>
        /// <param name="nodes">Nodes to combine into a linear combination</param>                
        /// <returns></returns>
        Task<LinearWeight[]> GetLinearWeigthsAsync(INodes nodes, ICellRequest cell);
    }

    public interface IScatteredPointsLinearInterpolatorOnSphereFactory
    {
        Task<IScatteredPointsLinearInterpolatorOnSphere> CreateAsync();
    }


    public class LinearWeightsContextFactoryFacade<TNodes> : ILinearCombintaionContextFactory
        where TNodes : RealValueNodes
    {
        private static readonly AutoRegistratingTraceSource traceSource = new AutoRegistratingTraceSource("LinearWeightsContextFactoryFacade");
        private readonly IScatteredPointsLinearInterpolatorOnSphereFactory linearInterpolatorFactory;
        private readonly ICellRequestMapFactory<TNodes> timeSeriesAveragerFactory;

        public LinearWeightsContextFactoryFacade(IScatteredPointsLinearInterpolatorOnSphereFactory linearInterpolatorFactory, ICellRequestMapFactory<TNodes> timeSeriesAveragerFactory)
        {
            this.linearInterpolatorFactory = linearInterpolatorFactory;
            this.timeSeriesAveragerFactory = timeSeriesAveragerFactory;
        }


        /// <summary>
        /// Returns a computational context which is a set of nodes and a set "weights" to apply to the nodes to form a linear combination to get the mean value of the cells
        /// </summary>        
        public async Task<LinearCombinationContext> CreateAsync(IEnumerable<ICellRequest> cells)
        {
            Stopwatch sw = Stopwatch.StartNew();
            var tsAveragerTask = timeSeriesAveragerFactory.CreateAsync();
            var liTask = linearInterpolatorFactory.CreateAsync();

            var tsAverager = await tsAveragerTask;
            var linearInterpolator = await liTask;

            sw.Stop();

            traceSource.TraceEvent(TraceEventType.Verbose,1,"Time series averager and lenear interpolator constructed in {0}",sw.Elapsed);

            sw = Stopwatch.StartNew();
            var resultTasks = cells.Select<ICellRequest, Task<Tuple<ICellRequest, RealValueNodes, IEnumerable<LinearWeight>>>>(async c =>
            {
                var tsa = (RealValueNodes)(await tsAverager.GetAsync(c));
                var w = (IEnumerable<LinearWeight>)(await linearInterpolator.GetLinearWeigthsAsync(tsa,c));

                return Tuple.Create(c,tsa,w);                
            }).ToArray();

            var result = await Task.WhenAll(resultTasks);

            sw.Stop();
            traceSource.TraceEvent(TraceEventType.Verbose, 2, "Linear weights for {0} cells prepared in {1}", resultTasks.Length, sw.Elapsed);
            LinearCombinationContext lcc = new LinearCombinationContext(result);
            return lcc;
        }
    }
}
