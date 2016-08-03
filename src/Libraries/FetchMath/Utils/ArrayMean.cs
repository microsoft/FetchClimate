using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Utils
{
    public class IntegrationResult
    {
        /// <summary>
        /// The ammount of elements that were not summed as they are missing values
        /// </summary>
        //int MissingValuesCount; //TODO: to be added in future
        //int ElementsCount;
        public readonly double Integral;
        public readonly double SumOfWeights;

        public IntegrationResult(double integral, double sumOfWeights)
        {
            this.Integral = integral;
            this.SumOfWeights = sumOfWeights;
        }
    }


    /// <summary>
    /// Contains a static method with low-level optimized array values integration
    /// </summary>
    public static class ArrayMean
    {
        /// <summary>
        /// Produced integrated values for a sequence of integration points
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="prefetchedData"></param>
        /// <param name="prefetchedDataOrigin">The origin of prefetched array in terms of integration points domain</param>
        /// <param name="ipsArrays"></param>
        /// <returns></returns>
        public static IEnumerable<IntegrationResult> IntegrateSequenceWithMVs2D(string variable, Array prefetchedData, object missingValue,int[] prefetchedDataOrigin, IEnumerable<IPs[]> ipsArrays)
        {
            double integral = 0.0, weights = 0.0;            

            Type dataType = prefetchedData.GetType().GetElementType();
            
            int shape1 = prefetchedData.GetLength(1);

            GCHandle? capturedHandle;
            capturedHandle = GCHandle.Alloc(prefetchedData, GCHandleType.Pinned);
            IntPtr prefetchedDataPtr = capturedHandle.Value.AddrOfPinnedObject();

            try
            {               
                    if (dataType == typeof(double))
                    {
                        foreach (IPs[] ips in ipsArrays)
                        {
                            if (ips == null)// out of data cell
                            {
                                yield return new IntegrationResult(double.NaN, double.NaN);
                                continue;
                            }
                            integral = 0.0; weights = 0.0;
                            IPsIntegral.Integrate2DDoubleMVsCheck(prefetchedDataPtr, shape1,
                                (double)missingValue,
                                ref weights, ref integral,
                                prefetchedDataOrigin[0], prefetchedDataOrigin[1],
                                ips[0].Indices, ips[1].Indices,
                                ips[0].Weights, ips[1].Weights);
                            if (weights == 0.0) //only missing values in the region
                                yield return new IntegrationResult(double.NaN, double.NaN);
                            else
                                yield return new IntegrationResult(integral, weights);
                        }
                    }
                    else if (dataType == typeof(float))
                    {
                        foreach (IPs[] ips in ipsArrays)
                        {
                            if (ips == null)// out of data cell
                            {
                                yield return new IntegrationResult(double.NaN, double.NaN);
                                continue;
                            }
                            integral = 0.0; weights = 0.0;
                            IPsIntegral.Integrate2DFloatMVsCheck(prefetchedDataPtr, shape1,
                                (float)missingValue,
                                ref weights, ref integral,
                                prefetchedDataOrigin[0], prefetchedDataOrigin[1],
                                ips[0].Indices, ips[1].Indices,
                                ips[0].Weights, ips[1].Weights);
                            if (weights == 0.0) //only missing values in the region
                                yield return new IntegrationResult(double.NaN, double.NaN);
                            else
                                yield return new IntegrationResult(integral, weights);
                        }
                    }
                    else if (dataType == typeof(Int64))
                    {
                        foreach (IPs[] ips in ipsArrays)
                        {
                            if (ips == null)// out of data cell
                            {
                                yield return new IntegrationResult(double.NaN, double.NaN);
                                continue;
                            }
                            integral = 0.0; weights = 0.0;
                            IPsIntegral.Integrate2DInt64MVsCheck(prefetchedDataPtr, shape1,
                                (Int64)missingValue,
                                ref weights, ref integral,
                                prefetchedDataOrigin[0], prefetchedDataOrigin[1],
                                ips[0].Indices, ips[1].Indices,
                                ips[0].Weights, ips[1].Weights);
                            if (weights == 0.0) //only missing values in the region
                                yield return new IntegrationResult(double.NaN, double.NaN);
                            else
                                yield return new IntegrationResult(integral, weights);
                        }
                    }
                    else if (dataType == typeof(int))
                    {
                        foreach (IPs[] ips in ipsArrays)
                        {
                            if (ips == null)// out of data cell
                            {
                                yield return new IntegrationResult(double.NaN, double.NaN);
                                continue;
                            }
                            integral = 0.0; weights = 0.0;
                            IPsIntegral.Integrate2DInt32MVsCheck(prefetchedDataPtr, shape1,
                                (int)missingValue,
                                ref weights, ref integral,
                                prefetchedDataOrigin[0], prefetchedDataOrigin[1],
                                ips[0].Indices, ips[1].Indices,
                                ips[0].Weights, ips[1].Weights);
                            if (weights == 0.0) //only missing values in the region
                                yield return new IntegrationResult(double.NaN, double.NaN);
                            else
                                yield return new IntegrationResult(integral, weights);
                        }
                    }
                    else if (dataType == typeof(short))
                    {
                        foreach (IPs[] ips in ipsArrays)
                        {
                            if (ips == null)// out of data cell
                            {
                                yield return new IntegrationResult(double.NaN, double.NaN);
                                continue;
                            }
                            integral = 0.0; weights = 0.0;
                            IPsIntegral.Integrate2DInt16MVsCheck(prefetchedDataPtr, shape1,
                                (short)missingValue,
                                ref weights, ref integral,
                                prefetchedDataOrigin[0], prefetchedDataOrigin[1],
                                ips[0].Indices, ips[1].Indices,
                                ips[0].Weights, ips[1].Weights);
                            if (weights == 0.0) //only missing values in the region
                                yield return new IntegrationResult(double.NaN, double.NaN);
                            else
                                yield return new IntegrationResult(integral, weights);
                        }
                    }
                    else if (dataType == typeof(sbyte))
                    {
                        foreach (IPs[] ips in ipsArrays)
                        {
                            if (ips == null)// out of data cell
                            {
                                yield return new IntegrationResult(double.NaN, double.NaN);
                                continue;
                            }
                            integral = 0.0; weights = 0.0;
                            IPsIntegral.Integrate2DInt8MVsCheck(prefetchedDataPtr, shape1,
                                (sbyte)missingValue,
                                ref weights, ref integral,
                                prefetchedDataOrigin[0], prefetchedDataOrigin[1],
                                ips[0].Indices, ips[1].Indices,
                                ips[0].Weights, ips[1].Weights);
                            if (weights == 0.0) //only missing values in the region
                                yield return new IntegrationResult(double.NaN, double.NaN);
                            else
                                yield return new IntegrationResult(integral, weights);
                        }
                    }
                    else if (dataType == typeof(UInt64))
                    {
                        foreach (IPs[] ips in ipsArrays)
                        {
                            if (ips == null)// out of data cell
                            {
                                yield return new IntegrationResult(double.NaN, double.NaN);
                                continue;
                            }
                            integral = 0.0; weights = 0.0;
                            IPsIntegral.Integrate2DUInt64MVsCheck(prefetchedDataPtr, shape1,
                                (UInt64)missingValue,
                                ref weights, ref integral,
                                prefetchedDataOrigin[0], prefetchedDataOrigin[1],
                                ips[0].Indices, ips[1].Indices,
                                ips[0].Weights, ips[1].Weights);
                            if (weights == 0.0) //only missing values in the region
                                yield return new IntegrationResult(double.NaN, double.NaN);
                            else
                                yield return new IntegrationResult(integral, weights);
                        }
                    }
                    else if (dataType == typeof(UInt32))
                    {
                        foreach (IPs[] ips in ipsArrays)
                        {
                            if (ips == null)// out of data cell
                            {
                                yield return new IntegrationResult(double.NaN, double.NaN);
                                continue;
                            }
                            integral = 0.0; weights = 0.0;
                            IPsIntegral.Integrate2DUInt32MVsCheck(prefetchedDataPtr, shape1,
                                (UInt32)missingValue,
                                ref weights, ref integral,
                                prefetchedDataOrigin[0], prefetchedDataOrigin[1],
                                ips[0].Indices, ips[1].Indices,
                                ips[0].Weights, ips[1].Weights);
                            if (weights == 0.0) //only missing values in the region
                                yield return new IntegrationResult(double.NaN, double.NaN);
                            else
                                yield return new IntegrationResult(integral, weights);
                        }
                    }
                    else if (dataType == typeof(UInt16))
                    {
                        foreach (IPs[] ips in ipsArrays)
                        {
                            if (ips == null)// out of data cell
                            {
                                yield return new IntegrationResult(double.NaN, double.NaN);
                                continue;
                            }
                            integral = 0.0; weights = 0.0;
                            IPsIntegral.Integrate2DUInt16MVsCheck(prefetchedDataPtr, shape1,
                                (UInt16)missingValue,
                                ref weights, ref integral,
                                prefetchedDataOrigin[0], prefetchedDataOrigin[1],
                                ips[0].Indices, ips[1].Indices,
                                ips[0].Weights, ips[1].Weights);
                            if (weights == 0.0) //only missing values in the region
                                yield return new IntegrationResult(double.NaN, double.NaN);
                            else
                                yield return new IntegrationResult(integral, weights);
                        }
                    }
                    else if (dataType == typeof(byte))
                    {
                        foreach (IPs[] ips in ipsArrays)
                        {
                            if (ips == null)// out of data cell
                            {
                                yield return new IntegrationResult(double.NaN, double.NaN);
                                continue;
                            }
                            integral = 0.0; weights = 0.0;
                            IPsIntegral.Integrate2DUInt8MVsCheck(prefetchedDataPtr, shape1,
                                (byte)missingValue,
                                ref weights, ref integral,
                                prefetchedDataOrigin[0], prefetchedDataOrigin[1],
                                ips[0].Indices, ips[1].Indices,
                                ips[0].Weights, ips[1].Weights);
                            if (weights == 0.0) //only missing values in the region
                                yield return new IntegrationResult(double.NaN, double.NaN);
                            else
                                yield return new IntegrationResult(integral, weights);
                        }
                    }
                    else throw new NotSupportedException("data variable has unsupported type");
            }
            finally
            {
                capturedHandle.Value.Free();
            }
        }

        /// <summary>
        /// Produced integrated values for a sequence of integration points
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="prefetchedData"></param>
        /// <param name="prefetchedDataOrigin">The origin of prefetched array in terms of integration points domain</param>
        /// <param name="ipsArrays"></param>
        /// <returns></returns>
        public static IEnumerable<IntegrationResult> IntegrateSequence2D(string variable, Array prefetchedData, int[] prefetchedDataOrigin, IEnumerable<IPs[]> ipsArrays)
        {
            double integral = 0.0, weights = 0.0;

            Type dataType = prefetchedData.GetType().GetElementType();

            int shape1 = prefetchedData.GetLength(1);

            GCHandle? capturedHandle;
            capturedHandle = GCHandle.Alloc(prefetchedData, GCHandleType.Pinned);
            IntPtr prefetchedDataPtr = capturedHandle.Value.AddrOfPinnedObject();

            try
            {
                if (dataType == typeof(double))
                {
                    foreach (IPs[] ips in ipsArrays)
                    {
                        if (ips == null)// out of data cell
                        {
                            yield return new IntegrationResult(double.NaN, double.NaN);
                            continue;
                        }
                        integral = 0.0; weights = 0.0;
                        IPsIntegral.Integrate2DDouble(prefetchedDataPtr, shape1,
                            ref weights, ref integral,
                            prefetchedDataOrigin[0], prefetchedDataOrigin[1],
                            ips[0].Indices, ips[1].Indices,
                            ips[0].Weights, ips[1].Weights);
                        if (weights == 0.0) //only missing values in the region
                            yield return new IntegrationResult(double.NaN, double.NaN);
                        else
                            yield return new IntegrationResult(integral, weights);
                    }
                }
                else if (dataType == typeof(float))
                {
                    foreach (IPs[] ips in ipsArrays)
                    {
                        if (ips == null)// out of data cell
                        {
                            yield return new IntegrationResult(double.NaN, double.NaN);
                            continue;
                        }
                        integral = 0.0; weights = 0.0;
                        IPsIntegral.Integrate2DFloat(prefetchedDataPtr, shape1,
                            ref weights, ref integral,
                            prefetchedDataOrigin[0], prefetchedDataOrigin[1],
                            ips[0].Indices, ips[1].Indices,
                            ips[0].Weights, ips[1].Weights);
                        if (weights == 0.0) //only missing values in the region
                            yield return new IntegrationResult(double.NaN, double.NaN);
                        else
                            yield return new IntegrationResult(integral, weights);
                    }
                }
                else if (dataType == typeof(byte))
                {
                    foreach (IPs[] ips in ipsArrays)
                    {
                        if (ips == null)// out of data cell
                        {
                            yield return new IntegrationResult(double.NaN, double.NaN);
                            continue;
                        }
                        integral = 0.0; weights = 0.0;
                        IPsIntegral.Integrate2DUInt8(prefetchedDataPtr, shape1,
                            ref weights, ref integral,
                            prefetchedDataOrigin[0], prefetchedDataOrigin[1],
                            ips[0].Indices, ips[1].Indices,
                            ips[0].Weights, ips[1].Weights);
                        if (weights == 0.0) //only missing values in the region
                            yield return new IntegrationResult(double.NaN, double.NaN);
                        else
                            yield return new IntegrationResult(integral, weights);
                    }
                }
                else if (dataType == typeof(sbyte))
                {
                    foreach (IPs[] ips in ipsArrays)
                    {
                        if (ips == null)// out of data cell
                        {
                            yield return new IntegrationResult(double.NaN, double.NaN);
                            continue;
                        }
                        integral = 0.0; weights = 0.0;
                        IPsIntegral.Integrate2DInt8(prefetchedDataPtr, shape1,
                            ref weights, ref integral,
                            prefetchedDataOrigin[0], prefetchedDataOrigin[1],
                            ips[0].Indices, ips[1].Indices,
                            ips[0].Weights, ips[1].Weights);
                        if (weights == 0.0) //only missing values in the region
                            yield return new IntegrationResult(double.NaN, double.NaN);
                        else
                            yield return new IntegrationResult(integral, weights);
                    }
                }
                else if (dataType == typeof(UInt16))
                {
                    foreach (IPs[] ips in ipsArrays)
                    {
                        if (ips == null)// out of data cell
                        {
                            yield return new IntegrationResult(double.NaN, double.NaN);
                            continue;
                        }
                        integral = 0.0; weights = 0.0;
                        IPsIntegral.Integrate2DUInt16(prefetchedDataPtr, shape1,
                            ref weights, ref integral,
                            prefetchedDataOrigin[0], prefetchedDataOrigin[1],
                            ips[0].Indices, ips[1].Indices,
                            ips[0].Weights, ips[1].Weights);
                        if (weights == 0.0) //only missing values in the region
                            yield return new IntegrationResult(double.NaN, double.NaN);
                        else
                            yield return new IntegrationResult(integral, weights);
                    }
                }
                else if (dataType == typeof(Int16))
                {
                    foreach (IPs[] ips in ipsArrays)
                    {
                        if (ips == null)// out of data cell
                        {
                            yield return new IntegrationResult(double.NaN, double.NaN);
                            continue;
                        }
                        integral = 0.0; weights = 0.0;
                        IPsIntegral.Integrate2DInt16(prefetchedDataPtr, shape1,
                            ref weights, ref integral,
                            prefetchedDataOrigin[0], prefetchedDataOrigin[1],
                            ips[0].Indices, ips[1].Indices,
                            ips[0].Weights, ips[1].Weights);
                        if (weights == 0.0) //only missing values in the region
                            yield return new IntegrationResult(double.NaN, double.NaN);
                        else
                            yield return new IntegrationResult(integral, weights);
                    }
                }
                else if (dataType == typeof(int))
                {
                    foreach (IPs[] ips in ipsArrays)
                    {
                        if (ips == null)// out of data cell
                        {
                            yield return new IntegrationResult(double.NaN, double.NaN);
                            continue;
                        }
                        integral = 0.0; weights = 0.0;
                        IPsIntegral.Integrate2DInt32(prefetchedDataPtr, shape1,
                            ref weights, ref integral,
                            prefetchedDataOrigin[0], prefetchedDataOrigin[1],
                            ips[0].Indices, ips[1].Indices,
                            ips[0].Weights, ips[1].Weights);
                        if (weights == 0.0) //only missing values in the region
                            yield return new IntegrationResult(double.NaN, double.NaN);
                        else
                            yield return new IntegrationResult(integral, weights);
                    }
                }
                else if (dataType == typeof(uint))
                {
                    foreach (IPs[] ips in ipsArrays)
                    {
                        if (ips == null)// out of data cell
                        {
                            yield return new IntegrationResult(double.NaN, double.NaN);
                            continue;
                        }
                        integral = 0.0; weights = 0.0;
                        IPsIntegral.Integrate2DUInt32(prefetchedDataPtr, shape1,
                            ref weights, ref integral,
                            prefetchedDataOrigin[0], prefetchedDataOrigin[1],
                            ips[0].Indices, ips[1].Indices,
                            ips[0].Weights, ips[1].Weights);
                        if (weights == 0.0) //only missing values in the region
                            yield return new IntegrationResult(double.NaN, double.NaN);
                        else
                            yield return new IntegrationResult(integral, weights);
                    }
                }
                else if (dataType == typeof(Int64))
                {
                    foreach (IPs[] ips in ipsArrays)
                    {
                        if (ips == null)// out of data cell
                        {
                            yield return new IntegrationResult(double.NaN, double.NaN);
                            continue;
                        }
                        integral = 0.0; weights = 0.0;
                        IPsIntegral.Integrate2DInt64(prefetchedDataPtr, shape1,
                            ref weights, ref integral,
                            prefetchedDataOrigin[0], prefetchedDataOrigin[1],
                            ips[0].Indices, ips[1].Indices,
                            ips[0].Weights, ips[1].Weights);
                        if (weights == 0.0) //only missing values in the region
                            yield return new IntegrationResult(double.NaN, double.NaN);
                        else
                            yield return new IntegrationResult(integral, weights);
                    }
                }
                else if (dataType == typeof(UInt64))
                {
                    foreach (IPs[] ips in ipsArrays)
                    {
                        if (ips == null)// out of data cell
                        {
                            yield return new IntegrationResult(double.NaN, double.NaN);
                            continue;
                        }
                        integral = 0.0; weights = 0.0;
                        IPsIntegral.Integrate2DUInt64(prefetchedDataPtr, shape1,
                            ref weights, ref integral,
                            prefetchedDataOrigin[0], prefetchedDataOrigin[1],
                            ips[0].Indices, ips[1].Indices,
                            ips[0].Weights, ips[1].Weights);
                        if (weights == 0.0) //only missing values in the region
                            yield return new IntegrationResult(double.NaN, double.NaN);
                        else
                            yield return new IntegrationResult(integral, weights);
                    }
                }
                else throw new NotSupportedException("data variable has unsupported type");
            }
            finally
            {
                capturedHandle.Value.Free();
            }
        }
    }
}
