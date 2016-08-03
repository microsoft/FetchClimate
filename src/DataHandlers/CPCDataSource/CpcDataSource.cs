
using Microsoft.Research.Science.FetchClimate2.Integrators.Spatial;
using Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators;
using Microsoft.Research.Science.FetchClimate2.ValueAggregators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.CPCDataSource
{
    public class CpcDataHandler : BatchDataHandler
    {               
        public static async Task<CpcDataHandler> CreateAsync(IStorageContext dataContext)
        {
            var storageDefinition = dataContext.StorageDefinition;
            var timeAxis = await dataContext.GetDataAsync("time");
            var timeIntegrator = new TimeAxisAvgProcessing.TimeAxisAvgFacade(
                timeAxis,
                new TimeAxisProjections.ContinuousDays(new DateTime(1948, 1, 1)),
                new WeightProviders.StepFunctionInterpolation(),
                new DataCoverageEvaluators.ContinousMeansCoverageEvaluator()
                );            
            var latIntegratorTask = LinearIntegratorsFactory.SmartConstructAsync(dataContext, IntegratorsFactoryHelpers.AutodetectLatName(storageDefinition));
            var lonIntegratorTask = LinearIntegratorsFactory.SmartConstructAsync(dataContext, IntegratorsFactoryHelpers.AutodetectLonName(storageDefinition));

            
            var latIntegrator = await latIntegratorTask;
            var lonIntegrator = await lonIntegratorTask;       
     
            var baseNodeUncertainty = new CpcBaseUnceratinty();
            var temporalVarianceCalculaator = new LinearCombination1DVarianceCalc(new StorageContextMetadataTimeVarianceExtractor(storageDefinition), timeIntegrator);
            var spatialVarianceCalculator = new LinearCombinationOnSphereVarianceCalculator(new StorageContextMetadataSpatialVarianceExtractor(storageDefinition), latIntegrator, lonIntegrator);

            var gaussianFieldUncertaintyEvaluator = new SequentialTimeSpatialUncertaintyEvaluatorFacade(timeIntegrator, latIntegrator, lonIntegrator,temporalVarianceCalculaator,spatialVarianceCalculator, baseNodeUncertainty);
            var coverageCheckUncertaintyEvaluator = new GridUncertaintyConventionsDecorator(gaussianFieldUncertaintyEvaluator, latIntegrator, lonIntegrator, timeIntegrator);
            var variablePresenceCheckEvaluator = new VariablePresenceCheckDecorator(dataContext.StorageDefinition,coverageCheckUncertaintyEvaluator);

            var gridAggregator = new GridMeanAggregator(dataContext, timeIntegrator, latIntegrator, lonIntegrator, true);
            var clusteringAggregator = new GridClusteringDecorator(dataContext.StorageDefinition,gridAggregator, timeIntegrator, latIntegrator, lonIntegrator);
            var scaledAggregator = new Microsoft.Research.Science.FetchClimate2.ValueAggregators.LinearTransformDecorator(dataContext,clusteringAggregator);


            return new CpcDataHandler(dataContext, variablePresenceCheckEvaluator, scaledAggregator);
        }

        private CpcDataHandler(IStorageContext dataContext, IBatchUncertaintyEvaluator uncertaintyEvaluator, IBatchValueAggregator valueAggregator)
            : base(dataContext,uncertaintyEvaluator,valueAggregator)
        { }

        class CpcBaseUnceratinty : INodeUncertaintyProvider
        {
            public double GetBaseNodeStandardDeviation(ICellRequest cell)
            {
                if (cell.VariableName != "soilw")
                    return double.NaN;
                return 120.6507;
            }
        }
    }


}
