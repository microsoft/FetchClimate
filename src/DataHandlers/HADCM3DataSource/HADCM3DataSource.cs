using Microsoft.Research.Science.FetchClimate2.Integrators.Spatial;
using Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators;
using Microsoft.Research.Science.FetchClimate2.ValueAggregators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.HADCM3DataSource
{
    public class HADCM3DataHandler : BatchDataHandler
    {
        public static async Task<HADCM3DataHandler> CreateAsync(IStorageContext dataContext)
        {
            var storageDefinition = dataContext.StorageDefinition;
            var latIntegratorTask = LinearIntegratorsFactory.SmartConstructAsync(dataContext, IntegratorsFactoryHelpers.AutodetectLatName(storageDefinition));
            var lonIntegratorTask = LinearIntegratorsFactory.SmartConstructAsync(dataContext, IntegratorsFactoryHelpers.AutodetectLonName(storageDefinition));

            var timeAxis = ((double[])(await dataContext.GetDataAsync("time"))).Select(elem => elem - 15).ToArray();//shifting middle of the interval (30 days length in total) to the beginning of the interval
            var timeIntegrator = new TimeAxisAvgProcessing.TimeAxisAvgFacade( 
                timeAxis,
                new TimeAxisProjections.ContinuousDays360(1960, 1),//0 index axis value is 14400 which is 1/1/2000 12:00:00 AM
                new WeightProviders.StepFunctionInterpolation(),
                new DataCoverageEvaluators.ContinousMeansCoverageEvaluator()
                ); 
            var latIntegrator = await latIntegratorTask;
            var lonIntegrator = await lonIntegratorTask;

            var baseNodeUncertainty = new NoBaseUncertaintyProvider();
            var temporalVarianceCalculaator = new LinearCombination1DVarianceCalc(new StorageContextMetadataTimeVarianceExtractor(storageDefinition), timeIntegrator);
            var spatialVarianceCalculator = new LinearCombinationOnSphereVarianceCalculator(new StorageContextMetadataSpatialVarianceExtractor(storageDefinition), latIntegrator, lonIntegrator);


            var gaussianFieldUncertaintyEvaluator = new SequentialTimeSpatialUncertaintyEvaluatorFacade(timeIntegrator, latIntegrator, lonIntegrator,temporalVarianceCalculaator,spatialVarianceCalculator, baseNodeUncertainty);
            var coverageCheckUncertaintyEvaluator = new GridUncertaintyConventionsDecorator(gaussianFieldUncertaintyEvaluator, latIntegrator, lonIntegrator, timeIntegrator);
            var scaledUncertaintyEvaluator = new Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators.LinearTransformDecorator(coverageCheckUncertaintyEvaluator);
            var variablePresenceCheckEvaluator = new VariablePresenceCheckDecorator(dataContext.StorageDefinition,scaledUncertaintyEvaluator);

            scaledUncertaintyEvaluator.SetTranform("pr", p => p * 2592000);
            //we do not need DegK to DegC in uncertainty as uncertainty is based on defference and does not depend on constant ofsets

            var gridAggregator = new GridMeanAggregator(dataContext, timeIntegrator, latIntegrator, lonIntegrator, false);
            var clusteringAggregator = new GridClusteringDecorator(dataContext.StorageDefinition, gridAggregator, timeIntegrator, latIntegrator, lonIntegrator);
            var scaledAggregator = new Microsoft.Research.Science.FetchClimate2.ValueAggregators.LinearTransformDecorator(dataContext, clusteringAggregator);

            scaledAggregator.SetAdditionalTranform("pr", p => p * 2592000);
            scaledAggregator.SetAdditionalTranform("tas", t => t - 273.15);

            return new HADCM3DataHandler(dataContext, variablePresenceCheckEvaluator, scaledAggregator);
        }


        private HADCM3DataHandler(IStorageContext dataContext, IBatchUncertaintyEvaluator uncertaintyEvaluator, IBatchValueAggregator valueAggregator)
            : base(dataContext, uncertaintyEvaluator, valueAggregator)
        { }
    }
}
