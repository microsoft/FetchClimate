using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators
{
    /// <summary>
    /// Decorator that coerces uncertainty values according to <see cref="Microsoft.Research.Science.FetchClimate2.IRequestContext.GetMaskAsync"/> conventions (e.g. uncertainty NaN for absence of data, double.MaxMalue for absence of uncertainty info).
    /// Get the data presence from IGridCoverageProvider and ITimeCoverageProvider.
    /// </summary>
    public class GridUncertaintyConventionsDecorator : IBatchUncertaintyEvaluator
    {
        private readonly IBatchUncertaintyEvaluator component;
        private readonly IGridCoverageProvider latCoverageProvider;
        private readonly IGridCoverageProvider lonCoverageProvider;
        private readonly ITimeCoverageProvider timeCoverageProvider;

        public GridUncertaintyConventionsDecorator(
            IBatchUncertaintyEvaluator component,
            IGridCoverageProvider latCoverageProvider,
            IGridCoverageProvider lonCoverageProvider,
            ITimeCoverageProvider timeCoverageProvider) {
            this.component = component;
            this.latCoverageProvider = latCoverageProvider;
            this.lonCoverageProvider = lonCoverageProvider;
            this.timeCoverageProvider = timeCoverageProvider;
        }

        public async Task<double[]> EvaluateCellsBatchAsync(IEnumerable<ICellRequest> cells)
        {
            ICellRequest[] cellsArray = cells.ToArray();
            int N = cellsArray.Length;
            bool[] passCellFlag = new bool[N];
            double[] result = new double[N];

            List<ICellRequest> toPassCellsList = new List<ICellRequest>(N);
            for (int i = 0; i < N; i++)
            {
                DataCoverageResult coverage = GetCoverage(cellsArray[i]);

                bool passCurrent = true;
                if (coverage == DataCoverageResult.OutOfData)
                {
                    result[i] = double.NaN;
                    passCurrent = false;
                }
                else if (coverage == DataCoverageResult.DataWithoutUncertainty)
                {
                    result[i] = double.MaxValue;
                    passCurrent = false;
                }
                else
                {
                    toPassCellsList.Add(cellsArray[i]);
                }
                passCellFlag[i] = passCurrent;
            }

            double[] componentResults = await component.EvaluateCellsBatchAsync(toPassCellsList);

            int pointer = 0;
            for (int i = 0; i < N; i++)            
                if (passCellFlag[i])
                    result[i] = componentResults[pointer++];

            return result;
        }

        private DataCoverageResult GetCoverage(ICellRequest cell)
        {
            var timeR = timeCoverageProvider.GetCoverage(cell.Time);
            var latR = latCoverageProvider.GetCoverage(cell.LatMin, cell.LatMax);
            var lonR = lonCoverageProvider.GetCoverage(cell.LonMin, cell.LonMax);
            if (timeR == DataCoverageResult.OutOfData || latR == DataCoverageResult.OutOfData || lonR == DataCoverageResult.OutOfData)
                return DataCoverageResult.OutOfData;
            else if (timeR == DataCoverageResult.DataWithoutUncertainty || latR == DataCoverageResult.DataWithoutUncertainty || lonR == DataCoverageResult.DataWithoutUncertainty)
                return DataCoverageResult.DataWithoutUncertainty;
            else
                return DataCoverageResult.DataWithUncertainty;
        }
    }
}
