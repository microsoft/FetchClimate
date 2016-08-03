using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.TimeAxisAvgProcessing
{
    public abstract class TimeRegionsAxisAvgFacade : TimeAxisAvgFacade        
    {
        public TimeRegionsAxisAvgFacade(Array axis, ITimeAxisProjection timeAxisProjection, IWeightProvider weightsProvider, IDataCoverageEvaluator coverageEvaluator) :
            base(ConvertAxis(axis), timeAxisProjection,weightsProvider, coverageEvaluator)
        { }

        private static Array ConvertAxis(Array axis)
        {
            if (axis.Rank != 2)
                throw new ArgumentException("Supplied array is not 2D");
            if (axis.GetLength(1) != 2)
                throw new ArgumentException("The length of 2nd dimension of the array must be equal to 2");

            int len = axis.GetLength(0);

            object[] a = new object[len + 1];
            for (int i = 0; i < len; i++)
            {
                a[i] = axis.GetValue(i, 0);
                if (i > 0)
                    if (!a[i].Equals(axis.GetValue(i - 1, 1)))
                        throw new ArgumentException("Described intervals are overlapped or disjoint");

            }
            a[len] = axis.GetValue(len - 1, 1);
            return a;
        }
    }
}
