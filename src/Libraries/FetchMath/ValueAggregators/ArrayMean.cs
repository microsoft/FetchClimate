using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.ValueAggregators
{
    public class ArrayMean : IArrayAggregator
    {
        private static AutoRegistratingTraceSource ts = new AutoRegistratingTraceSource("ArrayMean", SourceLevels.All);
        private readonly ISpatGridIntegrator latAxisIntegrator;
        private readonly ISpatGridIntegrator lonAxisIntegrator;
        private readonly ITimeAxisIntegrator timeAxisIntegrator;
        private readonly IGridDataSetMetaData dataSetInfo;
        private readonly bool checkForMissingValues;


        public ArrayMean(IGridDataSetMetaData dataSetInfo, ITimeAxisIntegrator timeAxisIntegrator, ISpatGridIntegrator latAxisIntegrator, ISpatGridIntegrator lonAxisIntegrator, bool checkForMissingValues)
        {
            this.latAxisIntegrator = latAxisIntegrator;
            this.lonAxisIntegrator = lonAxisIntegrator;
            this.timeAxisIntegrator = timeAxisIntegrator;
            this.checkForMissingValues = checkForMissingValues;
            this.dataSetInfo = dataSetInfo;
        }

        public IEnumerable<double> Aggregate(string variable, Array prefetchedData, DataDomain prefetchDataDomain, IEnumerable<ICellRequest> cells)
        {
            Stopwatch calcSw = Stopwatch.StartNew();
            ts.TraceEvent(TraceEventType.Start, 3, "Integration started");
            double[] result = IntegrateBatch(variable, prefetchedData, prefetchDataDomain.Origin, PrepareIPsForCells(variable, cells)).ToArray();
            calcSw.Stop();
            ts.TraceEvent(TraceEventType.Stop, 3, string.Format("Integration took {0}. {1} values produced", calcSw.Elapsed, result.Length));
            return result;
        }

        public IEnumerable<double> IntegrateBatch(string variable, Array prefetchedData, int[] prefetchedDataOrigin, IEnumerable<IPs[]> ipsArrays)
        {
            switch (prefetchedData.Rank)
            {
                case 2:
                    return IntegrateBatch2D(variable, prefetchedData, prefetchedDataOrigin, ipsArrays);
                case 3:
                    return IntegrateBatch3D(variable, prefetchedData, prefetchedDataOrigin, ipsArrays);
                default:
                    throw new InvalidOperationException("Unexpected prefetched array rank. it is different from 2 or 3");
            }
        }

        private IEnumerable<double> IntegrateBatch2D(string variable, Array prefetchedData, int[] prefetchedDataOrigin, IEnumerable<IPs[]> ipsArrays)
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

            //invoking method from library
            if (effectiveMvCheck)
                foreach (var pointResult in Utils.ArrayMean.IntegrateSequenceWithMVs2D(variable, prefetchedData, missingValue, prefetchedDataOrigin, ipsArrays))
                    yield return (pointResult.Integral / pointResult.SumOfWeights);
            else
                foreach (var pointResult in Utils.ArrayMean.IntegrateSequence2D(variable, prefetchedData, prefetchedDataOrigin, ipsArrays))
                    yield return (pointResult.Integral / pointResult.SumOfWeights);
        }

        private IEnumerable<double> IntegrateBatch3D(string variable, Array prefetchedData, int[] prefetchedDataOrigin, IEnumerable<IPs[]> ipsArrays)
        {
            //TODO: move this method to the Utils.ArrayMean
            double integral = 0.0, weights = 0.0;

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
            int shape1 = prefetchedData.GetLength(1);
            int shape2 = prefetchedData.GetLength(2);

            GCHandle? capturedHandle;
            capturedHandle = GCHandle.Alloc(prefetchedData, GCHandleType.Pinned);
            IntPtr prefetchedDataPtr = capturedHandle.Value.AddrOfPinnedObject();
            try
            {
                if (effectiveMvCheck)
                {
                    if (dataType == typeof(double))
                    {
                        foreach (IPs[] ips in ipsArrays)
                        {
                            if (ips == null)// out of data cell
                            {
                                yield return double.NaN;
                                continue;
                            }
                            integral = 0.0; weights = 0.0;
                            IPsIntegral.Integrate3DDoubleMVsCheck(prefetchedDataPtr, shape1, shape2,
                                (double)missingValue,
                                ref weights, ref integral,
                                prefetchedDataOrigin[0], prefetchedDataOrigin[1], prefetchedDataOrigin[2],
                                ips[0].Indices, ips[1].Indices, ips[2].Indices,
                                ips[0].Weights, ips[1].Weights, ips[2].Weights);
                            if (weights == 0.0) //only missing values in the region
                                yield return double.NaN;
                            else
                                yield return (integral / weights);
                        }
                    }
                    else if (dataType == typeof(float))
                    {
                        foreach (IPs[] ips in ipsArrays)
                        {
                            if (ips == null)// out of data cell
                            {
                                yield return double.NaN;
                                continue;
                            }
                            integral = 0.0; weights = 0.0;
                            IPsIntegral.Integrate3DFloatMVsCheck(prefetchedDataPtr, shape1, shape2,
                                (float)missingValue,
                                ref weights, ref integral,
                                prefetchedDataOrigin[0], prefetchedDataOrigin[1], prefetchedDataOrigin[2],
                                ips[0].Indices, ips[1].Indices, ips[2].Indices,
                                ips[0].Weights, ips[1].Weights, ips[2].Weights);
                            if (weights == 0.0) //only missing values in the region
                                yield return double.NaN;
                            else
                                yield return (integral / weights);
                        }
                    }
                    else if (dataType == typeof(Int64))
                    {
                        foreach (IPs[] ips in ipsArrays)
                        {
                            if (ips == null)// out of data cell
                            {
                                yield return double.NaN;
                                continue;
                            }
                            integral = 0.0; weights = 0.0;
                            IPsIntegral.Integrate3DInt64MVsCheck(prefetchedDataPtr, shape1, shape2,
                                (Int64)missingValue,
                                ref weights, ref integral,
                                prefetchedDataOrigin[0], prefetchedDataOrigin[1], prefetchedDataOrigin[2],
                                ips[0].Indices, ips[1].Indices, ips[2].Indices,
                                ips[0].Weights, ips[1].Weights, ips[2].Weights);
                            if (weights == 0.0) //only missing values in the region
                                yield return double.NaN;
                            else
                                yield return (integral / weights);
                        }
                    }
                    else if (dataType == typeof(int))
                    {
                        foreach (IPs[] ips in ipsArrays)
                        {
                            if (ips == null)// out of data cell
                            {
                                yield return double.NaN;
                                continue;
                            }
                            integral = 0.0; weights = 0.0;
                            IPsIntegral.Integrate3DInt32MVsCheck(prefetchedDataPtr, shape1, shape2,
                                (int)missingValue,
                                ref weights, ref integral,
                                prefetchedDataOrigin[0], prefetchedDataOrigin[1], prefetchedDataOrigin[2],
                                ips[0].Indices, ips[1].Indices, ips[2].Indices,
                                ips[0].Weights, ips[1].Weights, ips[2].Weights);
                            if (weights == 0.0) //only missing values in the region
                                yield return double.NaN;
                            else
                                yield return (integral / weights);
                        }
                    }
                    else if (dataType == typeof(short))
                    {
                        foreach (IPs[] ips in ipsArrays)
                        {
                            if (ips == null)// out of data cell
                            {
                                yield return double.NaN;
                                continue;
                            }
                            integral = 0.0; weights = 0.0;
                            IPsIntegral.Integrate3DInt16MVsCheck(prefetchedDataPtr, shape1, shape2,
                                (short)missingValue,
                                ref weights, ref integral,
                                prefetchedDataOrigin[0], prefetchedDataOrigin[1], prefetchedDataOrigin[2],
                                ips[0].Indices, ips[1].Indices, ips[2].Indices,
                                ips[0].Weights, ips[1].Weights, ips[2].Weights);
                            if (weights == 0.0) //only missing values in the region
                                yield return double.NaN;
                            else
                                yield return (integral / weights);
                        }
                    }
                    else if (dataType == typeof(sbyte))
                    {
                        foreach (IPs[] ips in ipsArrays)
                        {
                            if (ips == null)// out of data cell
                            {
                                yield return double.NaN;
                                continue;
                            }
                            integral = 0.0; weights = 0.0;
                            IPsIntegral.Integrate3DInt8MVsCheck(prefetchedDataPtr, shape1, shape2,
                                (sbyte)missingValue,
                                ref weights, ref integral,
                                prefetchedDataOrigin[0], prefetchedDataOrigin[1], prefetchedDataOrigin[2],
                                ips[0].Indices, ips[1].Indices, ips[2].Indices,
                                ips[0].Weights, ips[1].Weights, ips[2].Weights);
                            if (weights == 0.0) //only missing values in the region
                                yield return double.NaN;
                            else
                                yield return (integral / weights);
                        }
                    }
                    else if (dataType == typeof(UInt64))
                    {
                        foreach (IPs[] ips in ipsArrays)
                        {
                            if (ips == null)// out of data cell
                            {
                                yield return double.NaN;
                                continue;
                            }
                            integral = 0.0; weights = 0.0;
                            IPsIntegral.Integrate3DUInt64MVsCheck(prefetchedDataPtr, shape1, shape2,
                                (UInt32)missingValue,
                                ref weights, ref integral,
                                prefetchedDataOrigin[0], prefetchedDataOrigin[1], prefetchedDataOrigin[2],
                                ips[0].Indices, ips[1].Indices, ips[2].Indices,
                                ips[0].Weights, ips[1].Weights, ips[2].Weights);
                            if (weights == 0.0) //only missing values in the region
                                yield return double.NaN;
                            else
                                yield return (integral / weights);
                        }
                    }
                    else if (dataType == typeof(UInt32))
                    {
                        foreach (IPs[] ips in ipsArrays)
                        {
                            if (ips == null)// out of data cell
                            {
                                yield return double.NaN;
                                continue;
                            }
                            integral = 0.0; weights = 0.0;
                            IPsIntegral.Integrate3DUInt32MVsCheck(prefetchedDataPtr, shape1, shape2,
                                (UInt32)missingValue,
                                ref weights, ref integral,
                                prefetchedDataOrigin[0], prefetchedDataOrigin[1], prefetchedDataOrigin[2],
                                ips[0].Indices, ips[1].Indices, ips[2].Indices,
                                ips[0].Weights, ips[1].Weights, ips[2].Weights);
                            if (weights == 0.0) //only missing values in the region
                                yield return double.NaN;
                            else
                                yield return (integral / weights);
                        }
                    }
                    else if (dataType == typeof(UInt16))
                    {
                        foreach (IPs[] ips in ipsArrays)
                        {
                            if (ips == null)// out of data cell
                            {
                                yield return double.NaN;
                                continue;
                            }
                            integral = 0.0; weights = 0.0;
                            IPsIntegral.Integrate3DUInt16MVsCheck(prefetchedDataPtr, shape1, shape2,
                                (UInt16)missingValue,
                                ref weights, ref integral,
                                prefetchedDataOrigin[0], prefetchedDataOrigin[1], prefetchedDataOrigin[2],
                                ips[0].Indices, ips[1].Indices, ips[2].Indices,
                                ips[0].Weights, ips[1].Weights, ips[2].Weights);
                            if (weights == 0.0) //only missing values in the region
                                yield return double.NaN;
                            else
                                yield return (integral / weights);
                        }
                    }
                    else if (dataType == typeof(byte))
                    {
                        foreach (IPs[] ips in ipsArrays)
                        {
                            if (ips == null)// out of data cell
                            {
                                yield return double.NaN;
                                continue;
                            }
                            integral = 0.0; weights = 0.0;
                            IPsIntegral.Integrate3DUInt8MVsCheck(prefetchedDataPtr, shape1, shape2,
                                (byte)missingValue,
                                ref weights, ref integral,
                                prefetchedDataOrigin[0], prefetchedDataOrigin[1], prefetchedDataOrigin[2],
                                ips[0].Indices, ips[1].Indices, ips[2].Indices,
                                ips[0].Weights, ips[1].Weights, ips[2].Weights);
                            if (weights == 0.0) //only missing values in the region
                                yield return double.NaN;
                            else
                                yield return (integral / weights);
                        }
                    }
                    else throw new NotSupportedException("data variable has unsupported type");
                }
                else
                {
                    if (dataType == typeof(double))
                    {
                        foreach (IPs[] ips in ipsArrays)
                        {
                            if (ips == null)// out of data cell
                            {
                                yield return double.NaN;
                                continue;
                            }
                            integral = 0.0; weights = 0.0;
                            IPsIntegral.Integrate3DDouble(prefetchedDataPtr, shape1, shape2,
                                ref weights, ref integral,
                                prefetchedDataOrigin[0], prefetchedDataOrigin[1], prefetchedDataOrigin[2],
                                ips[0].Indices, ips[1].Indices, ips[2].Indices,
                                ips[0].Weights, ips[1].Weights, ips[2].Weights);
                            if (weights == 0.0) //only missing values in the region
                                yield return double.NaN;
                            else
                                yield return (integral / weights);
                        }
                    }
                    else if (dataType == typeof(float))
                    {
                        foreach (IPs[] ips in ipsArrays)
                        {
                            if (ips == null)// out of data cell
                            {
                                yield return double.NaN;
                                continue;
                            }
                            integral = 0.0; weights = 0.0;
                            IPsIntegral.Integrate3DFloat(prefetchedDataPtr, shape1, shape2,
                                ref weights, ref integral,
                                prefetchedDataOrigin[0], prefetchedDataOrigin[1], prefetchedDataOrigin[2],
                                ips[0].Indices, ips[1].Indices, ips[2].Indices,
                                ips[0].Weights, ips[1].Weights, ips[2].Weights);
                            if (weights == 0.0) //only missing values in the region
                                yield return double.NaN;
                            else
                                yield return (integral / weights);
                        }
                    }
                    else if (dataType == typeof(Int64))
                    {
                        foreach (IPs[] ips in ipsArrays)
                        {
                            if (ips == null)// out of data cell
                            {
                                yield return double.NaN;
                                continue;
                            }
                            integral = 0.0; weights = 0.0;
                            IPsIntegral.Integrate3DInt64(prefetchedDataPtr, shape1, shape2,
                                ref weights, ref integral,
                                prefetchedDataOrigin[0], prefetchedDataOrigin[1], prefetchedDataOrigin[2],
                                ips[0].Indices, ips[1].Indices, ips[2].Indices,
                                ips[0].Weights, ips[1].Weights, ips[2].Weights);
                            if (weights == 0.0) //only missing values in the region
                                yield return double.NaN;
                            else
                                yield return (integral / weights);
                        }
                    }
                    else if (dataType == typeof(int))
                    {
                        foreach (IPs[] ips in ipsArrays)
                        {
                            if (ips == null)// out of data cell
                            {
                                yield return double.NaN;
                                continue;
                            }
                            integral = 0.0; weights = 0.0;
                            IPsIntegral.Integrate3DInt32(prefetchedDataPtr, shape1, shape2,
                                ref weights, ref integral,
                                prefetchedDataOrigin[0], prefetchedDataOrigin[1], prefetchedDataOrigin[2],
                                ips[0].Indices, ips[1].Indices, ips[2].Indices,
                                ips[0].Weights, ips[1].Weights, ips[2].Weights);
                            if (weights == 0.0) //only missing values in the region
                                yield return double.NaN;
                            else
                                yield return (integral / weights);
                        }
                    }
                    else if (dataType == typeof(short))
                    {
                        foreach (IPs[] ips in ipsArrays)
                        {
                            if (ips == null)// out of data cell
                            {
                                yield return double.NaN;
                                continue;
                            }
                            integral = 0.0; weights = 0.0;
                            IPsIntegral.Integrate3DInt16(prefetchedDataPtr, shape1, shape2,
                                ref weights, ref integral,
                                prefetchedDataOrigin[0], prefetchedDataOrigin[1], prefetchedDataOrigin[2],
                                ips[0].Indices, ips[1].Indices, ips[2].Indices,
                                ips[0].Weights, ips[1].Weights, ips[2].Weights);
                            if (weights == 0.0) //only missing values in the region
                                yield return double.NaN;
                            else
                                yield return (integral / weights);
                        }
                    }
                    else if (dataType == typeof(sbyte))
                    {
                        foreach (IPs[] ips in ipsArrays)
                        {
                            if (ips == null)// out of data cell
                            {
                                yield return double.NaN;
                                continue;
                            }
                            integral = 0.0; weights = 0.0;
                            IPsIntegral.Integrate3DInt8(prefetchedDataPtr, shape1, shape2,
                                ref weights, ref integral,
                                prefetchedDataOrigin[0], prefetchedDataOrigin[1], prefetchedDataOrigin[2],
                                ips[0].Indices, ips[1].Indices, ips[2].Indices,
                                ips[0].Weights, ips[1].Weights, ips[2].Weights);
                            if (weights == 0.0) //only missing values in the region
                                yield return double.NaN;
                            else
                                yield return (integral / weights);
                        }
                    }
                    else if (dataType == typeof(UInt64))
                    {
                        foreach (IPs[] ips in ipsArrays)
                        {
                            if (ips == null)// out of data cell
                            {
                                yield return double.NaN;
                                continue;
                            }
                            integral = 0.0; weights = 0.0;
                            IPsIntegral.Integrate3DUInt64(prefetchedDataPtr, shape1, shape2,
                                ref weights, ref integral,
                                prefetchedDataOrigin[0], prefetchedDataOrigin[1], prefetchedDataOrigin[2],
                                ips[0].Indices, ips[1].Indices, ips[2].Indices,
                                ips[0].Weights, ips[1].Weights, ips[2].Weights);
                            if (weights == 0.0) //only missing values in the region
                                yield return double.NaN;
                            else
                                yield return (integral / weights);
                        }
                    }
                    else if (dataType == typeof(UInt32))
                    {
                        foreach (IPs[] ips in ipsArrays)
                        {
                            if (ips == null)// out of data cell
                            {
                                yield return double.NaN;
                                continue;
                            }
                            integral = 0.0; weights = 0.0;
                            IPsIntegral.Integrate3DUInt32(prefetchedDataPtr, shape1, shape2,
                                ref weights, ref integral,
                                prefetchedDataOrigin[0], prefetchedDataOrigin[1], prefetchedDataOrigin[2],
                                ips[0].Indices, ips[1].Indices, ips[2].Indices,
                                ips[0].Weights, ips[1].Weights, ips[2].Weights);
                            if (weights == 0.0) //only missing values in the region
                                yield return double.NaN;
                            else
                                yield return (integral / weights);
                        }
                    }
                    else if (dataType == typeof(UInt16))
                    {
                        foreach (IPs[] ips in ipsArrays)
                        {
                            if (ips == null)// out of data cell
                            {
                                yield return double.NaN;
                                continue;
                            }
                            integral = 0.0; weights = 0.0;
                            IPsIntegral.Integrate3DUInt16(prefetchedDataPtr, shape1, shape2,
                                ref weights, ref integral,
                                prefetchedDataOrigin[0], prefetchedDataOrigin[1], prefetchedDataOrigin[2],
                                ips[0].Indices, ips[1].Indices, ips[2].Indices,
                                ips[0].Weights, ips[1].Weights, ips[2].Weights);
                            if (weights == 0.0) //only missing values in the region
                                yield return double.NaN;
                            else
                                yield return (integral / weights);
                        }
                    }
                    else if (dataType == typeof(byte))
                    {
                        foreach (IPs[] ips in ipsArrays)
                        {
                            if (ips == null)// out of data cell
                            {
                                yield return double.NaN;
                                continue;
                            }
                            integral = 0.0; weights = 0.0;
                            IPsIntegral.Integrate3DUInt8(prefetchedDataPtr, shape1, shape2,
                                ref weights, ref integral,
                                prefetchedDataOrigin[0], prefetchedDataOrigin[1], prefetchedDataOrigin[2],
                                ips[0].Indices, ips[1].Indices, ips[2].Indices,
                                ips[0].Weights, ips[1].Weights, ips[2].Weights);
                            if (weights == 0.0) //only missing values in the region
                                yield return double.NaN;
                            else
                                yield return (integral / weights);
                        }
                    }
                    else throw new NotSupportedException("data variable has unsupported type");
                }
            }
            finally
            {
                capturedHandle.Value.Free();
            }
        }

        private IEnumerable<IPs[]> PrepareIPsForCells(string variableName, IEnumerable<ICellRequest> t)
        {
            bool is2D = dataSetInfo.GetTimeDim(variableName) == -1;
            if (is2D)
                foreach (var item in t)
                {
                    IPs[] ipsArray = new IPs[2];
                    IPs lat = latAxisIntegrator.GetIPsForCell(item.LatMin, item.LatMax);
                    IPs lon = lonAxisIntegrator.GetIPsForCell(item.LonMin, item.LonMax);

                    ipsArray[dataSetInfo.GetLatitudeDim(variableName)] = lat;
                    ipsArray[dataSetInfo.GetLongitudeDim(variableName)] = lon;
                    yield return ipsArray;
                }
            else
                foreach (var item in t)
                {
                    IPs[] ipsArray = new IPs[3];
                    IPs time = timeAxisIntegrator.GetTempIPs(item.Time);
                    IPs lat = latAxisIntegrator.GetIPsForCell(item.LatMin, item.LatMax);
                    IPs lon = lonAxisIntegrator.GetIPsForCell(item.LonMin, item.LonMax);


                    ipsArray[dataSetInfo.GetTimeDim(variableName)] = time;
                    ipsArray[dataSetInfo.GetLatitudeDim(variableName)] = lat;
                    ipsArray[dataSetInfo.GetLongitudeDim(variableName)] = lon;
                    yield return ipsArray;
                }
        }


    }
}
