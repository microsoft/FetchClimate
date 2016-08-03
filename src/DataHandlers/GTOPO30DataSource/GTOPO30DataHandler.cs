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

namespace GTOPO30DataSource
{
    public class GTOPO30DataHandler : BatchDataHandler
    {
        public static async Task<GTOPO30DataHandler> CreateAsync(IStorageContext dataContext)
        {
            var storageDefinition = dataContext.StorageDefinition;
            var latIntegratorTask = LinearIntegratorsFactory.SmartConstructAsync(dataContext, IntegratorsFactoryHelpers.AutodetectLatName(storageDefinition));
            var lonIntegratorTask = LinearIntegratorsFactory.SmartConstructAsync(dataContext, IntegratorsFactoryHelpers.AutodetectLonName(storageDefinition));

            var timeIntegrator = new NoTimeAvgProcessing();
            var latIntegrator = await latIntegratorTask;
            var lonIntegrator = await lonIntegratorTask;

            var thinningLatIntegrator = new SpatGridIntegatorThinningDecorator(latIntegrator);
            var thinningLonIntegrator = new SpatGridIntegatorThinningDecorator(lonIntegrator);

            var baseNodeUncertainty = new Etopo1BaseUnceratinty();
            var temporalVarianceCalculaator = new ReducedDemensionLinearCombVarianceCalc();
            var spatialVarianceCalculator = new LinearCombinationOnSphereVarianceCalculator(new StorageContextMetadataSpatialVarianceExtractor(storageDefinition), latIntegrator, lonIntegrator);

            var bitMaskProvider = new EmbeddedResourceBitMaskProvider(typeof(GTOPO30DataHandler), "Microsoft.Research.Science.FetchClimate2.gtopo30.bf");
            
            var gaussianFieldUncertaintyEvaluator = new SequentialTimeSpatialUncertaintyEvaluatorFacade(timeIntegrator, thinningLatIntegrator, thinningLonIntegrator, temporalVarianceCalculaator, spatialVarianceCalculator, baseNodeUncertainty);
            var coverageCheckUncertaintyEvaluator = new GridUncertaintyConventionsDecorator(gaussianFieldUncertaintyEvaluator, latIntegrator, lonIntegrator, timeIntegrator);
            var landMaskEnabledUncertaintyEvaluator = new GridBitmaskDecorator(coverageCheckUncertaintyEvaluator, bitMaskProvider);
            var variablePresenceCheckEvaluator = new VariablePresenceCheckDecorator(dataContext.StorageDefinition, landMaskEnabledUncertaintyEvaluator);

            var gridAggregator = new GridMeanAggregator(dataContext, timeIntegrator, latIntegrator, lonIntegrator, true);
            var clusteringAggregator = new GridClusteringDecorator(dataContext.StorageDefinition,gridAggregator, timeIntegrator, latIntegrator, lonIntegrator);

            return new GTOPO30DataHandler(dataContext, variablePresenceCheckEvaluator, clusteringAggregator);
        }

        private GTOPO30DataHandler(IStorageContext dataContext, IBatchUncertaintyEvaluator uncertaintyEvaluator, IBatchValueAggregator valueAggregator)
            : base(dataContext, uncertaintyEvaluator, valueAggregator)
        { }

        class Etopo1BaseUnceratinty : INodeUncertaintyProvider
        {
            public double GetBaseNodeStandardDeviation(ICellRequest cell)
            {
                if (cell.VariableName != "elevation")
                    return double.NaN;
                else
                {
                    double res;
                    if (cell.LatMin < -60 && cell.LatMax < -60) //Antarctica
                    {
                        //ADD
                        res = 35.56;
                    }
                    else if (cell.LatMin > -7 && cell.LatMax < 8 && cell.LonMin > -77 && cell.LonMax < -41) //brasil
                    {
                        //IMW
                        res = 30;
                    }
                    else if (cell.LatMin > -60 && cell.LatMax < -7 && cell.LonMin > -94 && cell.LonMax < -25) //South America
                    {
                        //DTED + DCW
                        res = 57.5;
                    }
                    else if (cell.LatMin > -38 && cell.LatMax < 32 && cell.LonMin > -22 && cell.LonMax < 57) //Africa
                    {
                        //DTED + DCW
                        res = 57.5;
                    }
                    else if (cell.LatMin > -50 && cell.LatMax < -32 && cell.LonMin > 161 && cell.LonMax < 179) //New zeland
                    {
                        //N.Z. DEM
                        res = 9;
                    }
                    else if (cell.LatMin > -45 && cell.LatMax < 7 && cell.LonMin > 86 && cell.LonMax < 163) //Australia + Oceania
                    {
                        //DWC + 1/5 AMS
                        res = 108;
                    }
                    else if (cell.LatMin > 49 && cell.LatMax < 83.9 && cell.LonMin > -140 && cell.LonMax < -9) //Canada
                    {
                        //DWC + DTED
                        res = 71.3;
                    }
                    else
                    {
                        //USGS and DTED
                        res = 9;
                    }
                    return res;
                }
            }
        }
    }
}
