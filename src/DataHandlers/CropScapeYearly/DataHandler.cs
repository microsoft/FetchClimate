using Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.CropScapeYearly
{
    public class DataHandler: BatchDataHandler
        {            
            public static async Task<DataHandler> CreateAsync(IStorageContext dataContext)
            {
                var storageDefinition = dataContext.StorageDefinition;
                string latAxisName = IntegratorsFactoryHelpers.AutodetectLatName(storageDefinition);
                string lonAxisName = IntegratorsFactoryHelpers.AutodetectLonName(storageDefinition);

                var timeAxisDetection = StepFunctionAutoDetectHelper.SmartDetectAxis(dataContext);
                var result = timeAxisDetection as StepFunctionAutoDetectHelper.AxisFound;
                if (result == null)
                    throw new InvalidOperationException("Time axes detection failed");


                var latAxisTask = dataContext.GetDataAsync(latAxisName);
                var lonAxisTask = dataContext.GetDataAsync(lonAxisName);
                var timeAxis = await dataContext.GetDataAsync(result.AxisName);

                var timeGroupCalc = StepFunctionAutoDetectHelper.ConstructStatisticsCalculator(result.AxisKind, timeAxis, result.BaseOffset);

                var latAxis = await latAxisTask;
                var latStatCalc = new CoveredPointsStatistics(latAxis);
                var lonAxis = await lonAxisTask;
                var lonStatCalc = new CoveredPointsStatistics(lonAxis);                

                var baseUncertaintyProvider = new NoUncertaintyEvaluator();
                var coverageCheckUncertaintyEvaluator = new GridUncertaintyConventionsDecorator(baseUncertaintyProvider, latStatCalc, lonStatCalc, timeGroupCalc);
                var variablePresenceCheckEvaluator = new VariablePresenceCheckDecorator(dataContext.StorageDefinition, coverageCheckUncertaintyEvaluator);

                var gridAggregator = new GridModeAggregator(dataContext, timeGroupCalc, latStatCalc, lonStatCalc, true);
                var clusteringAggregator = new GridClusteringDecorator(dataContext.StorageDefinition, gridAggregator, timeGroupCalc, latStatCalc, lonStatCalc);

                return new DataHandler(dataContext, variablePresenceCheckEvaluator, clusteringAggregator);
            }

            private DataHandler(IStorageContext dataContext, IBatchUncertaintyEvaluator uncertaintyEvaluator, IBatchValueAggregator valueAggregator)
                : base(dataContext, uncertaintyEvaluator, valueAggregator)
            { }
        }    
}
