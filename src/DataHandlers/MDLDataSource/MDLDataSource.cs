using Microsoft.Research.Science.FetchClimate2.Integrators.Spatial;
using Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators;
using Microsoft.Research.Science.FetchClimate2.ValueAggregators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.MLDDataSource
{
    public class MLDDataHandler : BatchDataHandler
    {
        const string DATA = "data";

        public static async Task<MLDDataHandler> CreateAsync(IStorageContext dataContext)
        {
            var storageDefinition = dataContext.StorageDefinition;
            var latIntegratorTask = LinearIntegratorsFactory.SmartConstructAsync(dataContext, IntegratorsFactoryHelpers.AutodetectLatName(storageDefinition));
            var lonIntegratorTask = LinearIntegratorsFactory.SmartConstructAsync(dataContext, IntegratorsFactoryHelpers.AutodetectLonName(storageDefinition));

            var timeAxis = await dataContext.GetDataAsync("days since 1998-1-1 0:0:0");
            var timeIntegrator = new TimeAxisAvgProcessing.TimeAxisAvgFacade(
                timeAxis,
                new TimeAxisProjections.ContinuousDays(new DateTime(1998,1,1)),
                new WeightProviders.StepFunctionInterpolation(),
                new DataCoverageEvaluators.ContinousMeansCoverageEvaluator());
                
            var latIntegrator = await latIntegratorTask;
            var lonIntegrator = await lonIntegratorTask;

            var thinningLatIntegrator = new SpatGridIntegatorThinningDecorator(latIntegrator);
            var thinningLonIntegrator = new SpatGridIntegatorThinningDecorator(lonIntegrator);
            var thinningTimeIntegrator = new TimeAxisIntegratorThinningDecorator(timeIntegrator);

            var baseNodeUncertainty = new NoBaseUncertaintyProvider();
            var temporalVarianceCalculaator = new LinearCombination1DVarianceCalc(new StorageContextMetadataTimeVarianceExtractor(storageDefinition), timeIntegrator);
            var spatialVarianceCalculator = new LinearCombinationOnSphereVarianceCalculator(new StorageContextMetadataSpatialVarianceExtractor(storageDefinition), latIntegrator, lonIntegrator);


            var gaussianFieldUncertaintyEvaluator = new SequentialTimeSpatialUncertaintyEvaluatorFacade(thinningTimeIntegrator, thinningLatIntegrator, thinningLonIntegrator, temporalVarianceCalculaator, spatialVarianceCalculator, baseNodeUncertainty);
            var coverageCheckUncertaintyEvaluator = new GridUncertaintyConventionsDecorator(gaussianFieldUncertaintyEvaluator, latIntegrator, lonIntegrator, timeIntegrator);
            var variablePresenceCheckEvaluator = new VariablePresenceCheckDecorator(dataContext.StorageDefinition, coverageCheckUncertaintyEvaluator);

            var gridAggregator = new GridMeanAggregator(dataContext, timeIntegrator, latIntegrator, lonIntegrator, true);
            var clusteringAggregator = new GridClusteringDecorator(dataContext.StorageDefinition, gridAggregator, timeIntegrator, latIntegrator, lonIntegrator);

            gridAggregator.SetMissingValue("data", -9999);            

            return new MLDDataHandler(dataContext, variablePresenceCheckEvaluator, clusteringAggregator);
        }


        public MLDDataHandler(IStorageContext dataContext, IBatchUncertaintyEvaluator uncertaintyEvaluator, IBatchValueAggregator valueAggregator)
            : base(dataContext, uncertaintyEvaluator, valueAggregator)
        { }
    }
}
