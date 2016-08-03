using Microsoft.Research.Science.FetchClimate2;
using Microsoft.Research.Science.FetchClimate2.Integrators.Spatial;
using Microsoft.Research.Science.FetchClimate2.Integrators.Temporal;
using Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators;
using Microsoft.Research.Science.FetchClimate2.ValueAggregators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.BRAZILmodelDataSource
{
    /// <summary>
    /// For variables with time axis (Deforestation , Road density)
    /// </summary>
    /// 
    public class BRAZILmodelDataSource : BatchDataHandler
    {
        const int referenceYear = 2007;

        public static async Task<BRAZILmodelDataSource> CreateAsync(IStorageContext dataContext)
        {
            var storageDefinition = dataContext.StorageDefinition;
            var latIntegratorTask = LinearIntegratorsFactory.SmartConstructAsync(dataContext, IntegratorsFactoryHelpers.AutodetectLatName(storageDefinition));
            var lonIntegratorTask = LinearIntegratorsFactory.SmartConstructAsync(dataContext, IntegratorsFactoryHelpers.AutodetectLonName(storageDefinition));

            var timeAxis = Enumerable.Range(0, 47).ToArray();

            var timeIntegrator = new TimeAxisAvgProcessing.TimeAxisAvgFacade(
                timeAxis,
                new TimeAxisProjections.ContinousYears(referenceYear),
                new WeightProviders.StepFunctionInterpolation(),
                new DataCoverageEvaluators.ContinousMeansCoverageEvaluator());
            var latIntegrator = await latIntegratorTask;
            var lonIntegrator = await lonIntegratorTask;            

            var baseNodeUncertainty = new NoBaseUncertaintyProvider();
            var temporalVarianceCalculaator = new LinearCombination1DVarianceCalc(new StorageContextMetadataTimeVarianceExtractor(storageDefinition), timeIntegrator);
            var spatialVarianceCalculator = new LinearCombinationOnSphereVarianceCalculator(new StorageContextMetadataSpatialVarianceExtractor(storageDefinition), latIntegrator, lonIntegrator);

            var gaussianFieldUncertaintyEvaluator = new SequentialTimeSpatialUncertaintyEvaluatorFacade(timeIntegrator, latIntegrator, lonIntegrator,temporalVarianceCalculaator,spatialVarianceCalculator, baseNodeUncertainty);
            var coverageCheckUncertaintyEvaluator = new GridUncertaintyConventionsDecorator(gaussianFieldUncertaintyEvaluator, latIntegrator, lonIntegrator, timeIntegrator);
            var variablePresenceCheckEvaluator = new VariablePresenceCheckDecorator(dataContext.StorageDefinition, coverageCheckUncertaintyEvaluator);

            var gridAggregator = new GridMeanAggregator(dataContext, timeIntegrator, latIntegrator, lonIntegrator, true);
            var clusteringAggregator = new GridClusteringDecorator(dataContext.StorageDefinition, gridAggregator, timeIntegrator, latIntegrator, lonIntegrator);
            var scaledAggregator = new Microsoft.Research.Science.FetchClimate2.ValueAggregators.LinearTransformDecorator(dataContext,clusteringAggregator);

            gridAggregator.SetMissingValue("RoadDensityData",-9999.0);
            gridAggregator.SetMissingValue("Deforestation", -9999.0);

            return new BRAZILmodelDataSource(dataContext, variablePresenceCheckEvaluator, scaledAggregator);
        }

        private BRAZILmodelDataSource(IStorageContext dataContext, IBatchUncertaintyEvaluator uncertaintyEvaluator, IBatchValueAggregator valueAggregator)
            : base(dataContext,uncertaintyEvaluator,valueAggregator)
        { }
    }
}
