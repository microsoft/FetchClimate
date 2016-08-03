using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Utils.ScatteredPointsProcessing
{
    
    public interface IScatteredPointsLinearInterpolatorOnSphere
    {
        /// <summary>
        /// Returns a set of linear weights to use in linear combination for obtaining interpolated value
        /// </summary>
        /// <param name="lat">The latitude of the point to produce the weights for</param>
        /// <param name="lon">The longitude of the point to produce the weights for</param>
        /// <param name="interpolationContext"></param>
        /// <returns></returns>
        LinearWeight[] GetLinearWeigths(GeoCellTuple cell, double[] nodesLatsAxis, double[] nodesLonsAxis);
    }    

    public interface ITimeSeriesAverager<ObservationT> 
    {
        /// <summary>
        /// Returns a time averaged time series (set of points on Earth sphere) that can be used for further spatial interpolation
        /// </summary>
        /// <param name="cell">A region for calculation of which the averaged time series must be returned</param>
        /// <returns></returns>
        IValueNodes<ObservationT> GetAveragedTimeSeries(GeoCellTuple cell);
    }

    public interface INodes
    {
        double[] Lat { get; }
        double[] Lon { get; }
    }

    public interface IValueNodes<ObservationT> : INodes
    {
        ObservationT[] Value { get; }
    }

    public class LinearCombinationProviderFacade<ObservationT>
    {
        private readonly IScatteredPointsLinearInterpolatorOnSphere linearInterpolator;
        private readonly ITimeSeriesAverager<ObservationT> timeSeriesAverager;

        public LinearCombinationProviderFacade(IScatteredPointsLinearInterpolatorOnSphere linearInterpolator, ITimeSeriesAverager<ObservationT> timeSeriesAverager)
        {
            this.linearInterpolator = linearInterpolator;
            this.timeSeriesAverager = timeSeriesAverager;
        }

        /// <summary>
        /// Returns a set of nodes and a set "weights" to apply to the nodes to form a linear combination to get the mean value of the cells
        /// </summary>
        /// <param name="cells">A geo-temporal regions to get mean values for</param>
        /// <param name="nodes">A points to combine in linear combination</param>
        /// <param name="weights">A sequence corresponding to cells sequence. Contains a sequence of weights to multiply the indexed nodes with and then sum up to get the mean value</param>
        public void GetLinearCombination(IEnumerable<GeoCellTuple> cells, out IEnumerable<IValueNodes<ObservationT>> nodes, out IEnumerable<LinearWeight[]> weights)
        {
            List<IValueNodes<ObservationT>> nodesList=  new List<IValueNodes<ObservationT>>();
            List<LinearWeight[]> weightsList = new List<LinearWeight[]>();

            cells.Select<GeoCellTuple,object>(c =>
                {
                    var tsa = timeSeriesAverager.GetAveragedTimeSeries(c);
                    var w = linearInterpolator.GetLinearWeigths(c, tsa.Lat, tsa.Lon);

                    nodesList.Add(tsa);
                    weightsList.Add(w);
                    return 1;
                }).ToArray();

            nodes = nodesList;
            weights = weightsList;
        }
    }
}
