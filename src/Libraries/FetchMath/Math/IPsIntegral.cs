using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Research.Science.FetchClimate2
{
    //We can't use generic parameter here. E.g. "struct, IConvertible, IComparable<T>,IFormattable" type constraint doe not provide getting the pointer of the type
    public static class IPsIntegral
    {
        #region 3D domains

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>
        /// <param name="k_data_len">third dimesion length of the prefetched array</param>
        /// <param name="missingValue"></param>
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>        
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>
        /// <param name="k_data_offset">an offset of the prefetched array in the raw dataset along the hird dimension</param>
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_k">an indeces of third dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>
        /// <param name="weights_k"></param>
        public static unsafe void Integrate3DDoubleMVsCheck(IntPtr data, int j_data_len, int k_data_len, double missingValue, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int k_data_offset, int[] indeces_i, int[] indeces_j, int[] indeces_k, double[] weights_i, double[] weights_j, double[] weights_k)
        {
            double* dataPtr = (double*)data;
            double weigth, weights_ij;
            double v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int k_len = indeces_k.Length;
            int i_row_offset, i_j_offset, i_value, j_value, k_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * k_data_len * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    i_j_offset = (j_value * k_data_len) + i_row_offset;
                    weights_ij = weights_i[i] * weights_j[j];
                    for (int k = 0; k < k_len; k++)
                    {
                        k_value = indeces_k[k] - k_data_offset;
                        weigth = weights_ij * weights_k[k];
                        v = dataPtr[i_j_offset + k_value];
                        if (v != missingValue)
                        {
                            integral += weigth * v;
                            sumOfTheWeights += weigth;

                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>
        /// <param name="k_data_len">third dimesion length of the prefetched array</param>        
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>
        /// <param name="k_data_offset">an offset of the prefetched array in the raw dataset along the hird dimension</param>
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_k">an indeces of third dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>
        /// <param name="weights_k"></param>
        public static unsafe void Integrate3DDouble(IntPtr data, int j_data_len, int k_data_len, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int k_data_offset, int[] indeces_i, int[] indeces_j, int[] indeces_k, double[] weights_i, double[] weights_j, double[] weights_k)
        {
            double* dataPtr = (double*)data;
            double weigth, weights_ij;
            double v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int k_len = indeces_k.Length;
            int i_row_offset, i_j_offset, i_value, j_value, k_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * k_data_len * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    i_j_offset = (j_value * k_data_len) + i_row_offset;
                    weights_ij = weights_i[i] * weights_j[j];
                    for (int k = 0; k < k_len; k++)
                    {
                        k_value = indeces_k[k] - k_data_offset;
                        weigth = weights_ij * weights_k[k];
                        v = dataPtr[i_j_offset + k_value];

                        integral += weigth * v;
                        sumOfTheWeights += weigth;
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>
        /// <param name="k_data_len">third dimesion length of the prefetched array</param>
        /// <param name="missingValue"></param>
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>        
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>
        /// <param name="k_data_offset">an offset of the prefetched array in the raw dataset along the hird dimension</param>
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_k">an indeces of third dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>
        /// <param name="weights_k"></param>
        public static unsafe void Integrate3DFloatMVsCheck(IntPtr data, int j_data_len, int k_data_len, float missingValue, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int k_data_offset, int[] indeces_i, int[] indeces_j, int[] indeces_k, double[] weights_i, double[] weights_j, double[] weights_k)
        {
            float* dataPtr = (float*)data;
            double weigth, weights_ij;
            float v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int k_len = indeces_k.Length;
            int i_row_offset, i_j_offset, i_value, j_value, k_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * k_data_len * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    i_j_offset = (j_value * k_data_len) + i_row_offset;
                    weights_ij = weights_i[i] * weights_j[j];
                    for (int k = 0; k < k_len; k++)
                    {
                        k_value = indeces_k[k] - k_data_offset;
                        weigth = weights_ij * weights_k[k];
                        v = dataPtr[i_j_offset + k_value];
                        if (v != missingValue)
                        {
                            integral += weigth * v;
                            sumOfTheWeights += weigth;

                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>
        /// <param name="k_data_len">third dimesion length of the prefetched array</param>        
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>
        /// <param name="k_data_offset">an offset of the prefetched array in the raw dataset along the hird dimension</param>
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_k">an indeces of third dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>
        /// <param name="weights_k"></param>
        public static unsafe void Integrate3DFloat(IntPtr data, int j_data_len, int k_data_len, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int k_data_offset, int[] indeces_i, int[] indeces_j, int[] indeces_k, double[] weights_i, double[] weights_j, double[] weights_k)
        {
            float* dataPtr = (float*)data;
            double weigth, weights_ij;
            float v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int k_len = indeces_k.Length;
            int i_row_offset, i_j_offset, i_value, j_value, k_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * k_data_len * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    i_j_offset = (j_value * k_data_len) + i_row_offset;
                    weights_ij = weights_i[i] * weights_j[j];
                    for (int k = 0; k < k_len; k++)
                    {
                        k_value = indeces_k[k] - k_data_offset;
                        weigth = weights_ij * weights_k[k];
                        v = dataPtr[i_j_offset + k_value];

                        integral += weigth * v;
                        sumOfTheWeights += weigth;
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>
        /// <param name="k_data_len">third dimesion length of the prefetched array</param>
        /// <param name="missingValue"></param>
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>        
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>
        /// <param name="k_data_offset">an offset of the prefetched array in the raw dataset along the hird dimension</param>
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_k">an indeces of third dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>
        /// <param name="weights_k"></param>
        public static unsafe void Integrate3DInt64MVsCheck(IntPtr data, int j_data_len, int k_data_len, Int64 missingValue, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int k_data_offset, int[] indeces_i, int[] indeces_j, int[] indeces_k, double[] weights_i, double[] weights_j, double[] weights_k)
        {
            Int64* dataPtr = (Int64*)data;
            double weigth, weights_ij;
            Int64 v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int k_len = indeces_k.Length;
            int i_row_offset, i_j_offset, i_value, j_value, k_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * k_data_len * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    i_j_offset = (j_value * k_data_len) + i_row_offset;
                    weights_ij = weights_i[i] * weights_j[j];
                    for (int k = 0; k < k_len; k++)
                    {
                        k_value = indeces_k[k] - k_data_offset;
                        weigth = weights_ij * weights_k[k];
                        v = dataPtr[i_j_offset + k_value];
                        if (v != missingValue)
                        {
                            integral += weigth * v;
                            sumOfTheWeights += weigth;

                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>
        /// <param name="k_data_len">third dimesion length of the prefetched array</param>        
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>
        /// <param name="k_data_offset">an offset of the prefetched array in the raw dataset along the hird dimension</param>
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_k">an indeces of third dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>
        /// <param name="weights_k"></param>
        public static unsafe void Integrate3DInt64(IntPtr data, int j_data_len, int k_data_len, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int k_data_offset, int[] indeces_i, int[] indeces_j, int[] indeces_k, double[] weights_i, double[] weights_j, double[] weights_k)
        {
            Int64* dataPtr = (Int64*)data;
            double weigth, weights_ij;
            Int64 v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int k_len = indeces_k.Length;
            int i_row_offset, i_j_offset, i_value, j_value, k_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * k_data_len * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    i_j_offset = (j_value * k_data_len) + i_row_offset;
                    weights_ij = weights_i[i] * weights_j[j];
                    for (int k = 0; k < k_len; k++)
                    {
                        k_value = indeces_k[k] - k_data_offset;
                        weigth = weights_ij * weights_k[k];
                        v = dataPtr[i_j_offset + k_value];

                        integral += weigth * v;
                        sumOfTheWeights += weigth;
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>
        /// <param name="k_data_len">third dimesion length of the prefetched array</param>
        /// <param name="missingValue"></param>
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>        
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>
        /// <param name="k_data_offset">an offset of the prefetched array in the raw dataset along the hird dimension</param>
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_k">an indeces of third dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>
        /// <param name="weights_k"></param>
        public static unsafe void Integrate3DUInt64MVsCheck(IntPtr data, int j_data_len, int k_data_len, UInt64 missingValue, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int k_data_offset, int[] indeces_i, int[] indeces_j, int[] indeces_k, double[] weights_i, double[] weights_j, double[] weights_k)
        {
            UInt64* dataPtr = (UInt64*)data;
            double weigth, weights_ij;
            UInt64 v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int k_len = indeces_k.Length;
            int i_row_offset, i_j_offset, i_value, j_value, k_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * k_data_len * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    i_j_offset = (j_value * k_data_len) + i_row_offset;
                    weights_ij = weights_i[i] * weights_j[j];
                    for (int k = 0; k < k_len; k++)
                    {
                        k_value = indeces_k[k] - k_data_offset;
                        weigth = weights_ij * weights_k[k];
                        v = dataPtr[i_j_offset + k_value];
                        if (v != missingValue)
                        {
                            integral += weigth * v;
                            sumOfTheWeights += weigth;

                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>
        /// <param name="k_data_len">third dimesion length of the prefetched array</param>        
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>
        /// <param name="k_data_offset">an offset of the prefetched array in the raw dataset along the hird dimension</param>
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_k">an indeces of third dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>
        /// <param name="weights_k"></param>
        public static unsafe void Integrate3DUInt64(IntPtr data, int j_data_len, int k_data_len, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int k_data_offset, int[] indeces_i, int[] indeces_j, int[] indeces_k, double[] weights_i, double[] weights_j, double[] weights_k)
        {
            UInt64* dataPtr = (UInt64*)data;
            double weigth, weights_ij;
            UInt64 v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int k_len = indeces_k.Length;
            int i_row_offset, i_j_offset, i_value, j_value, k_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * k_data_len * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    i_j_offset = (j_value * k_data_len) + i_row_offset;
                    weights_ij = weights_i[i] * weights_j[j];
                    for (int k = 0; k < k_len; k++)
                    {
                        k_value = indeces_k[k] - k_data_offset;
                        weigth = weights_ij * weights_k[k];
                        v = dataPtr[i_j_offset + k_value];

                        integral += weigth * v;
                        sumOfTheWeights += weigth;
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>
        /// <param name="k_data_len">third dimesion length of the prefetched array</param>
        /// <param name="missingValue"></param>
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>        
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>
        /// <param name="k_data_offset">an offset of the prefetched array in the raw dataset along the hird dimension</param>
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_k">an indeces of third dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>
        /// <param name="weights_k"></param>
        public static unsafe void Integrate3DInt32MVsCheck(IntPtr data, int j_data_len, int k_data_len, int missingValue, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int k_data_offset, int[] indeces_i, int[] indeces_j, int[] indeces_k, double[] weights_i, double[] weights_j, double[] weights_k)
        {
            int* dataPtr = (int*)data;
            double weigth, weights_ij;
            int v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int k_len = indeces_k.Length;
            int i_row_offset, i_j_offset, i_value, j_value, k_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * k_data_len * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    i_j_offset = (j_value * k_data_len) + i_row_offset;
                    weights_ij = weights_i[i] * weights_j[j];
                    for (int k = 0; k < k_len; k++)
                    {
                        k_value = indeces_k[k] - k_data_offset;
                        weigth = weights_ij * weights_k[k];
                        v = dataPtr[i_j_offset + k_value];
                        if (v != missingValue)
                        {
                            integral += weigth * v;
                            sumOfTheWeights += weigth;

                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>
        /// <param name="k_data_len">third dimesion length of the prefetched array</param>        
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>
        /// <param name="k_data_offset">an offset of the prefetched array in the raw dataset along the hird dimension</param>
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_k">an indeces of third dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>
        /// <param name="weights_k"></param>
        public static unsafe void Integrate3DInt32(IntPtr data, int j_data_len, int k_data_len, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int k_data_offset, int[] indeces_i, int[] indeces_j, int[] indeces_k, double[] weights_i, double[] weights_j, double[] weights_k)
        {
            int* dataPtr = (int*)data;
            double weigth, weights_ij;
            int v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int k_len = indeces_k.Length;
            int i_row_offset, i_j_offset, i_value, j_value, k_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * k_data_len * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    i_j_offset = (j_value * k_data_len) + i_row_offset;
                    weights_ij = weights_i[i] * weights_j[j];
                    for (int k = 0; k < k_len; k++)
                    {
                        k_value = indeces_k[k] - k_data_offset;
                        weigth = weights_ij * weights_k[k];
                        v = dataPtr[i_j_offset + k_value];

                        integral += weigth * v;
                        sumOfTheWeights += weigth;
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>
        /// <param name="k_data_len">third dimesion length of the prefetched array</param>
        /// <param name="missingValue"></param>
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>        
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>
        /// <param name="k_data_offset">an offset of the prefetched array in the raw dataset along the hird dimension</param>
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_k">an indeces of third dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>
        /// <param name="weights_k"></param>
        public static unsafe void Integrate3DUInt32MVsCheck(IntPtr data, int j_data_len, int k_data_len, UInt32 missingValue, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int k_data_offset, int[] indeces_i, int[] indeces_j, int[] indeces_k, double[] weights_i, double[] weights_j, double[] weights_k)
        {
            UInt32* dataPtr = (UInt32*)data;
            double weigth, weights_ij;
            UInt32 v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int k_len = indeces_k.Length;
            int i_row_offset, i_j_offset, i_value, j_value, k_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * k_data_len * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    i_j_offset = (j_value * k_data_len) + i_row_offset;
                    weights_ij = weights_i[i] * weights_j[j];
                    for (int k = 0; k < k_len; k++)
                    {
                        k_value = indeces_k[k] - k_data_offset;
                        weigth = weights_ij * weights_k[k];
                        v = dataPtr[i_j_offset + k_value];
                        if (v != missingValue)
                        {
                            integral += weigth * v;
                            sumOfTheWeights += weigth;

                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>
        /// <param name="k_data_len">third dimesion length of the prefetched array</param>        
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>
        /// <param name="k_data_offset">an offset of the prefetched array in the raw dataset along the hird dimension</param>
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_k">an indeces of third dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>
        /// <param name="weights_k"></param>
        public static unsafe void Integrate3DUInt32(IntPtr data, int j_data_len, int k_data_len, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int k_data_offset, int[] indeces_i, int[] indeces_j, int[] indeces_k, double[] weights_i, double[] weights_j, double[] weights_k)
        {
            UInt32* dataPtr = (UInt32*)data;
            double weigth, weights_ij;
            UInt32 v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int k_len = indeces_k.Length;
            int i_row_offset, i_j_offset, i_value, j_value, k_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * k_data_len * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    i_j_offset = (j_value * k_data_len) + i_row_offset;
                    weights_ij = weights_i[i] * weights_j[j];
                    for (int k = 0; k < k_len; k++)
                    {
                        k_value = indeces_k[k] - k_data_offset;
                        weigth = weights_ij * weights_k[k];
                        v = dataPtr[i_j_offset + k_value];

                        integral += weigth * v;
                        sumOfTheWeights += weigth;
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>
        /// <param name="k_data_len">third dimesion length of the prefetched array</param>
        /// <param name="missingValue"></param>
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>        
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>
        /// <param name="k_data_offset">an offset of the prefetched array in the raw dataset along the hird dimension</param>
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_k">an indeces of third dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>
        /// <param name="weights_k"></param>
        public static unsafe void Integrate3DInt16MVsCheck(IntPtr data, int j_data_len, int k_data_len, short missingValue, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int k_data_offset, int[] indeces_i, int[] indeces_j, int[] indeces_k, double[] weights_i, double[] weights_j, double[] weights_k)
        {
            short* dataPtr = (short*)data;
            double weigth, weights_ij;
            short v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int k_len = indeces_k.Length;
            int i_row_offset, i_j_offset, i_value, j_value, k_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * k_data_len * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    i_j_offset = (j_value * k_data_len) + i_row_offset;
                    weights_ij = weights_i[i] * weights_j[j];
                    for (int k = 0; k < k_len; k++)
                    {
                        k_value = indeces_k[k] - k_data_offset;
                        weigth = weights_ij * weights_k[k];
                        v = dataPtr[i_j_offset + k_value];
                        if (v != missingValue)
                        {
                            integral += weigth * v;
                            sumOfTheWeights += weigth;

                        }
                    }
                }
            }
        }
       
        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>
        /// <param name="k_data_len">third dimesion length of the prefetched array</param>        
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>
        /// <param name="k_data_offset">an offset of the prefetched array in the raw dataset along the hird dimension</param>
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_k">an indeces of third dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>
        /// <param name="weights_k"></param>
        public static unsafe void Integrate3DInt16(IntPtr data, int j_data_len, int k_data_len, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int k_data_offset, int[] indeces_i, int[] indeces_j, int[] indeces_k, double[] weights_i, double[] weights_j, double[] weights_k)
        {
            short* dataPtr = (short*)data;
            double weigth, weights_ij;
            short v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int k_len = indeces_k.Length;
            int i_row_offset, i_j_offset, i_value, j_value, k_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * k_data_len * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    i_j_offset = (j_value * k_data_len) + i_row_offset;
                    weights_ij = weights_i[i] * weights_j[j];
                    for (int k = 0; k < k_len; k++)
                    {
                        k_value = indeces_k[k] - k_data_offset;
                        weigth = weights_ij * weights_k[k];
                        v = dataPtr[i_j_offset + k_value];

                        integral += weigth * v;
                        sumOfTheWeights += weigth;
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>
        /// <param name="k_data_len">third dimesion length of the prefetched array</param>
        /// <param name="missingValue"></param>
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>        
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>
        /// <param name="k_data_offset">an offset of the prefetched array in the raw dataset along the hird dimension</param>
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_k">an indeces of third dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>
        /// <param name="weights_k"></param>
        public static unsafe void Integrate3DUInt16MVsCheck(IntPtr data, int j_data_len, int k_data_len, UInt16 missingValue, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int k_data_offset, int[] indeces_i, int[] indeces_j, int[] indeces_k, double[] weights_i, double[] weights_j, double[] weights_k)
        {
            UInt16* dataPtr = (UInt16*)data;
            double weigth, weights_ij;
            UInt16 v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int k_len = indeces_k.Length;
            int i_row_offset, i_j_offset, i_value, j_value, k_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * k_data_len * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    i_j_offset = (j_value * k_data_len) + i_row_offset;
                    weights_ij = weights_i[i] * weights_j[j];
                    for (int k = 0; k < k_len; k++)
                    {
                        k_value = indeces_k[k] - k_data_offset;
                        weigth = weights_ij * weights_k[k];
                        v = dataPtr[i_j_offset + k_value];
                        if (v != missingValue)
                        {
                            integral += weigth * v;
                            sumOfTheWeights += weigth;

                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>
        /// <param name="k_data_len">third dimesion length of the prefetched array</param>        
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>
        /// <param name="k_data_offset">an offset of the prefetched array in the raw dataset along the hird dimension</param>
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_k">an indeces of third dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>
        /// <param name="weights_k"></param>
        public static unsafe void Integrate3DUInt16(IntPtr data, int j_data_len, int k_data_len, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int k_data_offset, int[] indeces_i, int[] indeces_j, int[] indeces_k, double[] weights_i, double[] weights_j, double[] weights_k)
        {
            UInt16* dataPtr = (UInt16*)data;
            double weigth, weights_ij;
            UInt16 v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int k_len = indeces_k.Length;
            int i_row_offset, i_j_offset, i_value, j_value, k_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * k_data_len * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    i_j_offset = (j_value * k_data_len) + i_row_offset;
                    weights_ij = weights_i[i] * weights_j[j];
                    for (int k = 0; k < k_len; k++)
                    {
                        k_value = indeces_k[k] - k_data_offset;
                        weigth = weights_ij * weights_k[k];
                        v = dataPtr[i_j_offset + k_value];

                        integral += weigth * v;
                        sumOfTheWeights += weigth;
                    }
                }
            }
        }        

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>
        /// <param name="k_data_len">third dimesion length of the prefetched array</param>
        /// <param name="missingValue"></param>
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>        
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>
        /// <param name="k_data_offset">an offset of the prefetched array in the raw dataset along the hird dimension</param>
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_k">an indeces of third dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>
        /// <param name="weights_k"></param>
        public static unsafe void Integrate3DInt8MVsCheck(IntPtr data, int j_data_len, int k_data_len, sbyte missingValue, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int k_data_offset, int[] indeces_i, int[] indeces_j, int[] indeces_k, double[] weights_i, double[] weights_j, double[] weights_k)
        {
            sbyte* dataPtr = (sbyte*)data;
            double weigth, weights_ij;
            sbyte v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int k_len = indeces_k.Length;
            int i_row_offset, i_j_offset, i_value, j_value, k_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * k_data_len * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    i_j_offset = (j_value * k_data_len) + i_row_offset;
                    weights_ij = weights_i[i] * weights_j[j];
                    for (int k = 0; k < k_len; k++)
                    {
                        k_value = indeces_k[k] - k_data_offset;
                        weigth = weights_ij * weights_k[k];
                        v = dataPtr[i_j_offset + k_value];
                        if (v != missingValue)
                        {
                            integral += weigth * v;
                            sumOfTheWeights += weigth;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>
        /// <param name="k_data_len">third dimesion length of the prefetched array</param>        
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>
        /// <param name="k_data_offset">an offset of the prefetched array in the raw dataset along the hird dimension</param>
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_k">an indeces of third dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>
        /// <param name="weights_k"></param>
        public static unsafe void Integrate3DInt8(IntPtr data, int j_data_len, int k_data_len, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int k_data_offset, int[] indeces_i, int[] indeces_j, int[] indeces_k, double[] weights_i, double[] weights_j, double[] weights_k)
        {
            sbyte* dataPtr = (sbyte*)data;
            double weigth, weights_ij;
            sbyte v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int k_len = indeces_k.Length;
            int i_row_offset, i_j_offset, i_value, j_value, k_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * k_data_len * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    i_j_offset = (j_value * k_data_len) + i_row_offset;
                    weights_ij = weights_i[i] * weights_j[j];
                    for (int k = 0; k < k_len; k++)
                    {
                        k_value = indeces_k[k] - k_data_offset;
                        weigth = weights_ij * weights_k[k];
                        v = dataPtr[i_j_offset + k_value];

                        integral += weigth * v;
                        sumOfTheWeights += weigth;
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>
        /// <param name="k_data_len">third dimesion length of the prefetched array</param>
        /// <param name="missingValue"></param>
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>        
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>
        /// <param name="k_data_offset">an offset of the prefetched array in the raw dataset along the hird dimension</param>
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_k">an indeces of third dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>
        /// <param name="weights_k"></param>
        public static unsafe void Integrate3DUInt8MVsCheck(IntPtr data, int j_data_len, int k_data_len, byte missingValue, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int k_data_offset, int[] indeces_i, int[] indeces_j, int[] indeces_k, double[] weights_i, double[] weights_j, double[] weights_k)
        {
            byte* dataPtr = (byte*)data;
            double weigth, weights_ij;
            byte v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int k_len = indeces_k.Length;
            int i_row_offset, i_j_offset, i_value, j_value, k_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * k_data_len * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    i_j_offset = (j_value * k_data_len) + i_row_offset;
                    weights_ij = weights_i[i] * weights_j[j];
                    for (int k = 0; k < k_len; k++)
                    {
                        k_value = indeces_k[k] - k_data_offset;
                        weigth = weights_ij * weights_k[k];
                        v = dataPtr[i_j_offset + k_value];
                        if (v != missingValue)
                        {
                            integral += weigth * v;
                            sumOfTheWeights += weigth;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>
        /// <param name="k_data_len">third dimesion length of the prefetched array</param>        
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>
        /// <param name="k_data_offset">an offset of the prefetched array in the raw dataset along the hird dimension</param>
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_k">an indeces of third dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>
        /// <param name="weights_k"></param>
        public static unsafe void Integrate3DUInt8(IntPtr data, int j_data_len, int k_data_len, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int k_data_offset, int[] indeces_i, int[] indeces_j, int[] indeces_k, double[] weights_i, double[] weights_j, double[] weights_k)
        {
            byte* dataPtr = (byte*)data;
            double weigth, weights_ij;
            byte v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int k_len = indeces_k.Length;
            int i_row_offset, i_j_offset, i_value, j_value, k_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * k_data_len * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    i_j_offset = (j_value * k_data_len) + i_row_offset;
                    weights_ij = weights_i[i] * weights_j[j];
                    for (int k = 0; k < k_len; k++)
                    {
                        k_value = indeces_k[k] - k_data_offset;
                        weigth = weights_ij * weights_k[k];
                        v = dataPtr[i_j_offset + k_value];

                        integral += weigth * v;
                        sumOfTheWeights += weigth;
                    }
                }
            }
        }        


        #endregion

        #region 2D domains

        /// <summary>
        /// Sums the data with corresponing weigts with skipping of mising values
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>        
        /// <param name="missingValue"></param>
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the weights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>        
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>        
        /// <param name="indeces_i">an indices of first dimension to sum (zero based raw dataset indices)</param>
        /// <param name="indeces_j">an indices of second dimension to sum (zero based raw dataset indices)</param>        
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>        
        public static unsafe void Integrate2DDoubleMVsCheck(IntPtr data, int j_data_len, double missingValue, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int[] indeces_i, int[] indeces_j, double[] weights_i, double[] weights_j)
        {
            double* dataPtr = (double*)data;
            double weigth;
            double v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int i_row_offset, i_value, j_value;

            bool mvNan = double.IsNaN(missingValue);
            bool addingNeeded;

            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    v = dataPtr[j_value + i_row_offset];
                    addingNeeded = (mvNan)?(!double.IsNaN(v)):(v != missingValue);
                    if (addingNeeded)
                    {
                        weigth = weights_i[i] * weights_j[j];
                        integral += weigth * v;
                        sumOfTheWeights += weigth;
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>        
        /// <param name="missingValue"></param>
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the weights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>        
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>        
        /// <param name="indeces_i">an indices of first dimension to sum (zero based raw dataset indices)</param>
        /// <param name="indeces_j">an indices of second dimension to sum (zero based raw dataset indices)</param>        
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>        
        public static unsafe void Integrate2DFloatMVsCheck(IntPtr data, int j_data_len, float missingValue, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int[] indeces_i, int[] indeces_j, double[] weights_i, double[] weights_j)
        {
            float* dataPtr = (float*)data;
            double weigth;
            float v;            
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int i_row_offset, i_value, j_value;
            bool mvNan = float.IsNaN(missingValue);
            bool addingNeeded;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    v = dataPtr[j_value + i_row_offset];
                    addingNeeded = (mvNan) ? (!float.IsNaN(v)) : (v != missingValue);
                    if (addingNeeded)
                    {
                        weigth = weights_i[i] * weights_j[j];
                        integral += weigth * v;
                        sumOfTheWeights += weigth;
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>        
        /// <param name="missingValue"></param>
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>        
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>        
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>        
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>        
        public static unsafe void Integrate2DInt64MVsCheck(IntPtr data, int j_data_len, Int64 missingValue, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int[] indeces_i, int[] indeces_j, double[] weights_i, double[] weights_j)
        {
            Int64* dataPtr = (Int64*)data;
            double weigth;
            Int64 v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int i_row_offset, i_value, j_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    v = dataPtr[j_value + i_row_offset];
                    if (v != missingValue)
                    {
                        weigth = weights_i[i] * weights_j[j];
                        integral += weigth * v;
                        sumOfTheWeights += weigth;
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>        
        /// <param name="missingValue"></param>
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>        
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>        
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>        
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>        
        public static unsafe void Integrate2DInt32MVsCheck(IntPtr data, int j_data_len, int missingValue, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int[] indeces_i, int[] indeces_j, double[] weights_i, double[] weights_j)
        {
            int* dataPtr = (int*)data;
            double weigth;
            int v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int i_row_offset, i_value, j_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    v = dataPtr[j_value + i_row_offset];
                    if (v != missingValue)
                    {
                        weigth = weights_i[i] * weights_j[j];
                        integral += weigth * v;
                        sumOfTheWeights += weigth;
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>        
        /// <param name="missingValue"></param>
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>        
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>        
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>        
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>        
        public static unsafe void Integrate2DInt16MVsCheck(IntPtr data, int j_data_len, short missingValue, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int[] indeces_i, int[] indeces_j, double[] weights_i, double[] weights_j)
        {
            short* dataPtr = (short*)data;
            double weigth;
            int v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int i_row_offset, i_value, j_value;

            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    v = dataPtr[j_value + i_row_offset];
                    if (v != missingValue)
                    {
                        weigth = weights_i[i] * weights_j[j];
                        integral += weigth * v;
                        sumOfTheWeights += weigth;
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>        
        /// <param name="missingValue"></param>
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>        
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>        
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>        
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>        
        public static unsafe void Integrate2DInt8MVsCheck(IntPtr data, int j_data_len, sbyte missingValue, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int[] indeces_i, int[] indeces_j, double[] weights_i, double[] weights_j)
        {
            sbyte* dataPtr = (sbyte*)data;
            double weigth;
            sbyte v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int i_row_offset, i_value, j_value;

            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    v = dataPtr[j_value + i_row_offset];
                    if (v != missingValue)
                    {
                        weigth = weights_i[i] * weights_j[j];
                        integral += weigth * v;
                        sumOfTheWeights += weigth;
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>        
        /// <param name="missingValue"></param>
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>        
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>        
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>        
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>        
        public static unsafe void Integrate2DUInt64MVsCheck(IntPtr data, int j_data_len, UInt64 missingValue, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int[] indeces_i, int[] indeces_j, double[] weights_i, double[] weights_j)
        {
            UInt64* dataPtr = (UInt64*)data;
            double weigth;
            UInt64 v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int i_row_offset, i_value, j_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    v = dataPtr[j_value + i_row_offset];
                    if (v != missingValue)
                    {
                        weigth = weights_i[i] * weights_j[j];
                        integral += weigth * v;
                        sumOfTheWeights += weigth;
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>        
        /// <param name="missingValue"></param>
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>        
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>        
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>        
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>        
        public static unsafe void Integrate2DUInt32MVsCheck(IntPtr data, int j_data_len, UInt32 missingValue, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int[] indeces_i, int[] indeces_j, double[] weights_i, double[] weights_j)
        {
            UInt32* dataPtr = (UInt32*)data;
            double weigth;
            UInt32 v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int i_row_offset, i_value, j_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    v = dataPtr[j_value + i_row_offset];
                    if (v != missingValue)
                    {
                        weigth = weights_i[i] * weights_j[j];
                        integral += weigth * v;
                        sumOfTheWeights += weigth;
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>        
        /// <param name="missingValue"></param>
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>        
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>        
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>        
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>        
        public static unsafe void Integrate2DUInt16MVsCheck(IntPtr data, int j_data_len, UInt16 missingValue, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int[] indeces_i, int[] indeces_j, double[] weights_i, double[] weights_j)
        {
            UInt16* dataPtr = (UInt16*)data;
            double weigth;
            UInt16 v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int i_row_offset, i_value, j_value;

            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    v = dataPtr[j_value + i_row_offset];
                    if (v != missingValue)
                    {
                        weigth = weights_i[i] * weights_j[j];
                        integral += weigth * v;
                        sumOfTheWeights += weigth;
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>        
        /// <param name="missingValue"></param>
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>        
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>        
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>        
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>        
        public static unsafe void Integrate2DUInt8MVsCheck(IntPtr data, int j_data_len, byte missingValue, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int[] indeces_i, int[] indeces_j, double[] weights_i, double[] weights_j)
        {
            byte* dataPtr = (byte*)data;
            double weigth;
            byte v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int i_row_offset, i_value, j_value;

            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    v = dataPtr[j_value + i_row_offset];
                    if (v != missingValue)
                    {
                        weigth = weights_i[i] * weights_j[j];
                        integral += weigth * v;
                        sumOfTheWeights += weigth;
                    }
                }
            }
        }
        
        /// <summary>
        /// Sums the data with corresponding weights. If MV present, than integral value is Nan
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>        
        /// <param name="missingValue"></param>
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the weights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>        
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>        
        /// <param name="indeces_i">an indices of first dimension to sum (zero based raw dataset indices)</param>
        /// <param name="indeces_j">an indices of second dimension to sum (zero based raw dataset indices)</param>        
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>        
        public static unsafe void Integrate2DDoubleNanOnMv(IntPtr data, int j_data_len, double missingValue, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int[] indeces_i, int[] indeces_j, double[] weights_i, double[] weights_j)
        {
            double* dataPtr = (double*)data;
            double weigth;
            double v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int i_row_offset, i_value, j_value;
            bool stop = false;
            for (int i = 0; i < i_len && !stop; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * j_data_len;
                for (int j = 0; j < j_len && !stop; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    v = dataPtr[j_value + i_row_offset];
                    if (v == missingValue)
                    {
                        stop = true;
                        integral = weigth = double.NaN;
                    }
                    else
                    {
                        weigth = weights_i[i] * weights_j[j];
                        integral += weigth * v;
                        sumOfTheWeights += weigth;
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponding weights. If MV present, than integral value is Nan
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>        
        /// <param name="missingValue"></param>
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the weights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>        
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>        
        /// <param name="indeces_i">an indices of first dimension to sum (zero based raw dataset indices)</param>
        /// <param name="indeces_j">an indices of second dimension to sum (zero based raw dataset indices)</param>        
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>        
        public static unsafe void Integrate2DInt64NanOnMv(IntPtr data, int j_data_len, Int64 missingValue, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int[] indeces_i, int[] indeces_j, double[] weights_i, double[] weights_j)
        {
            Int64* dataPtr = (Int64*)data;
            double weigth;
            Int64 v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int i_row_offset, i_value, j_value;
            bool stop = false;
            for (int i = 0; i < i_len && !stop; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * j_data_len;
                for (int j = 0; j < j_len && !stop; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    v = dataPtr[j_value + i_row_offset];
                    if (v == missingValue)
                    {
                        stop = true;
                        integral = weigth = double.NaN;
                    }
                    else
                    {
                        weigth = weights_i[i] * weights_j[j];
                        integral += weigth * v;
                        sumOfTheWeights += weigth;
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponding weights. If MV present, than integral value is Nan
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>        
        /// <param name="missingValue"></param>
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the weights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>        
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>        
        /// <param name="indeces_i">an indices of first dimension to sum (zero based raw dataset indices)</param>
        /// <param name="indeces_j">an indices of second dimension to sum (zero based raw dataset indices)</param>        
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>        
        public static unsafe void Integrate2DInt32NanOnMv(IntPtr data, int j_data_len, int missingValue, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int[] indeces_i, int[] indeces_j, double[] weights_i, double[] weights_j)
        {
            int* dataPtr = (int*)data;
            double weigth;
            int v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int i_row_offset, i_value, j_value;
            bool stop = false;
            for (int i = 0; i < i_len && !stop; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * j_data_len;
                for (int j = 0; j < j_len && !stop; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    v = dataPtr[j_value + i_row_offset];
                    if (v == missingValue)
                    {
                        stop = true;
                        integral = weigth = double.NaN;
                    }
                    else
                    {
                        weigth = weights_i[i] * weights_j[j];
                        integral += weigth * v;
                        sumOfTheWeights += weigth;
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponding weights. If MV present, than integral value is Nan
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>        
        /// <param name="missingValue"></param>
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the weights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>        
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>        
        /// <param name="indeces_i">an indices of first dimension to sum (zero based raw dataset indices)</param>
        /// <param name="indeces_j">an indices of second dimension to sum (zero based raw dataset indices)</param>        
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>        
        public static unsafe void Integrate2DInt16NanOnMv(IntPtr data, int j_data_len, Int16 missingValue, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int[] indeces_i, int[] indeces_j, double[] weights_i, double[] weights_j)
        {
            Int16* dataPtr = (Int16*)data;
            double weigth;
            Int16 v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int i_row_offset, i_value, j_value;
            bool stop = false;
            for (int i = 0; i < i_len && !stop; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * j_data_len;
                for (int j = 0; j < j_len && !stop; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    v = dataPtr[j_value + i_row_offset];
                    if (v == missingValue)
                    {
                        stop = true;
                        integral = weigth = double.NaN;
                    }
                    else
                    {
                        weigth = weights_i[i] * weights_j[j];
                        integral += weigth * v;
                        sumOfTheWeights += weigth;
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponding weights. If MV present, than integral value is Nan
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>        
        /// <param name="missingValue"></param>
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the weights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>        
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>        
        /// <param name="indeces_i">an indices of first dimension to sum (zero based raw dataset indices)</param>
        /// <param name="indeces_j">an indices of second dimension to sum (zero based raw dataset indices)</param>        
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>        
        public static unsafe void Integrate2DInt8NanOnMv(IntPtr data, int j_data_len, sbyte missingValue, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int[] indeces_i, int[] indeces_j, double[] weights_i, double[] weights_j)
        {
            sbyte* dataPtr = (sbyte*)data;
            double weigth;
            sbyte v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int i_row_offset, i_value, j_value;
            bool stop = false;
            for (int i = 0; i < i_len && !stop; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * j_data_len;
                for (int j = 0; j < j_len && !stop; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    v = dataPtr[j_value + i_row_offset];
                    if (v == missingValue)
                    {
                        stop = true;
                        integral = weigth = double.NaN;
                    }
                    else
                    {
                        weigth = weights_i[i] * weights_j[j];
                        integral += weigth * v;
                        sumOfTheWeights += weigth;
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponding weights. If MV present, than integral value is Nan
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>        
        /// <param name="missingValue"></param>
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the weights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>        
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>        
        /// <param name="indeces_i">an indices of first dimension to sum (zero based raw dataset indices)</param>
        /// <param name="indeces_j">an indices of second dimension to sum (zero based raw dataset indices)</param>        
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>        
        public static unsafe void Integrate2DUInt64NanOnMv(IntPtr data, int j_data_len, UInt64 missingValue, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int[] indeces_i, int[] indeces_j, double[] weights_i, double[] weights_j)
        {
            UInt64* dataPtr = (UInt64*)data;
            double weigth;
            UInt64 v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int i_row_offset, i_value, j_value;
            bool stop = false;
            for (int i = 0; i < i_len && !stop; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * j_data_len;
                for (int j = 0; j < j_len && !stop; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    v = dataPtr[j_value + i_row_offset];
                    if (v == missingValue)
                    {
                        stop = true;
                        integral = weigth = double.NaN;
                    }
                    else
                    {
                        weigth = weights_i[i] * weights_j[j];
                        integral += weigth * v;
                        sumOfTheWeights += weigth;
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponding weights. If MV present, than integral value is Nan
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>        
        /// <param name="missingValue"></param>
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the weights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>        
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>        
        /// <param name="indeces_i">an indices of first dimension to sum (zero based raw dataset indices)</param>
        /// <param name="indeces_j">an indices of second dimension to sum (zero based raw dataset indices)</param>        
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>        
        public static unsafe void Integrate2DUInt32NanOnMv(IntPtr data, int j_data_len, UInt32 missingValue, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int[] indeces_i, int[] indeces_j, double[] weights_i, double[] weights_j)
        {
            UInt32* dataPtr = (UInt32*)data;
            double weigth;
            UInt32 v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int i_row_offset, i_value, j_value;
            bool stop = false;
            for (int i = 0; i < i_len && !stop; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * j_data_len;
                for (int j = 0; j < j_len && !stop; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    v = dataPtr[j_value + i_row_offset];
                    if (v == missingValue)
                    {
                        stop = true;
                        integral = weigth = double.NaN;
                    }
                    else
                    {
                        weigth = weights_i[i] * weights_j[j];
                        integral += weigth * v;
                        sumOfTheWeights += weigth;
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponding weights. If MV present, than integral value is Nan
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>        
        /// <param name="missingValue"></param>
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the weights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>        
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>        
        /// <param name="indeces_i">an indices of first dimension to sum (zero based raw dataset indices)</param>
        /// <param name="indeces_j">an indices of second dimension to sum (zero based raw dataset indices)</param>        
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>        
        public static unsafe void Integrate2DUInt16NanOnMv(IntPtr data, int j_data_len, UInt16 missingValue, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int[] indeces_i, int[] indeces_j, double[] weights_i, double[] weights_j)
        {
            UInt16* dataPtr = (UInt16*)data;
            double weigth;
            UInt16 v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int i_row_offset, i_value, j_value;
            bool stop = false;
            for (int i = 0; i < i_len && !stop; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * j_data_len;
                for (int j = 0; j < j_len && !stop; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    v = dataPtr[j_value + i_row_offset];
                    if (v == missingValue)
                    {
                        stop = true;
                        integral = weigth = double.NaN;
                    }
                    else
                    {
                        weigth = weights_i[i] * weights_j[j];
                        integral += weigth * v;
                        sumOfTheWeights += weigth;
                    }
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponding weights. If MV present, than integral value is Nan
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>        
        /// <param name="missingValue"></param>
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the weights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>        
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>        
        /// <param name="indeces_i">an indices of first dimension to sum (zero based raw dataset indices)</param>
        /// <param name="indeces_j">an indices of second dimension to sum (zero based raw dataset indices)</param>        
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>        
        public static unsafe void Integrate2DUInt8NanOnMv(IntPtr data, int j_data_len, byte missingValue, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int[] indeces_i, int[] indeces_j, double[] weights_i, double[] weights_j)
        {
            byte* dataPtr = (byte*)data;
            double weigth;
            byte v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int i_row_offset, i_value, j_value;
            bool stop = false;
            for (int i = 0; i < i_len && !stop; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * j_data_len;
                for (int j = 0; j < j_len && !stop; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    v = dataPtr[j_value + i_row_offset];
                    if (v == missingValue)
                    {
                        stop = true;
                        integral = weigth = double.NaN;
                    }
                    else
                    {
                        weigth = weights_i[i] * weights_j[j];
                        integral += weigth * v;
                        sumOfTheWeights += weigth;
                    }
                }
            }
        }


        /// <summary>
        /// Sums the data with corresponding weights
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>        
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the weights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>
        /// <param name="indeces_i">an indices of first dimension to sum (zero based raw dataset indices)</param>
        /// <param name="indeces_j">an indices of second dimension to sum (zero based raw dataset indices)</param>        
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>        
        public static unsafe void Integrate2DDouble(IntPtr data, int j_data_len, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int[] indeces_i, int[] indeces_j, double[] weights_i, double[] weights_j)
        {
            double* dataPtr = (double*)data;
            double weigth;
            double v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int i_row_offset, i_value, j_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    v = dataPtr[j_value + i_row_offset];

                    weigth = weights_i[i] * weights_j[j];
                    integral += weigth * v;
                    sumOfTheWeights += weigth;
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponding weights
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>        
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the weights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>
        /// <param name="indeces_i">an indices of first dimension to sum (zero based raw dataset indices)</param>
        /// <param name="indeces_j">an indices of second dimension to sum (zero based raw dataset indices)</param>        
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>        
        public static unsafe void Integrate2DFloat(IntPtr data, int j_data_len, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int[] indeces_i, int[] indeces_j, double[] weights_i, double[] weights_j)
        {
            float* dataPtr = (float*)data;
            double weigth;
            float v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int i_row_offset, i_value, j_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    v = dataPtr[j_value + i_row_offset];

                    weigth = weights_i[i] * weights_j[j];
                    integral += weigth * v;
                    sumOfTheWeights += weigth;
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>        
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>        
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>        
        public static unsafe void Integrate2DInt64(IntPtr data, int j_data_len, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int[] indeces_i, int[] indeces_j, double[] weights_i, double[] weights_j)
        {
            Int64* dataPtr = (Int64*)data;
            double weigth;
            Int64 v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int i_row_offset, i_value, j_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    v = dataPtr[j_value + i_row_offset];

                    weigth = weights_i[i] * weights_j[j];
                    integral += weigth * v;
                    sumOfTheWeights += weigth;
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>        
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>        
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>        
        public static unsafe void Integrate2DInt32(IntPtr data, int j_data_len, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int[] indeces_i, int[] indeces_j, double[] weights_i, double[] weights_j)
        {
            int* dataPtr = (int*)data;
            double weigth;
            int v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int i_row_offset, i_value, j_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    v = dataPtr[j_value + i_row_offset];

                    weigth = weights_i[i] * weights_j[j];
                    integral += weigth * v;
                    sumOfTheWeights += weigth;
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponding weights
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>        
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the weights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>        
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>        
        public static unsafe void Integrate2DInt16(IntPtr data, int j_data_len, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int[] indeces_i, int[] indeces_j, double[] weights_i, double[] weights_j)
        {
            short* dataPtr = (short*)data;
            double weigth;
            short v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int i_row_offset, i_value, j_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    v = dataPtr[j_value + i_row_offset];

                    weigth = weights_i[i] * weights_j[j];
                    integral += weigth * v;
                    sumOfTheWeights += weigth;
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponding weights
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>        
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the weights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>        
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>        
        public static unsafe void Integrate2DInt8(IntPtr data, int j_data_len, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int[] indeces_i, int[] indeces_j, double[] weights_i, double[] weights_j)
        {
            sbyte* dataPtr = (sbyte*)data;
            double weigth;
            sbyte v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int i_row_offset, i_value, j_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    v = dataPtr[j_value + i_row_offset];

                    weigth = weights_i[i] * weights_j[j];
                    integral += weigth * v;
                    sumOfTheWeights += weigth;
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>        
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>        
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>        
        public static unsafe void Integrate2DUInt64(IntPtr data, int j_data_len, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int[] indeces_i, int[] indeces_j, double[] weights_i, double[] weights_j)
        {
            UInt64* dataPtr = (UInt64*)data;
            double weigth;
            UInt64 v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int i_row_offset, i_value, j_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    v = dataPtr[j_value + i_row_offset];

                    weigth = weights_i[i] * weights_j[j];
                    integral += weigth * v;
                    sumOfTheWeights += weigth;
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponing weigts
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>        
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the wights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>        
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>        
        public static unsafe void Integrate2DUInt32(IntPtr data, int j_data_len, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int[] indeces_i, int[] indeces_j, double[] weights_i, double[] weights_j)
        {
            UInt32* dataPtr = (UInt32*)data;
            double weigth;
            UInt32 v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int i_row_offset, i_value, j_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    v = dataPtr[j_value + i_row_offset];

                    weigth = weights_i[i] * weights_j[j];
                    integral += weigth * v;
                    sumOfTheWeights += weigth;
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponding weights
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>        
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the weights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>        
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>        
        public static unsafe void Integrate2DUInt16(IntPtr data, int j_data_len, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int[] indeces_i, int[] indeces_j, double[] weights_i, double[] weights_j)
        {
            UInt16* dataPtr = (UInt16*)data;
            double weigth;
            UInt16 v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int i_row_offset, i_value, j_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    v = dataPtr[j_value + i_row_offset];

                    weigth = weights_i[i] * weights_j[j];
                    integral += weigth * v;
                    sumOfTheWeights += weigth;
                }
            }
        }

        /// <summary>
        /// Sums the data with corresponding weights
        /// </summary>
        /// <param name="data">A pointer to the prefetched array</param>
        /// <param name="j_data_len">second dimension length of the prefetched array</param>        
        /// <param name="sumOfTheWeights">out parameter that holds sum of all the weights</param>
        /// <param name="integral">out parameter that holds the sum of non MV data multiplied by weights</param>
        /// <param name="i_data_offset">an offset of the prefetched array in the raw dataset along the first dimension</param>
        /// <param name="j_data_offset">an offset of the prefetched array in the raw dataset along the second dimension</param>
        /// <param name="indeces_i">an indeces of first dimension to sum (zero based raw dataset indeces)</param>
        /// <param name="indeces_j">an indeces of second dimension to sum (zero based raw dataset indeces)</param>        
        /// <param name="weights_i"></param>
        /// <param name="weights_j"></param>        
        public static unsafe void Integrate2DUInt8(IntPtr data, int j_data_len, ref double sumOfTheWeights, ref double integral, int i_data_offset, int j_data_offset, int[] indeces_i, int[] indeces_j, double[] weights_i, double[] weights_j)
        {
            byte* dataPtr = (byte*)data;
            double weigth;
            byte v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int i_row_offset, i_value, j_value;
            for (int i = 0; i < i_len; i++)
            {
                i_value = indeces_i[i] - i_data_offset;
                i_row_offset = i_value * j_data_len;
                for (int j = 0; j < j_len; j++)
                {
                    j_value = indeces_j[j] - j_data_offset;
                    v = dataPtr[j_value + i_row_offset];

                    weigth = weights_i[i] * weights_j[j];
                    integral += weigth * v;
                    sumOfTheWeights += weigth;
                }
            }
        }
        
        #endregion

    }
}
