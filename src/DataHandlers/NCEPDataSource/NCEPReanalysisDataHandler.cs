using Microsoft.Research.Science.FetchClimate2.Integrators.Spatial;
using Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators;
using Microsoft.Research.Science.FetchClimate2.ValueAggregators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    public class NCEPReanalysisRegularGridDataHandler : BatchDataHandler
    {
        public static async Task<NCEPReanalysisRegularGridDataHandler> CreateAsync(IStorageContext dataContext)
        {
            var storageDefinition = dataContext.StorageDefinition;
            var timeAxis = await dataContext.GetDataAsync("time");

            var timeIntegrator = new TimeAxisAvgProcessing.TimeAxisAvgFacade(
                timeAxis,
                new TimeAxisProjections.ContinuousHours(new DateTime(1, 1, 1)),
                new WeightProviders.LinearInterpolation(),
                new DataCoverageEvaluators.IndividualObsCoverageEvaluator());                
            var latIntegratorTask = LinearIntegratorsFactory.SmartConstructAsync(dataContext, IntegratorsFactoryHelpers.AutodetectLatName(storageDefinition));
            var lonIntegratorTask = LinearIntegratorsFactory.SmartConstructAsync(dataContext, IntegratorsFactoryHelpers.AutodetectLonName(storageDefinition));
            
            var latIntegrator = await latIntegratorTask;
            var lonIntegrator = await lonIntegratorTask;

            var thinningTimeIntegrator = new TimeAxisIntegratorThinningDecorator(timeIntegrator);
            var thinningLatIntegrator = new SpatGridIntegatorThinningDecorator(latIntegrator);
            var thinningLonIntegrator = new SpatGridIntegatorThinningDecorator(lonIntegrator);

            var baseNodeUncertainty = new NoBaseUncertaintyProvider();
            var temporalVarianceCalculaator = new LinearCombination1DVarianceCalc(new StorageContextMetadataTimeVarianceExtractor(storageDefinition), timeIntegrator);
            var spatialVarianceCalculator = new LinearCombinationOnSphereVarianceCalculator(new StorageContextMetadataSpatialVarianceExtractor(storageDefinition), latIntegrator, lonIntegrator);

            var gaussianFieldUncertaintyEvaluator = new SequentialTimeSpatialUncertaintyEvaluatorFacade(thinningTimeIntegrator, thinningLatIntegrator, thinningLonIntegrator, temporalVarianceCalculaator, spatialVarianceCalculator, baseNodeUncertainty);
            var coverageCheckUncertaintyEvaluator = new GridUncertaintyConventionsDecorator(gaussianFieldUncertaintyEvaluator, latIntegrator, lonIntegrator, timeIntegrator); //we do not need DegK to DegC in uncertainty as uncertainty is based on defference and does not depend on constant ofsets
            var variablePresenceCheckEvaluator = new VariablePresenceCheckDecorator(dataContext.StorageDefinition, coverageCheckUncertaintyEvaluator);

            var gridAggregator = new GridMeanAggregator(dataContext, timeIntegrator, latIntegrator, lonIntegrator, false);
            var clusteringAggregator = new GridClusteringDecorator(dataContext.StorageDefinition,gridAggregator, timeIntegrator, latIntegrator, lonIntegrator);
            var scaledAggregator = new Microsoft.Research.Science.FetchClimate2.ValueAggregators.LinearTransformDecorator(dataContext, clusteringAggregator);

            scaledAggregator.SetAdditionalTranform("air", new Func<double, double>(v => v - 273.15)); // DegK to DegC

            return new NCEPReanalysisRegularGridDataHandler(dataContext, variablePresenceCheckEvaluator, scaledAggregator);
        }

        public NCEPReanalysisRegularGridDataHandler(IStorageContext dataContext, IBatchUncertaintyEvaluator uncertaintyEvaluator, IBatchValueAggregator valueAggregator)
            : base(dataContext, uncertaintyEvaluator, valueAggregator)
        { }
    }

    public class NCEPReanalysisGaussT62GridDataHandler : BatchDataHandler
    {
        public static async Task<NCEPReanalysisGaussT62GridDataHandler> CreateAsync(IStorageContext dataContext)
        {
            var storageDefinition = dataContext.StorageDefinition;
            var timeAxis = await dataContext.GetDataAsync("time");
            var timeIntegrator = new TimeAxisAvgProcessing.TimeAxisAvgFacade(
                timeAxis,
                new TimeAxisProjections.ContinuousHours(new DateTime(1, 1, 1)),
                new WeightProviders.StepFunctionInterpolation(),
                new DataCoverageEvaluators.ContinousMeansCoverageEvaluator());            
            var latIntegratorTask = LinearIntegratorsFactory.SmartConstructAsync(dataContext, IntegratorsFactoryHelpers.AutodetectLatName(storageDefinition));
            var lonIntegratorTask = LinearIntegratorsFactory.SmartConstructAsync(dataContext, IntegratorsFactoryHelpers.AutodetectLonName(storageDefinition));

            
            var latIntegrator = await latIntegratorTask;
            var lonIntegrator = await lonIntegratorTask;

            var thinningTimeIntegrator = new TimeAxisIntegratorThinningDecorator(timeIntegrator);
            var thinningLatIntegrator = new SpatGridIntegatorThinningDecorator(latIntegrator);
            var thinningLonIntegrator = new SpatGridIntegatorThinningDecorator(lonIntegrator);

            var baseNodeUncertainty = new NoBaseUncertaintyProvider();
            var temporalVarianceCalculaator = new LinearCombination1DVarianceCalc(new StorageContextMetadataTimeVarianceExtractor(storageDefinition), timeIntegrator);
            var spatialVarianceCalculator = new LinearCombinationOnSphereVarianceCalculator(new StorageContextMetadataSpatialVarianceExtractor(storageDefinition), latIntegrator, lonIntegrator);

            var gaussianFieldUncertaintyEvaluator = new SequentialTimeSpatialUncertaintyEvaluatorFacade(thinningTimeIntegrator, latIntegrator, lonIntegrator, temporalVarianceCalculaator, spatialVarianceCalculator, baseNodeUncertainty);
            var coverageCheckUncertaintyEvaluator = new GridUncertaintyConventionsDecorator(gaussianFieldUncertaintyEvaluator, latIntegrator, lonIntegrator, timeIntegrator);
            var scaledUncertaintyEvaluator = new Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators.LinearTransformDecorator(coverageCheckUncertaintyEvaluator);
            var variablePresenceCheckEvaluator = new VariablePresenceCheckDecorator(dataContext.StorageDefinition, scaledUncertaintyEvaluator);

            scaledUncertaintyEvaluator.SetTranform("prate", new Func<double, double>(v => v * 2592000.0)); //kg/m^2/s to mm/month (assuming month is 30 days)  

            var gridAggregator = new GridMeanAggregator(dataContext, timeIntegrator, latIntegrator, lonIntegrator, false);
            var clusteringAggregator = new GridClusteringDecorator(dataContext.StorageDefinition, gridAggregator, timeIntegrator, latIntegrator, lonIntegrator);
            var scaledAggregator = new Microsoft.Research.Science.FetchClimate2.ValueAggregators.LinearTransformDecorator(dataContext, clusteringAggregator);

            scaledAggregator.SetAdditionalTranform("prate", new Func<double, double>(v => v * 2592000.0)); //kg/m^2/s to mm/month (assuming month is 30 days)  

            return new NCEPReanalysisGaussT62GridDataHandler(dataContext, variablePresenceCheckEvaluator, scaledAggregator);
        }

        public NCEPReanalysisGaussT62GridDataHandler(IStorageContext dataContext, IBatchUncertaintyEvaluator uncertaintyEvaluator, IBatchValueAggregator valueAggregator)
            : base(dataContext, uncertaintyEvaluator, valueAggregator)
        { }
    }
}
