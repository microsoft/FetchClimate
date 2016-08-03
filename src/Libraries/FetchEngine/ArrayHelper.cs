using System;

namespace Microsoft.Research.Science.FetchClimate2
{
    public static class ArrayHelper
    {
        public static Array GetConstantArray<T>(int[] dims, T val)
        {
            Array result = Array.CreateInstance(typeof(T), dims);
            int[] idx = new int[dims.Length];
            while (true)
            {
                result.SetValue(val, idx);
                int i = 0;
                while (i < dims.Length)
                    if (++idx[i] >= dims[i])
                        idx[i++] = 0;
                    else
                        break;
                if (i >= dims.Length)
                    break;
            }
            return result;
        }
    }
}