using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Utils
{
    /// <summary>
    /// Contains a static method with low-level optimized mode calculation functions
    /// </summary>
    public static class ArrayMode
    {
        public static IEnumerable<double> FindModeSequence3D(string variable, Array prefetchedData, int[] prefetchedDataOrigin, IEnumerable<int[][]> idxArrays, object missingValue = null)
        {
            Type dataType = prefetchedData.GetType().GetElementType();
            int shape1 = prefetchedData.GetLength(1);
            int shape2 = prefetchedData.GetLength(2);

            GCHandle? capturedHandle;
            capturedHandle = GCHandle.Alloc(prefetchedData, GCHandleType.Pinned);
            IntPtr prefetchedDataPtr = capturedHandle.Value.AddrOfPinnedObject();
            try
            {
                if (dataType == typeof(Byte))
                {
                    foreach (int[][] idc in idxArrays)
                    {
                        if (idc == null)// out of data cell
                        {
                            yield return double.NaN;
                            continue;
                        }

                        yield return FindMode3DUInt8(prefetchedDataPtr, shape1, shape2,
                            prefetchedDataOrigin[0], prefetchedDataOrigin[1], prefetchedDataOrigin[2],
                            idc[0], idc[1], idc[2],
                            missingValue);
                    }
                }
                else
                    throw new NotSupportedException(string.Format("type {0} is not supported yet by mode calulation code", dataType.ToString()));
            }
            finally
            {
                capturedHandle.Value.Free();
            }
        }       

        public static unsafe double FindMode3DUInt8(IntPtr data, int j_data_len, int k_data_len, int i_data_offset, int j_data_offset, int k_data_offset, int[] indeces_i, int[] indeces_j, int[] indeces_k, object missingValue = null)
        {
            int[] counters = new int[256];
            byte* dataPtr = (byte*)data;            
            byte v;
            int i_len = indeces_i.Length;
            int j_len = indeces_j.Length;
            int k_len = indeces_k.Length;
            int i_row_offset, i_j_offset, i_value, j_value, k_value;
            if (missingValue == null)
                for (int i = 0; i < i_len; i++)
                {
                    i_value = indeces_i[i] - i_data_offset;
                    i_row_offset = i_value * k_data_len * j_data_len;
                    for (int j = 0; j < j_len; j++)
                    {
                        j_value = indeces_j[j] - j_data_offset;
                        i_j_offset = (j_value * k_data_len) + i_row_offset;
                        for (int k = 0; k < k_len; k++)
                        {
                            k_value = indeces_k[k] - k_data_offset;
                            v = dataPtr[i_j_offset + k_value];
                            counters[v]++;
                        }
                    }
                }
            else
            {
                byte mv = (byte)missingValue;
                for (int i = 0; i < i_len; i++)
                {
                    i_value = indeces_i[i] - i_data_offset;
                    i_row_offset = i_value * k_data_len * j_data_len;
                    for (int j = 0; j < j_len; j++)
                    {
                        j_value = indeces_j[j] - j_data_offset;
                        i_j_offset = (j_value * k_data_len) + i_row_offset;
                        for (int k = 0; k < k_len; k++)
                        {
                            k_value = indeces_k[k] - k_data_offset;
                            v = dataPtr[i_j_offset + k_value];
                            if (v != mv)
                                counters[v]++;
                        }
                    }
                }
            }

            int maxCount = 0;
            int maxIndex = -1;
            int count;

            for (int i = 0; i < 256; i++)
            {
                count = counters[i];
                if (maxCount < count)
                {
                    maxCount = count;
                    maxIndex = i;
                }
            }
            if (maxIndex == -1)
                return double.NaN;
            else
                return (double)maxIndex;
        }
    }
}
