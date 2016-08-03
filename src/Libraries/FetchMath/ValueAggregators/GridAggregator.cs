using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    public class DataDomain
    {
        public int[] Origin { get; set; }
        public int[] Shape { get; set; }
    }

    public interface IArrayAggregator
    {
        IEnumerable<double> Aggregate(string variable, Array prefetchedData, DataDomain prefetchDataDomain, IEnumerable<ICellRequest> cells);
    }

    public interface IGridDataSetMetaData
    {
        int GetVariableRank(string variableName);
        int GetLatitudeDim(string variableName);
        int GetLongitudeDim(string variableName);
        int GetTimeDim(string variableName);

        object GetMissingValue(string variableName);
    }

    /// <summary>
    /// Extracts needed portion of raw data (rectangular box) and aggregates the data from it using arrayAggregator
    /// </summary>
    public class GridAggregator : IBatchValueAggregator
    {
        private static AutoRegistratingTraceSource ts = new AutoRegistratingTraceSource("GridAggregator", SourceLevels.All);
        private readonly ITimeAxisBoundingBoxCalculator timeBBCalc;
        private readonly ISpatGridBoundingBoxCalculator latBBcalc;
        private readonly ISpatGridBoundingBoxCalculator lonBBcalc;
        private readonly IGridDataSetMetaData metadata;
        private readonly IArrayAggregator arrayAggregator;        
        private readonly IDataStorage dataStorage;

        public GridAggregator(IDataStorage context, IGridDataSetMetaData metadata, IArrayAggregator arrayAggregator, ITimeAxisBoundingBoxCalculator timeBBCalc, ISpatGridBoundingBoxCalculator latBBcalc, ISpatGridBoundingBoxCalculator lonBBcalc)
        {
            this.latBBcalc = latBBcalc;
            this.lonBBcalc = lonBBcalc;
            this.timeBBCalc = timeBBCalc;
            this.arrayAggregator = arrayAggregator;           
            this.dataStorage = context;
            this.metadata = metadata;
        }

        protected DataDomain CalcDataDomain(ICellRequest[] cells, ITimeAxisBoundingBoxCalculator timeBBcalc, ISpatGridBoundingBoxCalculator latBBcalc, ISpatGridBoundingBoxCalculator lonBBcalc)
        {
            if (cells.Length == 0)
                throw new ArgumentException("cells array must contain elements");
            string variable = cells[0].VariableName;            
            //calculating bounding box
            IndexBoundingBox timeBB = new IndexBoundingBox();
            IndexBoundingBox latBB = new IndexBoundingBox();
            IndexBoundingBox lonBB = new IndexBoundingBox();

            Stopwatch bbCalc = Stopwatch.StartNew();
            ts.TraceEvent(TraceEventType.Start, 1, "Calculating bounding box");
            var cellArray = cells.ToArray();

            foreach (var geoCellTuple in cellArray)
            {
                IndexBoundingBox timeBB2 = timeBBcalc.GetBoundingBox(geoCellTuple.Time);
                IndexBoundingBox latBB2 = latBBcalc.GetBoundingBox(geoCellTuple.LatMin, geoCellTuple.LatMax);
                IndexBoundingBox lonBB2 = lonBBcalc.GetBoundingBox(geoCellTuple.LonMin, geoCellTuple.LonMax);

                timeBB = IndexBoundingBox.Union(timeBB, timeBB2);
                latBB = IndexBoundingBox.Union(latBB, latBB2);
                lonBB = IndexBoundingBox.Union(lonBB, lonBB2);
            }
            bbCalc.Stop();
            ts.TraceEvent(TraceEventType.Stop, 1, string.Format("Bounding box calculated in {0}", bbCalc.Elapsed));
            if (timeBB.IsSingular || latBB.IsSingular || lonBB.IsSingular)
            {
                ts.TraceEvent(TraceEventType.Information, 4, "Bounding box is singular. (all cells are out of the data) Returning NoData");
                return null;
            }
            else
            {
                ts.TraceEvent(TraceEventType.Information, 5, string.Format("{0} elements in bounding box",
                (timeBB.last - timeBB.first + 1) * (latBB.last - latBB.first + 1) * (lonBB.last - lonBB.first + 1)));
            }

            var dataDomain = ConstructDataDomain(variable,metadata, lonBB, latBB, timeBB);
            return dataDomain;
        }

        protected async static Task<Array> FetchRawDataAsync(IDataStorage storage, DataDomain dataDomain, string variable)
        {
            Stopwatch dataRequestSw = Stopwatch.StartNew();
            ts.TraceEvent(TraceEventType.Start, 2, "Extracting data from storage");
            var data = await storage.GetDataAsync(variable, dataDomain.Origin, null, dataDomain.Shape);
            dataRequestSw.Stop();
            long sizeMb = (data.Length *
                Marshal.SizeOf(data.GetType().GetElementType())) / 1024 / 1024;
            ts.TraceEvent(TraceEventType.Stop, 2, string.Format("Data extracted in {0}. {1}MB extracted", dataRequestSw.Elapsed, sizeMb));
            return data;
        }

        /// <summary>
        /// The method producing double array of mean values from the sequence of cells
        /// </summary>
        /// <param name="variable">A variable name (data source scope) to get the mean values for</param>
        /// <param name="cells">A sequence of cells to get the mean values for</param>
        /// <returns></returns>
        public async Task<double[]> AggregateCellsBatchAsync(IEnumerable<ICellRequest> cells)
        {
            ICellRequest first = cells.FirstOrDefault();
            if (first == null)
                return new double[0];
            else
            {
                var cellArray = cells.ToArray();
                var variable = first.VariableName;
                DataDomain dataDomain = CalcDataDomain(cellArray,this.timeBBCalc,this.latBBcalc,this.lonBBcalc);
                if (dataDomain == null)
                    return Enumerable.Repeat(double.NaN, cellArray.Length).ToArray();

                Array data = await FetchRawDataAsync(dataStorage, dataDomain, variable);
                
                double[] result = arrayAggregator.Aggregate(variable, data, dataDomain, cellArray).ToArray();
                return result;
            }
        }

        private DataDomain ConstructDataDomain(string variable, IGridDataSetMetaData dataSetInfo,  IndexBoundingBox lonBB, IndexBoundingBox latBB, IndexBoundingBox timeBB)
        {
            int[] origin = null, shape = null;
            int varaibleRank = dataSetInfo.GetVariableRank(variable);
            if (varaibleRank > 3)
                throw new InvalidOperationException("Variables with rank >3 are not supported");
            origin = new int[varaibleRank];
            shape = new int[varaibleRank];

            int latDimNum = dataSetInfo.GetLatitudeDim(variable);
            int lonDimNum = dataSetInfo.GetLongitudeDim(variable);
            int timeDimNum = dataSetInfo.GetTimeDim(variable); 

            origin[latDimNum] = latBB.first; shape[latDimNum] = latBB.last - latBB.first + 1;
            if (latDimNum != lonDimNum)
            {
                origin[lonDimNum] = lonBB.first; shape[lonDimNum] = lonBB.last - lonBB.first + 1;
            }
            if (timeDimNum != -1)
            {
                origin[timeDimNum] = timeBB.first; shape[timeDimNum] = timeBB.last - timeBB.first + 1;
            }

            return new DataDomain() { Origin = origin, Shape = shape };
        }
    }
}
