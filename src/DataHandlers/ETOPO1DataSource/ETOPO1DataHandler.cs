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

namespace ETOPO1DataSource
{
    public class ETOPO1DataHandler : BatchDataHandler
    {
        public static async Task<ETOPO1DataHandler> CreateAsync(IStorageContext dataContext)
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

            var gaussianFieldUncertaintyEvaluator = new SequentialTimeSpatialUncertaintyEvaluatorFacade(timeIntegrator, thinningLatIntegrator, thinningLonIntegrator, temporalVarianceCalculaator, spatialVarianceCalculator, baseNodeUncertainty);
            var coverageCheckUncertaintyEvaluator = new GridUncertaintyConventionsDecorator(gaussianFieldUncertaintyEvaluator, latIntegrator, lonIntegrator, timeIntegrator);
            var variablePresenceCheckEvaluator = new VariablePresenceCheckDecorator(dataContext.StorageDefinition,coverageCheckUncertaintyEvaluator);

            var gridAggregator = new GridMeanAggregator(dataContext, timeIntegrator, latIntegrator, lonIntegrator, false);
            var clusteringAggregator = new GridClusteringDecorator(dataContext.StorageDefinition,gridAggregator, timeIntegrator, latIntegrator, lonIntegrator);

            return new ETOPO1DataHandler(dataContext, variablePresenceCheckEvaluator, clusteringAggregator);
        }

        private ETOPO1DataHandler(IStorageContext dataContext, IBatchUncertaintyEvaluator uncertaintyEvaluator, IBatchValueAggregator valueAggregator)
            : base(dataContext, uncertaintyEvaluator, valueAggregator)
        { }

        class Etopo1BaseUnceratinty : INodeUncertaintyProvider
        {

            public double GetBaseNodeStandardDeviation(ICellRequest cell)
            {
                if (cell.VariableName != "Elevation")
                    return double.NaN;
                else
                {
                    double res;
                    if (cell.LatMin < -58.5 && cell.LatMax < -58.5) //Antarctica
                    {
                        // Antarctica RAMP datasource
                        res = 30;
                    }
                    else if (cell.LatMin > 61 && cell.LatMax > 61) //north
                    {
                        //GLOBE datasource
                        res = 30;
                    }
                    else
                    {
                        //SRTM30
                        if (cell.LatMin > -33 && cell.LatMax < 36 && cell.LonMin > 16 && cell.LonMax < 49) //Africa
                            res = 3.8;
                        else if (cell.LatMin > 19 && cell.LatMax < 80 && cell.LonMin > -8 && cell.LonMax < 180) //Eurasia
                            res = 3.7;
                        else if (cell.LatMin > -38 && cell.LatMax < -12 && cell.LonMin > 115 && cell.LonMax < 155) //Australia
                            res = 3.5;
                        else if (cell.LatMin > 14 && cell.LatMax < 74 && cell.LonMin > -171 && cell.LonMax < -50) //North America
                            res = 4.0;
                        else if (cell.LatMin > -55 && cell.LatMax < 13 && cell.LonMin > -87 && cell.LonMax < -34) //Sourth America
                            res = 4.1;
                        else if (cell.LatMin > -47 && cell.LatMax < -33 && cell.LonMin > 165 && cell.LonMax < 179) //New Zeland
                            res = 5.9;
                        else
                            res = 3.8;
                    }
                    return res;
                }
            }
        }
    }
}
