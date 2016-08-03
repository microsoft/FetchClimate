using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators
{
    public interface IBitMaskProvider
    {
        bool HasData(double lat, double lon);

        /// <summary>
        /// computes the percentage of the data points without missing values in the given rectangle
        /// </summary>
        /// <param name="latmin">Min latitude</param>
        /// <param name="latmax">Max latitude</param>
        /// <param name="lonmin">Min longitude</param>
        /// <param name="lonmax">Max longitude</param>
        /// <returns>value from 0.0 to 1.0</returns>
        double GetDataPercentage(double latmin, double latmax, double lonmin, double lonmax);
    }

    /// <summary>
    /// Uses IBitMaskProvider to follow the IFetchEngine uncertainty conventions (e.g. uncertainty NaN for absence of data, double.MaxMalue for absence of Uncertainty info)
    /// </summary>
    public class GridBitmaskDecorator : IBatchUncertaintyEvaluator
    {
        private readonly IBitMaskProvider maskProvider;
        private readonly IBatchUncertaintyEvaluator component;

        public GridBitmaskDecorator(IBatchUncertaintyEvaluator component, IBitMaskProvider bitmaskProvider)
        {
            this.maskProvider = bitmaskProvider;
            this.component = component;
        }

        public async Task<double[]> EvaluateCellsBatchAsync(IEnumerable<ICellRequest> cells)
        {
            ICellRequest[] cellArray = cells.ToArray();

            int N = cellArray.Length;

            if (N == 0)
                return new double[0];
            else
            {

                bool[] passToComponentFlags = new bool[N];
                bool[] fullCoverageFlags = new bool[N];
                double[] result = new double[N];

                List<ICellRequest> passToComponentCells = new List<ICellRequest>(N);

                //Result matrix
                //Integrators info\non-MV concentration	0.0	(0.0;1.0)	1.0
                //With unc	ND	NU	U
                //Without unc	ND	NU	NU
                //Out of range (no mean values)	ND	ND	ND

                for (int i = 0; i < N; i++)
                {
                    ICellRequest cell = cellArray[i];
                    double landP = maskProvider.GetDataPercentage(cell.LatMin, cell.LatMax, cell.LonMin, cell.LonMax);

                    bool passToComponentFlag = true;
                    bool fullCoverageFlag = false;

                    if (landP == 0.0)
                    {
                        passToComponentFlag = false;
                        result[i] = double.NaN;
                    }
                    else if (landP == 1.0)
                    {
                        fullCoverageFlag = true;
                    }

                    passToComponentFlags[i] = passToComponentFlag;
                    fullCoverageFlags[i] = fullCoverageFlag;
                    if (passToComponentFlag)
                        passToComponentCells.Add(cell);
                }

                double[] componentResults = await component.EvaluateCellsBatchAsync(passToComponentCells);

                int pointer = 0;
                for (int i = 0; i < N; i++)
                    if (passToComponentFlags[i])
                    {
                        if (!double.IsNaN(componentResults[pointer]) && !fullCoverageFlags[i])
                            result[i] = double.MaxValue;
                        else
                            result[i] = componentResults[pointer];
                        pointer++;
                    }

                return result;
            }
        }
    }
}
