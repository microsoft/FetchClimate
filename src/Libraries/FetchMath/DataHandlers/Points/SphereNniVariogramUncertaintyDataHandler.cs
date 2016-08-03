using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.FSharp.Core;
using System.Diagnostics;
using System.Threading.Tasks.Schedulers;

namespace Microsoft.Research.Science.FetchClimate2.DataHandlers
{
    /// <summary>
    /// Natural neighbor interpolation with variogram based uncertainty estimation
    /// </summary>
    public abstract class SphereNniVariogramUncertaintyDataHandler : NaturalNeigbourInterpolationOnSphereDataHandler
    {
        public SphereNniVariogramUncertaintyDataHandler(IStorageContext context, bool performCheckForMissingValues, ITimeAxisIntegrator timeAxisIntegrator, IScatteredObservationsProvider observationProvider, ISpatPointsLinearInterpolator2D nni, string latArrayName = null, string lonArrayName = null)
            : base(context, performCheckForMissingValues, timeAxisIntegrator, observationProvider, nni, latArrayName, lonArrayName)
        {

        }

        protected override async Task<double[]> EvaluateCellsBatchAsync(IRequestContext context, ComputationalContext computationalContext, IEnumerable<GeoCellTuple> cells)
        {
            await SaveObservationsAndDalanayDiagsForCells(context, computationalContext, cells);

            VariogramModule.IVariogramFitter variogramFitter = new LMDotNetVariogramFitter.Fitter();            

            Dictionary<ITimeSegment,VariogramModule.IVariogram> variograms = new Dictionary<ITimeSegment,VariogramModule.IVariogram>();

            Dictionary<ITimeSegment, IObservationsInformation> observations = (Dictionary<ITimeSegment, IObservationsInformation>)computationalContext["observations"];

            LimitedConcurrencyLevelTaskScheduler lclts = new LimitedConcurrencyLevelTaskScheduler(Environment.ProcessorCount);
            TaskFactory taskFactory = new TaskFactory(lclts);

            var variogramTasks = observations.Select(pair => taskFactory.StartNew(() =>
                {
                    ITimeSegment ts =pair.Key;
                    TraceVerbose(string.Format("Fitting variogram for {0} ({1} observations)",ts,pair.Value.Observations.Length));
                    Stopwatch sw1 = Stopwatch.StartNew();
                    var lats = pair.Value.Observations.Select(o => o.Latitude).ToArray();
                    var lons = pair.Value.Observations.Select(o => o.Longitude).ToArray();
                    var vals = pair.Value.Observations.Select(o => o.Value).ToArray();
                    var pointSet = new EmpVariogramBuilder.PointSet(lats, lons, vals);

                    var dist = FuncConvert.ToFSharpFunc(new Converter<Tuple<double, double>,FSharpFunc<Tuple<double, double>, double>>(t1 =>
                        FuncConvert.ToFSharpFunc(new Converter<Tuple<double, double>, double>(t2 => SphereMath.GetDistance(t1.Item1,t1.Item2,t2.Item1,t2.Item2)))));
                    
                    var empVar = EmpVariogramBuilder.EmpiricalVariogramBuilder.BuildEmpiricalVariogram(pointSet,dist);                   
                    var fitted_variogram = variogramFitter.Fit(empVar);
                    VariogramModule.IVariogram effectiveVariogram = null;
                    sw1.Stop();

                    if (FSharpOption<VariogramModule.IDescribedVariogram>.get_IsSome(fitted_variogram))
                    {
                        effectiveVariogram = fitted_variogram.Value;
                        TraceVerbose(string.Format("Variogram fited for {0} ({1} observations) in {2}", ts, pair.Value.Observations.Length, sw1.Elapsed));
                    }
                    else
                    {
                        TraceWarning(string.Format("Variogram fitting failed for {0} ({1} observations) in {2}. Using fallback variogram", ts, pair.Value.Observations.Length, sw1.Elapsed));
                        effectiveVariogram = variogramFitter.GetFallback(empVar);
                    }
                    lock("saving_variograms")
                    {
                        variograms.Add(ts, effectiveVariogram);
                    }
                }));

            TraceVerbose(string.Format("Starting calculations of linear weights for all cells"));
            Stopwatch sw2 = Stopwatch.StartNew();
            var weigths = CalcLinearWeights(computationalContext, cells);
            sw2.Stop();
            TraceVerbose(string.Format("calculations of linear weights for all cells ended in {0}",sw2.Elapsed));

            TraceVerbose(string.Format("Waiting for all variograms to be computed"));
            await Task.WhenAll(variogramTasks);
            TraceVerbose(string.Format("All variograms are computed. Calculating variances and values"));

            Stopwatch sw3 = Stopwatch.StartNew();
            var resultValues = cells.Zip(weigths, (cell,weightTuple) =>
                {                    
                    ITimeSegment ts = cell.Time;
                    var weight = weightTuple.Item2;
                    VariogramModule.IVariogram variogram = variograms[ts];
                    var observation = observations[ts].Observations;

                    Debug.Assert(Math.Abs(weight.Sum(w=>w.Weight)-1.0)<1e-10);

                    double sill = variogram.Sill;
                    
                    double cellLat = (cell.LatMax+cell.LatMin)*0.5;
                    double cellLon = (cell.LonMax+cell.LonMin)*0.5;
                    //var = cov(0)+ sum sum (w[i]*w[j]*cov(i,j))-2.0*sum(w[i]*cov(x,i))
                    double cov_at_0 = sill;
                    
                    double acc = cov_at_0; //cov(0)                    
                    for (int i = 0; i < weight.Length; i++)
			        {       
                        double w = weight[i].Weight;
                        int idx1 = weight[i].DataIndex;
                        double lat1 = observation[idx1].Latitude;
                        double lon1 = observation[idx1].Longitude;
			            for (int j = 0; j < i; j++)                    
			            {
                            int idx2 = weight[j].DataIndex;
                            double lat2 = observation[idx2].Latitude;
                            double lon2 = observation[idx2].Longitude;
                            double dist = SphereMath.GetDistance(lat1, lon1, lat2, lon2);
                            double cov = sill-variogram.GetGamma(dist);
                            acc += 2.0 * w * weight[j].Weight * cov;
			            }
                        acc += w * w * cov_at_0; //diagonal elements
                        double dist2 = SphereMath.GetDistance(lat1,lon1,cellLat,cellLon);
                        double cov2 = sill-variogram.GetGamma(dist2);
                        acc -= 2.0*w*cov2;
        			}
                    return Tuple.Create(cell,Math.Sqrt(acc),weight.Sum(w => observation[w.DataIndex].Value*w.Weight));
                }).ToArray();
            sw3.Stop();
            TraceVerbose(string.Format("All sigmas calulated in {0}",sw3.Elapsed));
            computationalContext.Add("results",resultValues);
            return resultValues.Select(r => r.Item2).ToArray();            
        }

        protected async override Task<double[]> AggregateCellsBatchAsync(IRequestContext context, ComputationalContext computationalContext, IEnumerable<GeoCellTuple> cells)
        {
            TraceVerbose(string.Format("Aggregation: exracting already computed values"));
            Stopwatch sw = Stopwatch.StartNew();
            Tuple<GeoCellTuple, double, double>[] precomputed = (Tuple<GeoCellTuple, double, double>[])computationalContext["results"];
            int idx = 0;
            GeoCellTuple[] cellsArray = cells.ToArray();
            int n = cellsArray.Length;
            double[] res = new double[n];
            for (int aggIdx = 0; aggIdx < n; aggIdx++)
            {
                while (!cellsArray[aggIdx].Equals(precomputed[idx].Item1)) idx++;
                res[aggIdx] = precomputed[idx].Item3;
            }
            sw.Stop();
            TraceVerbose(string.Format("Aggregation: extracted values in {0}",sw.Elapsed));
            return res;
        }
    }
}
