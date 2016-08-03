using Microsoft.FSharp.Core;
using Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints.LinearCombination;
using Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators;
using Microsoft.Research.Science.FetchClimate2.Utils;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;

namespace Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints
{
    /// <summary>
    /// Factory that returns IVariogramProvider objects whaped into INodeMapHashBasedDecorator for request caching
    /// </summary>
    public class VariogramProviderCachingFactory : IVariogramProviderFactory
    {
        private readonly IVariogramProvider component;
        private readonly AsyncLazy<IVariogramProvider> cachingComponent;

        public VariogramProviderCachingFactory(IVariogramProvider component)
        {
            this.component = component;
            cachingComponent = new AsyncLazy<IVariogramProvider>(async () =>
                {
                    var map = new varioToNodesMapAdapter(component);
                    var cached = new AsyncMapCacheDecorator<RealValueNodes, VariogramModule.IVariogram>(converter, map);
                    var varioProvider = new NodesMapToVarioAdapter(cached);
                    return varioProvider;
                });
        }

        class varioToNodesMapAdapter : IAsyncMap<RealValueNodes,VariogramModule.IVariogram>
        {
            IVariogramProvider component;
            public varioToNodesMapAdapter(IVariogramProvider vario)
            {
                component = vario;
            }
            public Task<VariogramModule.IVariogram> GetAsync(RealValueNodes nodes)
            {
                return component.GetSpatialVariogramAsync((RealValueNodes)nodes);
            }
        }

        class NodesMapToVarioAdapter : IVariogramProvider
        {
            private readonly IAsyncMap<RealValueNodes, VariogramModule.IVariogram> component;
            public NodesMapToVarioAdapter(IAsyncMap<RealValueNodes, VariogramModule.IVariogram> component)
            {
                this.component = component;
            }
            public Task<VariogramModule.IVariogram> GetSpatialVariogramAsync(LinearCombination.RealValueNodes nodes)
            {
                return component.GetAsync(nodes);
            }
        }

        private readonly IEquatableConverter<RealValueNodes> converter = new HashBasedEquatibleRealValueNodesConverter();       

        public async Task<IVariogramProvider> ConstructAsync()
        {
            return await cachingComponent.GetValueAsync();
        }
    }

    public class LmDotNetVariogramProvider : IVariogramProvider
    {
        public static AutoRegistratingTraceSource traceSource = new AutoRegistratingTraceSource("LmDotNetVariogramProvider", SourceLevels.All);


        private static readonly TaskFactory taskFactory;
        static LmDotNetVariogramProvider()
        {
            LimitedConcurrencyLevelTaskScheduler lclts = new LimitedConcurrencyLevelTaskScheduler(Environment.ProcessorCount);               
            taskFactory = new TaskFactory(lclts);
        }

        public Task<VariogramModule.IVariogram> GetSpatialVariogramAsync(LinearCombination.RealValueNodes nodes)
        {
            var task = taskFactory.StartNew(new Func<object, VariogramModule.IVariogram>(obj =>
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    LinearCombination.RealValueNodes localNodes = (LinearCombination.RealValueNodes)obj;
                    var variogramFitter = new LMDotNetVariogramFitter.Fitter() as VariogramModule.IVariogramFitter;

                    traceSource.TraceEvent(TraceEventType.Start, 1, "Starting build of emperical variogram");
                    var pointSet = new EmpVariogramBuilder.PointSet(localNodes.Lats, localNodes.Lons, localNodes.Values);

                    var dist = FuncConvert.ToFSharpFunc(new Converter<Tuple<double, double>, FSharpFunc<Tuple<double, double>, double>>(t1 =>
                        FuncConvert.ToFSharpFunc(new Converter<Tuple<double, double>, double>(t2 => SphereMath.GetDistance(t1.Item1, t1.Item2, t2.Item1, t2.Item2)))));

                    var empVar = EmpVariogramBuilder.EmpiricalVariogramBuilder.BuildEmpiricalVariogram(pointSet, dist);                    
                    sw.Stop();
                    traceSource.TraceEvent(TraceEventType.Stop, 1, string.Format("Emperical variogram is build in {0}", sw.Elapsed));
                    sw = Stopwatch.StartNew();
                    traceSource.TraceEvent(TraceEventType.Start, 2, "Starting variogram fitting");
                    var variogramRes = variogramFitter.Fit(empVar);                    
                    sw.Stop();
                    traceSource.TraceEvent(TraceEventType.Stop, 2, string.Format("Emperical variogram is build in {0}", sw.Elapsed));
                    if (FSharpOption<VariogramModule.IDescribedVariogram>.get_IsNone(variogramRes))
                    {
                        traceSource.TraceEvent(TraceEventType.Error, 3, "Fariogram fitting failed. Falling back to coarse variogram approximation");
                        return variogramFitter.GetFallback(empVar);
                    }
                    else
                        return variogramRes.Value;                    
                }),nodes);
            return task;
        }
    }
}
