using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints;
using Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators;
using Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints.LinearCombination;
using Microsoft.Research.Science.FetchClimate2.Utils;
using Microsoft.Research.Science.FetchClimate2.Integrators.Temporal;

namespace Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints.TimeSeries
{   
    //WARNING!!!
    //TOO MANY COMPLEXITY AND RESPONSIBILITY HERE
    //REFACTORING NEEDED


    /// <summary>
    /// Extracts the array of station indices (in the dataset, data storage) that can be used to further calculation a mean value of the GeoCellTuple
    /// </summary>
    public interface IStationLocator
    {
        /// <summary>
        /// Extraxts the array of station indices (in the dataset, data storage) that can be used to further calculation a mean value of the GeoCellTuple
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        int[] GetRelevantStationsIndices(IGeoCell cell);
    }

    public class TimeIntegratorBasedAveragerFactory : ICellRequestMapFactory<RealValueNodes>        
    {
        private readonly ITimeAxisAvgProcessing timeIntegrator;
        private readonly IStationLocator stationLocator;
        private readonly double[] stationsLats, stationsLons;
        private readonly string stationDimName;
        private readonly IStorageContext storageContext;

        public static async Task<TimeIntegratorBasedAveragerFactory> CreateAsync(IStorageContext storageContext, ITimeAxisAvgProcessing timeIntegrator, IStationLocator stationLocator, string latAxisName = "autodetect", string lonAxisName = "autodetect")
        {
            if (latAxisName == "autodetect")
                latAxisName = IntegratorsFactoryHelpers.AutodetectLatName(storageContext.StorageDefinition);
            if (lonAxisName == "autodetect")
                lonAxisName = IntegratorsFactoryHelpers.AutodetectLonName(storageContext.StorageDefinition);

            //preparing virtual observation axes
            var latsTask = storageContext.GetDataAsync(latAxisName);
            var lonsTask = storageContext.GetDataAsync(lonAxisName);

            var stationsLatsAxis = await latsTask;
            var stationsLonsAxis = await lonsTask;

            int stationsCount = stationsLatsAxis.Length;

            Type latsArrType = stationsLatsAxis.GetType().GetElementType();
            Type lonsArrType = stationsLonsAxis.GetType().GetElementType();
            double[] stationsLats, stationsLons;
            if (latsArrType == typeof(double))
                stationsLats = (double[])stationsLatsAxis;
            else if (latsArrType == typeof(float))
                stationsLats = ((float[])stationsLatsAxis).Select(v => (double)v).ToArray();
            else
                throw new InvalidOperationException("The latitude coordinates of points are neither double nor float type");

            if (lonsArrType == typeof(double))
                stationsLons = (double[])stationsLonsAxis;
            else if (lonsArrType == typeof(float))
                stationsLons = ((float[])stationsLonsAxis).Select(v => (double)v).ToArray();
            else
                throw new InvalidOperationException("The longatude coordinates of points are neither double nor float type");


            return new TimeIntegratorBasedAveragerFactory(storageContext, timeIntegrator, stationLocator, stationsLats, stationsLons, storageContext.StorageDefinition.VariablesDimensions[latAxisName][0]);
        }

        private TimeIntegratorBasedAveragerFactory(IStorageContext context, ITimeAxisAvgProcessing timeIntegrator, IStationLocator stationLocator, double[] stationsLats, double[] stationsLons, string stationDimName)
        {
            this.timeIntegrator = timeIntegrator;
            this.stationLocator = stationLocator;
            this.stationsLats = stationsLats;
            this.stationsLons = stationsLons;
            this.stationDimName = stationDimName;
            this.storageContext = context;
        }

        public async Task<ICellRequestMap<RealValueNodes>> CreateAsync()
        {
            return new TimeIntegratorBasedAverager(storageContext, timeIntegrator, stationLocator, stationsLats, stationsLons, stationDimName);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TNodes"></typeparam>
    class TimeIntegratorBasedAverager : ICellRequestMap<RealValueNodes>
    {
        private readonly Dictionary<string, int> timeDimNumber = new Dictionary<string, int>();
        private readonly Dictionary<string, int> stationsDimNumber = new Dictionary<string, int>();
        private readonly DataRepresentationDictionary dataRepresentationDictionary;
        private readonly MissingValuesDictionary missingValuesDict;
        private readonly IReadOnlyDictionary<string, Type> varTypes;
        private readonly IStationLocator stationLocator;
        private readonly ITimeAxisAvgProcessing timeIntegrator;
        private readonly IDataStorage dataStorage;
        private static readonly AutoRegistratingTraceSource traceSource = new AutoRegistratingTraceSource("TimeIntegratorBasedAverager", SourceLevels.All);
        double[] stationsLats, stationsLons;


        internal TimeIntegratorBasedAverager(IStorageContext storageContext, ITimeAxisAvgProcessing timeIntegrator, IStationLocator stationLocator, double[] lats, double[] lons, string stationsDimName)
        {
            this.timeIntegrator = timeIntegrator;
            this.stationLocator = stationLocator;
            this.dataStorage = storageContext;
            this.stationsLats = lats;
            this.stationsLons = lons;

            IDataStorageDefinition storageDef = storageContext.StorageDefinition;

            //determining dimension order
            var varDimensions = storageContext.StorageDefinition.VariablesDimensions;            

            foreach (string dataVarName in varDimensions.Where(a => a.Value.Length == 2).Select(b => b.Key))
            {
                if (varDimensions[dataVarName][0] == stationsDimName)
                {
                    stationsDimNumber.Add(dataVarName, 0);
                    timeDimNumber.Add(dataVarName, 1);
                }
                else
                {
                    stationsDimNumber.Add(dataVarName, 1);
                    timeDimNumber.Add(dataVarName, 0);
                }
            }

            dataRepresentationDictionary = new DataRepresentationDictionary(storageContext.StorageDefinition);
            missingValuesDict = new MissingValuesDictionary(storageContext.StorageDefinition);
            varTypes = new Dictionary<string, Type>(storageContext.StorageDefinition.VariablesTypes);
        }

        public async Task<RealValueNodes> GetAsync(ICellRequest cell)
        {
            var variableName = cell.VariableName;
            var timeSegment = cell.Time;

            var bounds = timeIntegrator.GetBoundingBox(timeSegment);
            
            if(bounds.IsSingular)
            {
                double[] empty = new double[0];
                return new RealValueNodes(empty, empty, empty);
            }

            int[] origin = new int[2];
            int[] shape = new int[2];

            int timeDimNum = timeDimNumber[variableName];
            int stationDimNum = stationsDimNumber[variableName];
            

            int[] stationsIdxs = stationLocator.GetRelevantStationsIndices(cell);
            int stationMinIdx = stationsIdxs.Min(), stationMaxIdx = stationsIdxs.Max();

            int stationsCount = stationsIdxs.Length;

            //data fetching
            origin[timeDimNum] = bounds.first;
            origin[stationDimNum] = stationMinIdx;            
            shape[timeDimNum] = bounds.last-bounds.first+1;
            shape[stationDimNum] = stationMaxIdx-stationMinIdx+1;
            traceSource.TraceEvent(TraceEventType.Verbose, 2, "Requesting raw data for cell");
            var dataTask = dataStorage.GetDataAsync(variableName, origin, null, shape);

            //integration points calculation
            IPs timeIntegrationPoints = timeIntegrator.GetTempIPs(timeSegment);
            
            IPs[][] ips = new IPs[stationsCount][];
            for(int i=0;i<stationsCount;i++)
            {
                var curr = new IPs[2];
                ips[i] = curr;
                int idx = stationsIdxs[i];
                curr[stationDimNum] = new IPs() { Weights = new double[] { 1.0 }, Indices = new int[] { idx }, BoundingIndices = new IndexBoundingBox() { first = idx, last = idx } };
                curr[timeDimNum]=timeIntegrationPoints;                
            }
                       
            var data = await dataTask;

            object missingValue = missingValuesDict.GetMissingValue(variableName);

            if (missingValue != null && missingValue.GetType() != varTypes[variableName])
            {
                traceSource.TraceEvent(TraceEventType.Warning, 1, string.Format("A missing value of the variable \"{0}\"has different type from variable type. Ignoring MV definition",variableName));
                missingValue = null;
            }
            

            IEnumerable<Utils.IntegrationResult> obsResults;

            if(missingValue!=null)
                obsResults = Utils.ArrayMean.IntegrateSequenceWithMVs2D(variableName, data, missingValue, origin, ips);
            else
                obsResults = Utils.ArrayMean.IntegrateSequence2D(variableName, data, origin, ips);

            double scaleFacotor = dataRepresentationDictionary.ScaleFactors[variableName];
            double addOffset = dataRepresentationDictionary.AddOffsets[variableName];

            var scaledResults = obsResults.Select(r => (r.Integral / r.SumOfWeights)*scaleFacotor+addOffset).ToArray();
            
            int len = scaledResults.Length;
            List<double> valuesList = new List<double>(len);
            List<double> latsList = new List<double>(len);
            List<double> lonsList = new List<double>(len);
            for (int i = 0; i < len; i++)
            {
                if (!double.IsNaN(scaledResults[i]))
                {
                    valuesList.Add(scaledResults[i]);
                    latsList.Add(stationsLats[stationsIdxs[i]]);
                    lonsList.Add(stationsLons[stationsIdxs[i]]);
                }
            }

            return new RealValueNodes(latsList.ToArray(), lonsList.ToArray(), valuesList.ToArray());
        }
    }
}