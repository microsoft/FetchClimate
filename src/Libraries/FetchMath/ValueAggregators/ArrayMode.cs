using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.ValueAggregators
{
    public interface ISpatGridDataMaskProvider {
        int[] GetDataIndices(double lowerBound, double upperBound);
    }

    public interface ITimeAxisDataMaskProvider
    {
        int[] GetDataIndices(ITimeSegment ts);
    }

    public class ArrayMode : IArrayAggregator
    {
        private static AutoRegistratingTraceSource ts = new AutoRegistratingTraceSource("ArrayMode", SourceLevels.All);
        private readonly ITimeAxisDataMaskProvider timeAxisDataMaskProvider;
        private readonly ISpatGridDataMaskProvider latAxisDataMaskProvider, lonAxisDataMaskProvider;
        private readonly IGridDataSetMetaData dataSetInfo;
        private readonly bool checkForMissingValues;


        public ArrayMode(IGridDataSetMetaData dataSetInfo, ITimeAxisDataMaskProvider timeAxisDataMaskProvider, ISpatGridDataMaskProvider latAxisDataMaskProvider, ISpatGridDataMaskProvider lonAxisDataMaskProvider, bool checkForMissingValues)
        {
            this.timeAxisDataMaskProvider = timeAxisDataMaskProvider;
            this.latAxisDataMaskProvider = latAxisDataMaskProvider;
            this.lonAxisDataMaskProvider = lonAxisDataMaskProvider;
            this.checkForMissingValues = checkForMissingValues;
            this.dataSetInfo = dataSetInfo;
        }

        public IEnumerable<double> Aggregate(string variable, Array prefetchedData, DataDomain prefetchDataDomain, IEnumerable<ICellRequest> cells)
        {
            Stopwatch calcSw = Stopwatch.StartNew();
            ts.TraceEvent(TraceEventType.Start, 3, "Mode calculation started");
            double[] result = CalculateMode(variable, prefetchedData, prefetchDataDomain.Origin, PrepareIndicesForCells(variable, cells)).ToArray();
            calcSw.Stop();
            ts.TraceEvent(TraceEventType.Stop, 3, string.Format("Mode calculation {0}. {1} values produced", calcSw.Elapsed, result.Length));
            return result;
        }

        public IEnumerable<double> CalculateMode(string variable, Array prefetchedData, int[] prefetchedDataOrigin, IEnumerable<int[][]> idxArrays)
        {
            Type dataType = prefetchedData.GetType().GetElementType();

            bool effectiveMvCheck = checkForMissingValues;

            object missingValue = dataSetInfo.GetMissingValue(variable);
            if (missingValue == null)
            {
                if (dataType == typeof(double)) missingValue = double.NaN;
                else if (dataType == typeof(float)) missingValue = float.NaN;
                else
                    effectiveMvCheck = false; //switching off MV check if no MV information is available
            }

            switch (prefetchedData.Rank)
            {
                //case 2:                    
                //    if (effectiveMvCheck)
                //        return Utils.ArrayMode.FindModeSequenceWithMVs2D(variable, prefetchedData, missingValue, prefetchedDataOrigin, idxArrays);
                //    else
                //        return Utils.ArrayMode.FindModeSequence2D(variable, prefetchedData, prefetchedDataOrigin, idxArrays);
                case 3:
                    if (effectiveMvCheck)
                        return Utils.ArrayMode.FindModeSequence3D(variable, prefetchedData, prefetchedDataOrigin, idxArrays, missingValue);
                    else
                        return Utils.ArrayMode.FindModeSequence3D(variable, prefetchedData, prefetchedDataOrigin, idxArrays);
                default:
                    throw new InvalidOperationException("Unexpected prefetched array rank. Now only 3D varaibles are supported");
            }
        }

        private IEnumerable<int[][]> PrepareIndicesForCells(string variableName, IEnumerable<ICellRequest> t)
        {
            bool is2D = dataSetInfo.GetTimeDim(variableName) == -1;
            if (is2D)
                foreach (var item in t)
                {
                    int[][] idxArray = new int[2][];
                    int[] latIndices = latAxisDataMaskProvider.GetDataIndices(item.LatMin, item.LatMax);
                    int[] lonIndices = lonAxisDataMaskProvider.GetDataIndices(item.LonMin, item.LonMax);

                    idxArray[dataSetInfo.GetLatitudeDim(variableName)] = latIndices;
                    idxArray[dataSetInfo.GetLongitudeDim(variableName)] = lonIndices;
                    yield return idxArray;
                }
            else
                foreach (var item in t)
                {
                    int[][] idxArray = new int[3][];
                    int[] time = timeAxisDataMaskProvider.GetDataIndices(item.Time);
                    int[] lat = latAxisDataMaskProvider.GetDataIndices(item.LatMin, item.LatMax);
                    int[] lon = lonAxisDataMaskProvider.GetDataIndices(item.LonMin, item.LonMax);


                    idxArray[dataSetInfo.GetTimeDim(variableName)] = time;
                    idxArray[dataSetInfo.GetLatitudeDim(variableName)] = lat;
                    idxArray[dataSetInfo.GetLongitudeDim(variableName)] = lon;
                    yield return idxArray;
                }
        }
    }
}
