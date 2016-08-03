using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.InteropServices;

namespace DataHandlersTests.Math
{
    [TestClass]
    public class UtilsModeArrayTests
    {
        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void TestFindMode3DUInt8()
        {
            Array data = Array.CreateInstance(typeof(Byte),3,4,5);

            var threes = Enumerable.Repeat((byte)3,10);
            var tens = Enumerable.Repeat((byte)10,20);
            var fifties = Enumerable.Repeat((byte)50,30);

            var rand = new Random(1);

            byte[] allArray = threes.Concat(tens).Concat(fifties).ToArray();
            double[] randArray = Enumerable.Repeat(0,60).Select(dummy => rand.NextDouble()).ToArray();

            Array.Sort(randArray,allArray);

            Buffer.BlockCopy(allArray,0,data,0,60);

            int[] i_idx = Enumerable.Range(0,3).Select(i => i+11).ToArray(); //data subset origin is 11
            int[] j_idx = Enumerable.Range(0,4).Select(i => i+13).ToArray(); //data subset origin is 13
            int[] k_idx = Enumerable.Range(0,5).Select(i => i+17).ToArray(); //data subset origin is 17

            GCHandle? capturedHandle;
            capturedHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            IntPtr prefetchedDataPtr = capturedHandle.Value.AddrOfPinnedObject();
            try{
                double mode = Microsoft.Research.Science.FetchClimate2.Utils.ArrayMode.FindMode3DUInt8(prefetchedDataPtr, 4, 5, 11, 13, 17, i_idx, j_idx, k_idx, (byte)0);
                Assert.AreEqual(50.0, mode);

                mode = Microsoft.Research.Science.FetchClimate2.Utils.ArrayMode.FindMode3DUInt8(prefetchedDataPtr, 4, 5, 11, 13, 17, i_idx, j_idx, k_idx);
                Assert.AreEqual(50.0, mode);
            }
            finally{
                capturedHandle.Value.Free();
            }
        }
    }
}
