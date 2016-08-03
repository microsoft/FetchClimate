using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Research.Science.FetchClimate2;
using Microsoft.Research.Science.FetchClimate2.Integrators;
using Microsoft.Research.Science.FetchClimate2.Integrators.Spatial;
using Microsoft.Research.Science.FetchClimate2.Integrators.Temporal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Research.Science.FetchClimate2.ValueAggregators;
using Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators;

namespace Microsoft.Research.Science.FetchClimate2.NDVIYearly
{
        public class DataHandler : BatchDataHandler
        {            
            public static async Task<DataHandler> CreateAsync(IStorageContext dataContext)
            {
                var storageDefinition = dataContext.StorageDefinition;
                var latIntegratorTask = LinearIntegratorsFactory.SmartConstructAsync(dataContext, IntegratorsFactoryHelpers.AutodetectLatName(storageDefinition));
                var lonIntegratorTask = LinearIntegratorsFactory.SmartConstructAsync(dataContext, IntegratorsFactoryHelpers.AutodetectLonName(storageDefinition));

                var timeAxisDetection = StepFunctionAutoDetectHelper.SmartDetectAxis(dataContext);
                var result = timeAxisDetection as StepFunctionAutoDetectHelper.AxisFound;
                if (result == null)
                    throw new InvalidOperationException("Time axes detection failed");

                var timeAxis = await dataContext.GetDataAsync(result.AxisName);
                if (timeAxis.Length <2)
                    throw new InvalidOperationException("time axis length must be at least 2");
                if (timeAxis.GetType().GetElementType()!=typeof(Int16))
                    throw new InvalidOperationException("NDVI Yearly data handler now supports only Int16 time axis");
                Int16[] typedTimeAxis = (Int16[])timeAxis;
                Int16 step = (Int16)(typedTimeAxis[typedTimeAxis.Length - 1] - typedTimeAxis[typedTimeAxis.Length - 2]);
                Int16[] extendedAxis = typedTimeAxis.Concat(new Int16[] { (Int16)(typedTimeAxis[typedTimeAxis.Length - 1] + step)}).ToArray();

                var latIntegrator = await latIntegratorTask;
                var lonIntegrator = await lonIntegratorTask;
                var timeIntegrator = StepFunctionAutoDetectHelper.ConstructAverager(result.AxisKind, extendedAxis, result.BaseOffset);

                var baseUncertaintyProvider = new NoUncertaintyEvaluator();
                var coverageCheckUncertaintyEvaluator = new GridUncertaintyConventionsDecorator(baseUncertaintyProvider, latIntegrator, lonIntegrator, timeIntegrator);
                var variablePresenceCheckEvaluator = new VariablePresenceCheckDecorator(dataContext.StorageDefinition, coverageCheckUncertaintyEvaluator);

                var gridAggregator = new GridMeanAggregator(dataContext, timeIntegrator, latIntegrator, lonIntegrator, true);
                var clusteringAggregator = new GridClusteringDecorator(dataContext.StorageDefinition, gridAggregator, timeIntegrator, latIntegrator, lonIntegrator);

                return new DataHandler(dataContext, variablePresenceCheckEvaluator, clusteringAggregator);
            }

            private DataHandler(IStorageContext dataContext, IBatchUncertaintyEvaluator uncertaintyEvaluator, IBatchValueAggregator valueAggregator)
                : base(dataContext, uncertaintyEvaluator, valueAggregator)
            { }
        }    
}
