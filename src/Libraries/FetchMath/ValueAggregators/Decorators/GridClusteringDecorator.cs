using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;



namespace Microsoft.Research.Science.FetchClimate2
{
    /// <summary>
    /// cluster the requested region using needed raw data size and process clusters in parallel
    /// </summary>
    public class GridClusteringDecorator : IBatchValueAggregator
    {
        private AutoRegistratingTraceSource ts = new AutoRegistratingTraceSource("GridClusteringDecorator", SourceLevels.All);

        private readonly int clusterSizeInMegabytes; // Megabytes
        private readonly IBatchValueAggregator component;
        private readonly ISpatGridBoundingBoxCalculator latAxisBBcalc;
        private readonly ISpatGridBoundingBoxCalculator lonAxisBBcalc;
        private readonly ITimeAxisBoundingBoxCalculator timeAxisBBcalc;
        private readonly IReadOnlyDictionary<string, Type> varDataTypes;

        public GridClusteringDecorator(IDataStorageDefinition storageDefinition, IBatchValueAggregator component, ITimeAxisBoundingBoxCalculator timeAxisBBcalc, ISpatGridBoundingBoxCalculator latAxisBBcalc, ISpatGridBoundingBoxCalculator lonAxisBBcalc, int clusterSizeInMegabytes = 128)
        {
            this.component = component;
            this.clusterSizeInMegabytes = clusterSizeInMegabytes;
            this.latAxisBBcalc = latAxisBBcalc;
            this.lonAxisBBcalc = lonAxisBBcalc;
            this.timeAxisBBcalc = timeAxisBBcalc;

            varDataTypes = storageDefinition.VariablesTypes;
        }


        public async Task<double[]> AggregateCellsBatchAsync(IEnumerable<ICellRequest> cells)
        {
            ICellRequest[] cellsArray = cells.ToArray();
            if (cellsArray.Length == 0)
                return new double[0];
            else
            {
                string variable = cellsArray[0].VariableName;
                Type dataType = varDataTypes[variable];
                int dataElementSizeBytes = Marshal.SizeOf(dataType);
                double[] result = new double[cellsArray.Length];

                var bb3Dseq = ConvertToBoundingBox3D(cellsArray);
                GreedyClustering<BoundingBox3D> clusterer = new GreedyClustering<BoundingBox3D>(new BoundingBoxVolumeSpaceInfo(), clusterSizeInMegabytes * 1024 * 1024 / dataElementSizeBytes /* 128 Mb*/);
                Stopwatch clusteringSw = Stopwatch.StartNew();
                ts.TraceEvent(TraceEventType.Start, 1, "Clustering started");
                var clusterIndeces = clusterer.GetClusters(bb3Dseq);
                clusteringSw.Stop();
                ts.TraceEvent(TraceEventType.Stop, 1, string.Format("Got {0} clusters in {1}", clusterIndeces.Length, clusteringSw.Elapsed));

                ICellRequest[][] clusters = new ICellRequest[clusterIndeces.Length][];
                Task[] clusterTasks = new Task[clusterIndeces.Length];
                for (int i = 0; i < clusterIndeces.Length; i++)
                {
                    int localI = i;
                    clusterTasks[i] = Task.Run(async () =>
                    {
                        int[] indecesInsideCluster = clusterIndeces[localI];
                        int clusterSize = indecesInsideCluster.Length;
                        clusters[localI] = new ICellRequest[clusterSize];
                        for (int j = 0; j < clusterSize; j++)
                            clusters[localI][j] = cellsArray[indecesInsideCluster[j]];


                        Stopwatch aggClusterSw = Stopwatch.StartNew();
                        double[] clusterRes = await component.AggregateCellsBatchAsync(clusters[localI]);
                        aggClusterSw.Stop();
                        ts.TraceEvent(TraceEventType.Information, 2, string.Format("Aggregated cluster {0}({1} elements) out of {3} in {2}", localI + 1, clusterSize, aggClusterSw.Elapsed, clusterIndeces.Length));
                        for (int j = 0; j < clusterSize; j++)
                            result[indecesInsideCluster[j]] = clusterRes[j];

                        clusterIndeces[localI] = null;// freeing memory            
                        clusters[localI] = null;
                    });
                };
                await Task.WhenAll(clusterTasks);
                ts.TraceEvent(TraceEventType.Information, 3, "All clusters have been processed");
                return result;
            }
        }


        private class BoundingBox3D : Tuple<IndexBoundingBox, IndexBoundingBox, IndexBoundingBox>
        {
            public BoundingBox3D(IndexBoundingBox fst, IndexBoundingBox snd, IndexBoundingBox trd)
                : base(fst, snd, trd)
            { }
        }

        private class BoundingBoxVolumeSpaceInfo : IGreedyClusterSpace<BoundingBox3D>
        {
            public BoundingBox3D GetUnion(BoundingBox3D first, BoundingBox3D second)
            {
                var bb1 = IndexBoundingBox.Union(first.Item1, second.Item1);
                var bb2 = IndexBoundingBox.Union(first.Item2, second.Item2);
                var bb3 = IndexBoundingBox.Union(first.Item3, second.Item3);

                return new BoundingBox3D(bb1, bb2, bb3);
            }

            public long GetSize(BoundingBox3D obj)
            {
                return (obj.Item1.last - obj.Item1.first + 1L) * (obj.Item2.last - obj.Item2.first + 1L) * (obj.Item3.last - obj.Item3.first + 1L);
            }
        }

        private IEnumerable<BoundingBox3D> ConvertToBoundingBox3D(IEnumerable<IGeoCell> cells)
        {
            foreach (var geoCellTuple in cells)
            {
                IndexBoundingBox timeBB = timeAxisBBcalc.GetBoundingBox(geoCellTuple.Time);
                IndexBoundingBox latBB = latAxisBBcalc.GetBoundingBox(geoCellTuple.LatMin, geoCellTuple.LatMax);
                IndexBoundingBox lonBB = lonAxisBBcalc.GetBoundingBox(geoCellTuple.LonMin, geoCellTuple.LonMax);
                yield return new BoundingBox3D(timeBB, latBB, lonBB);
            }
        }


    }
}
