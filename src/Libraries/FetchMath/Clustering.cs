using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    /// <summary>
    /// Provides the distance between two entities
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IGreedyClusterSpace<T>
    {
        //long GetDistance(T first, T second);
        T GetUnion(T first, T second);
        long GetSize(T obj);
    }

    /// <summary>
    /// Clusters the entities using custom distance function
    /// </summary>    
    /// <typeparam name="T"></typeparam>
    public class GreedyClustering<T>
    {
        long maxClusterSize;
        IGreedyClusterSpace<T> spaceInfo;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="spaceInfo">An object to get the distances from</param>
        /// <param name="maxClusterSize">The maximum single cluster size (element count)</param>
        public GreedyClustering(IGreedyClusterSpace<T> spaceInfo, long maxClusterSize)
        {
            this.maxClusterSize = maxClusterSize;
            this.spaceInfo = spaceInfo;
        }


        /// <summary>
        /// Returns the array of clusters together with corresponding indeces in the original array
        /// </summary>        
        /// <param name="toClusterize">an array to split into the clusters</param>
        /// <returns></returns>
        public int[][] GetClusters(IEnumerable<T> toClusterize)
        {
            List<int[]> clustersIndeces = new List<int[]>();

            //ordering elements by size desc
            long[] sizes = toClusterize.Select(c => spaceInfo.GetSize(c)).ToArray();
            var array = toClusterize.ToArray();
            int len = sizes.Length;
            int[] descendingSizeIndeces = Enumerable.Range(0, len).ToArray();
            Array.Sort(sizes, descendingSizeIndeces);
            descendingSizeIndeces = descendingSizeIndeces.Reverse().ToArray();
            T[] descendingSizeElems = new T[len];
            for (int i = 0; i < len; i++)
                descendingSizeElems[i] = array[descendingSizeIndeces[i]];

            IEnumerable<T> notInClusters = descendingSizeElems;
            IEnumerable<int> notInClusterIndeces = descendingSizeIndeces;
            while (notInClusters.Any())
            {
                var zipped = notInClusters.Zip(notInClusterIndeces, (p, i) => Tuple.Create(p, i));
                T aggregator = zipped.First().Item1;

                long effectiveClusterSize = Math.Max(spaceInfo.GetSize(aggregator),maxClusterSize); //if the first elemt is large than cluster size, it forms the cluster                    

                List<T> notInCurrentCluster = new List<T>();
                List<int> notInCurrentClusterIndeces = new List<int>();
                List<int> currClustIndeces = new List<int>();

                foreach (var item in zipped)
                {
                    var testValue = spaceInfo.GetUnion(aggregator, item.Item1);
                    if (spaceInfo.GetSize(testValue) > effectiveClusterSize)
                    {
                        notInCurrentCluster.Add(item.Item1);
                        notInCurrentClusterIndeces.Add(item.Item2);
                    }
                    else
                    {
                        currClustIndeces.Add(item.Item2);
                        aggregator = testValue;
                    }
                }

                notInClusters = notInCurrentCluster;
                notInClusterIndeces = notInCurrentClusterIndeces;
                clustersIndeces.Add(currClustIndeces.ToArray());
            }

            return clustersIndeces.ToArray();
        }
    }
}
