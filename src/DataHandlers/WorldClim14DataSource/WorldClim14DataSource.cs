using Microsoft.Research.Science.FetchClimate2.Integrators.Spatial;
using Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators;
using Microsoft.Research.Science.FetchClimate2.ValueAggregators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Microsoft.Research.Science.FetchClimate2
{
    public class WorldClim14DataSource : BatchDataHandler
    {
        private const string TEMPERATURE = "tmean";
        private const string PRATE = "prec";


        public static async Task<WorldClim14DataSource> CreateAsync(IStorageContext dataContext)
        {
            var storageDefinition = dataContext.StorageDefinition;
            var latIntegratorTask = LinearIntegratorsFactory.SmartConstructAsync(dataContext, IntegratorsFactoryHelpers.AutodetectLatName(storageDefinition));
            var lonIntegratorTask = LinearIntegratorsFactory.SmartConstructAsync(dataContext, IntegratorsFactoryHelpers.AutodetectLonName(storageDefinition));

            var timeIntegrator = new TimeAxisAvgProcessing.MonthlyMeansOverEnoughYearsStepIntegratorFacade(50); // http://www.worldclim.org/methods says: "in most cases these records will represent the 1950-2000"
            var latIntegrator = await latIntegratorTask;
            var lonIntegrator = await lonIntegratorTask;

            var thinningLatIntegrator = new SpatGridIntegatorThinningDecorator(latIntegrator);
            var thinningLonIntegrator = new SpatGridIntegatorThinningDecorator(lonIntegrator);

            var baseNodeUncertainty = new WorldClimBaseNodeUnceratinty();
            var temporalVarianceCalculaator = new LinearCombination1DVarianceCalc(new StorageContextMetadataTimeVarianceExtractor(storageDefinition,12.0), timeIntegrator);
            var spatialVarianceCalculator = new LinearCombinationOnSphereVarianceCalculator(new StorageContextMetadataSpatialVarianceExtractor(storageDefinition), latIntegrator, lonIntegrator);


            var bitMaskProvider = new EmbeddedResourceBitMaskProvider(typeof(WorldClim14DataSource), "Microsoft.Research.Science.FetchClimate2.WcDataMask.bf");

            var gaussianFieldUncertaintyEvaluator = new SequentialTimeSpatialUncertaintyEvaluatorFacade(timeIntegrator, thinningLatIntegrator, thinningLonIntegrator, temporalVarianceCalculaator, spatialVarianceCalculator, baseNodeUncertainty);
            var coverageCheckUncertaintyEvaluator = new GridUncertaintyConventionsDecorator(gaussianFieldUncertaintyEvaluator, latIntegrator, lonIntegrator, timeIntegrator);
            var landMaskEnabledUncertaintyEvaluator = new GridBitmaskDecorator(coverageCheckUncertaintyEvaluator, bitMaskProvider);
            var variablePresenceCheckEvaluator = new VariablePresenceCheckDecorator(dataContext.StorageDefinition, landMaskEnabledUncertaintyEvaluator);

            var gridAggregator = new GridMeanAggregator(dataContext, timeIntegrator, latIntegrator, lonIntegrator, true);
            var clusteringAggregator = new GridClusteringDecorator(dataContext.StorageDefinition, gridAggregator, timeIntegrator, latIntegrator, lonIntegrator);
            var scaledAggregator = new Microsoft.Research.Science.FetchClimate2.ValueAggregators.LinearTransformDecorator(dataContext,clusteringAggregator);

            return new WorldClim14DataSource(dataContext, variablePresenceCheckEvaluator, scaledAggregator);
        }

        public WorldClim14DataSource(IStorageContext dataContext, IBatchUncertaintyEvaluator uncertaintyEvaluator, IBatchValueAggregator valueAggregator)
            : base(dataContext, uncertaintyEvaluator, valueAggregator)
        { }

        class WorldClimBaseNodeUnceratinty : INodeUncertaintyProvider
        {
            public double GetBaseNodeStandardDeviation(ICellRequest cell)
            {
                switch (cell.VariableName)
                {
                    case TEMPERATURE: return 0.4843674; //Deg C
                    case PRATE: return 12.13914; //mm/mon
                    default: throw new NotSupportedException(string.Format("unsupported variable \"{0}\" by data handler {1}", cell.VariableName, typeof(WorldClim14DataSource).AssemblyQualifiedName.ToString()));
                }
            }
        }
   }
}
