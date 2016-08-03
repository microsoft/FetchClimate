using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Research.Science.FetchClimate2.Tests
{
    class Range1dSpaceInfo : IGreedyClusterSpace<Tuple<int, int>>
    {        
        public Tuple<int, int> GetUnion(Tuple<int, int> first, Tuple<int, int> second)
        {
            int min = Math.Min(first.Item1, second.Item1);
            int max = Math.Max(first.Item2, second.Item2);
            return Tuple.Create(min, max);
        }

        public long GetSize(Tuple<int, int> obj)
        {
            return obj.Item2 - obj.Item1;
        }
    }

    [TestClass]
    public class ClusteringTests
    {
        Tuple<int, int> a(int i) { return Tuple.Create(i,i);}

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void TestClusteringIntegers()
        {
            Tuple<int, int>[] toCluster = new int[] { 1, 2, 3, 9, 10, 11, 15, 16, 17, 18 }.Select(i => a(i)).ToArray();

            GreedyClustering<Tuple<int, int>> c = new GreedyClustering<Tuple<int, int>>(new Range1dSpaceInfo(), 3);
            var res = c.GetClusters(toCluster);

            Assert.IsTrue(res[2].Contains(0));
            Assert.IsTrue(res[2].Contains(1));
            Assert.IsTrue(res[2].Contains(2));

            Assert.IsTrue(res[1].Contains(3));
            Assert.IsTrue(res[1].Contains(4));
            Assert.IsTrue(res[1].Contains(5));

            Assert.IsTrue(res[0].Contains(6));
            Assert.IsTrue(res[0].Contains(7));
            Assert.IsTrue(res[0].Contains(8));
            Assert.IsTrue(res[0].Contains(9));


            toCluster = new int[] { 1, 2, 17, 9, 10, 11, 18, 16, 3, 15 }.Select(i => a(i)).ToArray(); ;

            c = new GreedyClustering<Tuple<int, int>>(new Range1dSpaceInfo(), 3);
            res = c.GetClusters(toCluster);

            Assert.IsTrue(res[1].Contains(0));
            Assert.IsTrue(res[1].Contains(1));
            Assert.IsTrue(res[1].Contains(8));

            Assert.IsTrue(res[2].Contains(3));
            Assert.IsTrue(res[2].Contains(4));
            Assert.IsTrue(res[2].Contains(5));

            Assert.IsTrue(res[0].Contains(6));
            Assert.IsTrue(res[0].Contains(7));
            Assert.IsTrue(res[0].Contains(2));
            Assert.IsTrue(res[0].Contains(9));    
            
        }

        [TestMethod]
        //[Timeout(10000)]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void TestOnePointInCluster()
        {
            GreedyClustering<Tuple<int, int>> clust = new GreedyClustering<Tuple<int, int>>(new Range1dSpaceInfo(), 5L);

        Tuple<int, int>[] array = new Tuple<int, int>[] {
            Tuple.Create(0,1),
            Tuple.Create(1,2),
            Tuple.Create(3,4),
            Tuple.Create(-1,6),
            Tuple.Create(8,10)
        };

        var indeces = clust.GetClusters(array);
        Assert.AreEqual(2, indeces.Length);
        Assert.IsTrue(indeces[0].Contains(0));
        Assert.IsTrue(indeces[0].Contains(1));
        Assert.IsTrue(indeces[0].Contains(2));
        Assert.IsTrue(indeces[0].Contains(3));
        Assert.IsTrue(indeces[1].Contains(4));
        }
    }
    
}
