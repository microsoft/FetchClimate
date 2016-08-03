using Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators;
using Microsoft.Research.Science.FetchClimate2.ValueAggregators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Integrators.Spatial
{

    /// <summary>
    /// The join of ISpatGridIntegrator, IGridCoverageProvider and ISpatialAxisInfo
    /// </summary>
    public interface IGridAxisAvgProcessing :
        ISpatGridIntegrator, ISpatialAxisInfo, IGridCoverageProvider { }

    public interface IGridAxisModeProcessing :
        ISpatGridDataMaskProvider, ISpatialAxisInfo, IGridCoverageProvider { }    

    public class LinearIntegratorsFactory
    {
        /// <summary>
        /// Automatically selects approppriate linear integrator by axis data
        /// </summary>
        /// <param name="context"></param>
        /// <param name="axisArrayName"></param>
        /// <returns></returns>
        public static async Task<IGridAxisAvgProcessing> SmartConstructAsync(IStorageContext context, string axisArrayName)
        {            
            DoubleEpsComparer epsComparer = new DoubleEpsComparer(1e-5);
            var axis = await context.GetDataAsync(axisArrayName);
            double firstElem = Convert.ToDouble(axis.GetValue(0));
            double secondElem = Convert.ToDouble(axis.GetValue(1));
            double lastElem = Convert.ToDouble(axis.GetValue(axis.Length-1));
            if(epsComparer.Compare(firstElem+360.0,lastElem)==0)
            {
                //longitudes cycled, last and ferst elemets repeated
                return new LinearCycledLonsAvgProcessing(axis,true);
            }
            else if(epsComparer.Compare(firstElem+360.0-(secondElem-firstElem),lastElem)==0)
            {
                //longitudes cycled
                return new LinearCycledLonsAvgProcessing(axis,false);
            }
            else 
                return new LinearGridIntegrator(axis);
        }
    }
}
