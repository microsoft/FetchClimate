using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Integrators.Spatial
{
    public class LinearCycledLonsAvgProcessing: CycledLonsAvgFacade
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="areFirstAndLastValuesTheSame">Indicates that the first and the last elements of the the axis are the same point (as the axis is cycled along longitude)</param>
        public LinearCycledLonsAvgProcessing(Array axis, bool areFirstAndLastValuesTheSame)
            : base(axis,
            new WeightProviders.LinearInterpolation(),
            new DataCoverageEvaluators.IndividualObsCoverageEvaluator(), areFirstAndLastValuesTheSame)
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="axisArrayName"></param>
        /// <param name="areFirstAndLastValuesTheSame">Indicates that the first and the last elements of the the axis are the same point (as the axis is cycled along longitude)</param>
        /// <returns></returns>
        public static async Task<LinearCycledLonsAvgProcessing> ConstructAsync(IStorageContext context, string axisArrayName, bool areFirstAndLastValuesTheSame)
        {
            return new LinearCycledLonsAvgProcessing(await context.GetDataAsync(axisArrayName),areFirstAndLastValuesTheSame);
        }
    }
}
