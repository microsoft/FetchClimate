using Microsoft.Research.Science.FetchClimate2;
using Microsoft.Research.Science.FetchClimate2.DataHandlers;
using Microsoft.Research.Science.FetchClimate2.Integrators.Spatial;
using Microsoft.Research.Science.FetchClimate2.Integrators.Temporal;
using Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators;
using Microsoft.Research.Science.FetchClimate2.ValueAggregators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FutureClimate
{
    public class DataHandler : BatchDataHandler
    {
        public static async Task<DataHandler> CreateAsync(IStorageContext dataContext)
        {
            var timeAxisDetection = StepFunctionAutoDetectHelper.SmartDetectAxis(dataContext);
            var detected = timeAxisDetection as StepFunctionAutoDetectHelper.AxisFound;
            if (detected == null)
                throw new InvalidOperationException("Can't autodetect time axis. See logs for particular failure reason");
            var axis = await dataContext.GetDataAsync(detected.AxisName);
            var timeIntegrator = StepFunctionAutoDetectHelper.ConstructAverager(detected.AxisKind, axis, detected.BaseOffset);
            var storageDefinition = dataContext.StorageDefinition;
            var latIntegratorTask = LinearIntegratorsFactory.SmartConstructAsync(dataContext, IntegratorsFactoryHelpers.AutodetectLatName(storageDefinition));
            var lonIntegratorTask = LinearIntegratorsFactory.SmartConstructAsync(dataContext, IntegratorsFactoryHelpers.AutodetectLonName(storageDefinition));

            var latIntegrator = await latIntegratorTask;
            var lonIntegrator = await lonIntegratorTask;

            //var baseNodeUncertainty = new NoBaseUncertaintyProvider();
            //var temporalVarianceCalculaator = new LinearCombination1DVarianceCalc(new StorageContextMetadataTimeVarianceExtractor(storageDefinition), timeIntegrator);
            //var spatialVarianceCalculator = new LinearCombinationOnSphereVarianceCalculator(new StorageContextMetadataSpatialVarianceExtractor(storageDefinition), latIntegrator, lonIntegrator);

            //var gaussianFieldUncertaintyEvaluator = new SequentialTimeSpatialUncertaintyEvaluatorFacade(timeIntegrator, latIntegrator, lonIntegrator, temporalVarianceCalculaator, spatialVarianceCalculator, baseNodeUncertainty);
            var percentileEvaluator = new PercentileGridAggregator(dataContext, timeIntegrator, latIntegrator, lonIntegrator, true);
            var coverageCheckUncertaintyEvaluator = new GridUncertaintyConventionsDecorator(percentileEvaluator, latIntegrator, lonIntegrator, timeIntegrator);
            var scaledUncertaintyEvaluator = new Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators.LinearTransformDecorator(coverageCheckUncertaintyEvaluator);
            var variablePresenceCheckEvaluator = new VariablePresenceCheckDecorator(dataContext.StorageDefinition, scaledUncertaintyEvaluator);

            var gridAggregator = new GridMeanAggregator(dataContext, timeIntegrator, latIntegrator, lonIntegrator, true);
            var clusteringAggregator = new GridClusteringDecorator(dataContext.StorageDefinition, gridAggregator, timeIntegrator, latIntegrator, lonIntegrator);
            var scaledAggregator = new Microsoft.Research.Science.FetchClimate2.ValueAggregators.LinearTransformDecorator(dataContext, clusteringAggregator);

            return new DataHandler(dataContext, variablePresenceCheckEvaluator, scaledAggregator);
        }

        DataHandler(IStorageContext dataContext, IBatchUncertaintyEvaluator uncertaintyEvaluator, IBatchValueAggregator valueAggregator)
            : base(dataContext, uncertaintyEvaluator, valueAggregator)
        { }
    }
}
