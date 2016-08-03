using System;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Research.Science.FetchClimate2.Integrators.Spatial;
using Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators;
using Microsoft.Research.Science.FetchClimate2.ValueAggregators;

namespace Microsoft.Research.Science.FetchClimate2.GFDLDataSource
{
    public class GFDLDataHandler : BatchDataHandler
    {
        const string timeAxisName = "time";

        const string TEMPERATURE = "tas";
        const string PRECIPITATION = "pr";

        public static async Task<GFDLDataHandler> CreateAsync(IStorageContext dataContext)
        {
            var storageDefinition = dataContext.StorageDefinition;
            var latIntegratorTask = LinearIntegratorsFactory.SmartConstructAsync(dataContext, IntegratorsFactoryHelpers.AutodetectLatName(storageDefinition));
            var lonIntegratorTask = LinearIntegratorsFactory.SmartConstructAsync(dataContext, IntegratorsFactoryHelpers.AutodetectLonName(storageDefinition));

            var latIntegrator = await latIntegratorTask;
            var lonIntegrator = await lonIntegratorTask;
            var timeAxis = ((double[])(dataContext.GetDataAsync(timeAxisName).Result)).Select(elem => (elem - 15.5) * 0.9863013698630137).ToArray(); // Shifting middle of the interval  to the beginning of the interval and convert from 365 to 360 years day
            int startYear = GetStartYear(dataContext, timeAxisName);
            int startDay = GetStartDay(dataContext, timeAxisName);
            var timeIntegrator = new TimeAxisAvgProcessing.TimeAxisAvgFacade(
                timeAxis,
                new TimeAxisProjections.ContinuousDays360(startYear, startDay),
                new WeightProviders.StepFunctionInterpolation(),
                new DataCoverageEvaluators.ContinousMeansCoverageEvaluator())
                ; // units = days since 2001-01-01 00:00:00

            var baseNodeUncertainty = new NoBaseUncertaintyProvider();
            var temporalVarianceCalculaator = new LinearCombination1DVarianceCalc(new StorageContextMetadataTimeVarianceExtractor(storageDefinition), timeIntegrator);
            var spatialVarianceCalculator = new LinearCombinationOnSphereVarianceCalculator(new StorageContextMetadataSpatialVarianceExtractor(storageDefinition), latIntegrator, lonIntegrator);


            var gaussianFieldUncertaintyEvaluator = new SequentialTimeSpatialUncertaintyEvaluatorFacade(timeIntegrator, latIntegrator, lonIntegrator,temporalVarianceCalculaator,spatialVarianceCalculator, baseNodeUncertainty);
            var coverageCheckUncertaintyEvaluator = new GridUncertaintyConventionsDecorator(gaussianFieldUncertaintyEvaluator, latIntegrator, lonIntegrator, timeIntegrator);
            var scaledUncertaintyEvaluator = new Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators.LinearTransformDecorator(coverageCheckUncertaintyEvaluator);
            var variablePresenceCheckEvaluator = new VariablePresenceCheckDecorator(dataContext.StorageDefinition,scaledUncertaintyEvaluator);

            scaledUncertaintyEvaluator.SetTranform(PRECIPITATION, v => v * 2592000.0);// overriding variable metadata to convert kg/m^2/s to mm/month (assuming month is 30 days)            
            //we do not need DegK to DegC in uncertainty as uncertainty is based on defference and does not depend on constant ofsets

            var gridAggregator = new GridMeanAggregator(dataContext, timeIntegrator, latIntegrator, lonIntegrator, true);
            var clusteringAggregator = new GridClusteringDecorator(dataContext.StorageDefinition, gridAggregator, timeIntegrator, latIntegrator, lonIntegrator);
            var scaledAggregator = new Microsoft.Research.Science.FetchClimate2.ValueAggregators.LinearTransformDecorator(dataContext, clusteringAggregator);

            scaledAggregator.SetAdditionalTranform(PRECIPITATION, v => v*2592000.0);// overriding variable metadata to convert kg/m^2/s to mm/month (assuming month is 30 days)
            scaledAggregator.SetAdditionalTranform(TEMPERATURE, v => v-273.15);// K to C

            return new GFDLDataHandler(dataContext, variablePresenceCheckEvaluator, scaledAggregator);
        }

        private GFDLDataHandler(IStorageContext dataContext, IBatchUncertaintyEvaluator uncertaintyEvaluator, IBatchValueAggregator valueAggregator)
            : base(dataContext, uncertaintyEvaluator, valueAggregator)
        { }

        static DateTime GetTimeAxisStart(IStorageContext ctx, string timeAxisName)
        {
            var metadata = ctx.StorageDefinition.VariablesMetadata[timeAxisName];
            if (metadata == null)
                return DateTime.MinValue;
            object units;
            if (!metadata.TryGetValue("units", out units))
                return DateTime.MinValue;
            string[] parts = units.ToString().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3 || parts[0] != "days" || parts[1] != "since")
                return DateTime.MinValue;
            DateTime result;
            if (!DateTime.TryParseExact(parts[2], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                return DateTime.MinValue;
            else
                return result;
        }

        static int GetStartYear(IStorageContext ctx, string timeAxisName)
        {
            var ds = GetTimeAxisStart(ctx, timeAxisName);
            if (ds == DateTime.MinValue)
            {
                Trace.WriteLine("GFDL data handler: unable to find time axis metadata. Start year is set to 2000");
                return 2000;
            }
            else
                return ds.Year;
        }

        static int GetStartDay(IStorageContext ctx, string timeAxisName)
        {
            var ds = GetTimeAxisStart(ctx, timeAxisName);
            if (ds == DateTime.MinValue)
            {
                Trace.WriteLine("GFDL data handler: unable to find time axis metadata. Start day is set to 1st of January");
                return 1;
            }
            else
                return ds.DayOfYear;
        }
    }
}
