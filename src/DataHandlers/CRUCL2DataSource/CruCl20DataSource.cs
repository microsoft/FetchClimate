using Microsoft.Research.Science.FetchClimate2.Integrators.Temporal;
using Microsoft.Research.Science.FetchClimate2.Integrators.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators;
using Microsoft.Research.Science.FetchClimate2.ValueAggregators;

namespace Microsoft.Research.Science.FetchClimate2.DataSources
{
    public sealed class CruCl20DataHandler : BatchDataHandler
    {
        private const string TEMPERATURE = "tmp";
        private const string PRATE = "pre";
        private const string RELHUM = "reh";
        private const string DTR = "dtr";
        private const string FROST = "frs";
        private const string WET = "rd0";
        private const string SUNPERCENTAGE = "sunp";
        private const string WIND = "wnd";

        public static async Task<CruCl20DataHandler> CreateAsync(IStorageContext dataContext)
        {
            var storageDefinition = dataContext.StorageDefinition;
            var latIntegratorTask = LinearIntegratorsFactory.SmartConstructAsync(dataContext, IntegratorsFactoryHelpers.AutodetectLatName(storageDefinition));
            var lonIntegratorTask = LinearIntegratorsFactory.SmartConstructAsync(dataContext, IntegratorsFactoryHelpers.AutodetectLonName(storageDefinition));

            var timeIntegrator = new TimeAxisAvgProcessing.MonthlyMeansOverExactYearsStepIntegratorFacade(1961,1990);
            var latIntegrator = await latIntegratorTask;
            var lonIntegrator = await lonIntegratorTask;

            var thinningLatIntegrator = new SpatGridIntegatorThinningDecorator(latIntegrator);
            var thinningLonIntegrator = new SpatGridIntegatorThinningDecorator(lonIntegrator);

            var baseNodeUncertainty = new CruBaseNodeUnceratinty();
            var temporalVarianceCalculaator = new LinearCombination1DVarianceCalc(new StorageContextMetadataTimeVarianceExtractor(storageDefinition, 12.0), timeIntegrator);
            var spatialVarianceCalculator = new LinearCombinationOnSphereVarianceCalculator(new StorageContextMetadataSpatialVarianceExtractor(storageDefinition), latIntegrator, lonIntegrator);

            var bitMaskProvider = new EmbeddedResourceBitMaskProvider(typeof(CruCl20DataHandler), "Microsoft.Research.Science.FetchClimate2.CruDataMask.bf");

            var gaussianFieldUncertaintyEvaluator = new SequentialTimeSpatialUncertaintyEvaluatorFacade(timeIntegrator, latIntegrator, lonIntegrator, temporalVarianceCalculaator, spatialVarianceCalculator, baseNodeUncertainty);
            var coverageCheckUncertaintyEvaluator = new GridUncertaintyConventionsDecorator(gaussianFieldUncertaintyEvaluator, latIntegrator, lonIntegrator, timeIntegrator);
            var landMaskEnabledUncertaintyEvaluator = new GridBitmaskDecorator(coverageCheckUncertaintyEvaluator, bitMaskProvider);
            var variablePresenceCheckEvaluator = new VariablePresenceCheckDecorator(dataContext.StorageDefinition, landMaskEnabledUncertaintyEvaluator);

            var gridAggregator = new GridMeanAggregator(dataContext, timeIntegrator, latIntegrator, lonIntegrator, true);
            var clusteringAggregator = new GridClusteringDecorator(dataContext.StorageDefinition, gridAggregator, timeIntegrator, latIntegrator, lonIntegrator);

            return new CruCl20DataHandler(dataContext, variablePresenceCheckEvaluator, clusteringAggregator);
        }

        private CruCl20DataHandler(IStorageContext dataContext, IBatchUncertaintyEvaluator uncertaintyEvaluator, IBatchValueAggregator valueAggregator)
            : base(dataContext, uncertaintyEvaluator, valueAggregator)
        { }

        class CruBaseNodeUnceratinty : INodeUncertaintyProvider
        {
            RTGCVtable table = new RTGCVtable(double.NaN);

            public double GetBaseNodeStandardDeviation(ICellRequest cell)
            {
                RTGCVtable effectiveTable = null;

                switch (cell.VariableName)
                {
                    case WET: effectiveTable = RTGCVtable.WetDays; break;
                    case DTR: effectiveTable = RTGCVtable.DurnalTempRange; break;
                    case FROST: effectiveTable = RTGCVtable.FrostDays; break;
                    case TEMPERATURE: effectiveTable = RTGCVtable.Temp; break;
                    case PRATE: effectiveTable = RTGCVtable.Precip; break;
                    case RELHUM: effectiveTable = RTGCVtable.RelHum; break;
                    case SUNPERCENTAGE: effectiveTable = RTGCVtable.PureSky; break;
                    case WIND: effectiveTable = RTGCVtable.WindSpeed; break;
                }

                if (effectiveTable != null)
                    return effectiveTable.GetRTGCV(cell.LatMin, cell.LatMax, cell.LonMin, cell.LonMax, cell.Time.FirstDay, cell.Time.LastDay, (cell.Time.LastYear == cell.Time.FirstYear) && DateTime.IsLeapYear(cell.Time.LastYear));
                else
                    return double.NaN;
            }
        }
    }
}
