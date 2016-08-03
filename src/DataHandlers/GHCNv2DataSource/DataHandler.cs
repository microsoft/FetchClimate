using Microsoft.Research.Science.FetchClimate2;
using Delaunay_Voronoi_Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Research.Science.FetchClimate2.Utils;
using Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints.TimeSeries;
using Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators;
using Microsoft.Research.Science.FetchClimate2.DataHandlers;
using Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints.LinearCombination;
using Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints;

namespace Microsoft.Research.Science.FetchClimate2.GHCNv2DataSource
{
    public class DataHandler : ScatteredPointsAsLinearCombinationDataHandler
    {
        public static async Task<DataHandler> CreateAsync(IStorageContext dataContext)
        {
            IScatteredPointContextBasedLinearWeightProviderOnSphere<IDelaunay_Voronoi> weightsProvider = new AlexNNIAdapter();
            IAsyncMap<INodes,IDelaunay_Voronoi> interpolationContextFactory = new AlexNNIContextProvider();
            
            //NOTE: interpolation context is cached in the produced interpolators (e.g. Delaunay triangulation is computed only once for each nodes set)
            //We can use one cache for all of the requests (cache is not cleaned) as we use AllStationsStationLocator which returns all stations for any request
            IScatteredPointsLinearInterpolatorOnSphereFactory pointsInterpolatorOnSphereFactory = new CachingLinearWeightsProviderFactory2<IDelaunay_Voronoi>(weightsProvider,interpolationContextFactory);

            int stationsCount = dataContext.StorageDefinition.DimensionsLengths["stations"];
            var timeAxis = await dataContext.GetDataAsync("time");
            ITimeAxisAvgProcessing timeAxisIntegrator = new TimeAxisAvgProcessing.TimeAxisAvgFacade(
                timeAxis,
                new TimeAxisProjections.DateTimeMoments(),
                new WeightProviders.StepFunctionInterpolation(),
                new DataCoverageEvaluators.ContinousMeansCoverageEvaluator());                
            IStationLocator stationLocator = new AllStationsStationLocator(stationsCount);

            ICellRequestMapFactory<RealValueNodes> timeSeriesAveragerFactory = (await TimeIntegratorBasedAveragerFactory.CreateAsync(dataContext, timeAxisIntegrator, stationLocator));
            ICellRequestMap<RealValueNodes> timeSeriesAverager = await timeSeriesAveragerFactory.CreateAsync();

            //NOTE: Here we engage caching of timeseries. Timeseries is cached for all cells having the same time segment. It is acheived by HashBasedTimeSegmentOnlyConverter
            ICellRequestMapFactory<RealValueNodes> cachingTimeSeriesAveragerFactory = new CellRequestMapCachingFactory<RealValueNodes>(timeSeriesAverager, new HashBasedTimeSegmentOnlyConverter());

            ILinearCombintaionContextFactory compContextFactory = new LinearWeightsContextFactoryFacade<RealValueNodes>(pointsInterpolatorOnSphereFactory, cachingTimeSeriesAveragerFactory);

            //NOTE: Hege caching is engaged as well. We cache the variogram for the cells with the same corresponding node set.
            IVariogramProvider lmVariogramFitter = new LmDotNetVariogramProvider();
            IVariogramProviderFactory variogramProviderFactory = new VariogramProviderCachingFactory(lmVariogramFitter);
            IUncertaintyEvaluatorOfLinearCombination uncertatintyEvaluator = new PointsGausianFieldUncertaintyEvaluator(variogramProviderFactory);

            return new DataHandler(dataContext, compContextFactory, uncertatintyEvaluator);
        }

        private DataHandler(IStorageContext dataContext, ILinearCombintaionContextFactory linearCombContextFactory, IUncertaintyEvaluatorOfLinearCombination uncertaintyEvaluator)
            : base(dataContext, linearCombContextFactory, uncertaintyEvaluator)
        { }        
    }
}
