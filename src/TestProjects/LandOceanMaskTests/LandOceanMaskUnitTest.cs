using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO.Compression;
using System.IO;
using Microsoft.Research.Science.Data;
using Microsoft.Research.Science.Data.Imperative;
using Microsoft.Research.Science.FetchClimate2;

namespace LandOceanMaskTests
{
    [TestClass]
    public class LandOceanMaskUnitTest
    {
        [TestMethod]
        [TestCategory("Local")]
        [DeploymentItem("CruDataMask.bf")]
        [TestCategory("BVT")]
        public void CruLandOnlyTest()
        {
            GZipStream stream = new GZipStream(File.OpenRead("CruDataMask.bf"), CompressionMode.Decompress, false);
            DataMaskAnalyzer mask = new DataMaskAnalyzer(stream);

            Assert.AreEqual(1.0,mask.GetDataPercentage(10, 11, 10, 11));
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        [DeploymentItem("CruDataMask.bf")]
        public void CruSmallLandOnlyTest()
        {
            GZipStream stream = new GZipStream(File.OpenRead("CruDataMask.bf"), CompressionMode.Decompress, false);
            DataMaskAnalyzer mask = new DataMaskAnalyzer(stream);

            Assert.AreEqual(1.0, mask.GetDataPercentage(10, 11, 10, 10.2));//2bits wide mask
            Assert.AreEqual(1.0, mask.GetDataPercentage(10, 11, 10, 10.1));//1bit wide mask
        }

        [TestMethod]
        [TestCategory("Local")]
        [DeploymentItem("1.bf")]
        public void LandOceanMaskTest()
        {
            GZipStream stream = new GZipStream(File.OpenRead("1.bf"), CompressionMode.Decompress, false);
            DataMaskAnalyzer mask = new DataMaskAnalyzer(stream);

            string[] URIs = new string[] {
            @"msds:nc?file=\\vienna.mslab.cs.msu.su\ClimateData\WorldClimCur3.nc&openMode=readOnly",
            @"msds:nc?file=D:\ClimateData\WorldClimCurr.nc&openMode=readOnly",
            @"msds:nc?file=C:\ClimateData\WorldClimCurr.nc&openMode=readOnly",
            @"msds:az?name=WorldClimCurrent&DefaultEndpointsProtocol=http&AccountName=fetch&AccountKey=1Y0EOrnCX6ULY8c3iMHg9rrul2BWbPHKsHUceZ7SSh+ShM/q9K0ml49gQm+PE7G7i7zCvrpuT/vT1aHzEArutw=="};
            DataSet d = null;
            foreach(string uri in URIs)
            {
                try
                {
                    d = DataSet.Open(uri);
                    break;
                }
                catch (DataSetCreateException)
                {
                    continue;
                }
            }
            Assert.IsNotNull(d);

            Int16[] tmean;
            Single[] lat;
            Single[] lon;
            var dims = d["tmean"].Dimensions;
            Int32 latLen = dims["lat"].Length;
            Int32 lonLen = dims["lon"].Length;
            Int16 missingValue = (Int16)d["tmean"].MissingValue;
            lat = d.GetData<Single[]>("lat");
            lon = d.GetData<Single[]>("lon");

            //check values on a frame
            tmean = d.GetData<Int16[]>("tmean", DataSet.ReduceDim(0), DataSet.ReduceDim(0), DataSet.FromToEnd(0));
            for (int i = 0; i < tmean.Length; ++i)
            {
                Assert.AreEqual(tmean[i] != missingValue, mask.HasData(lat[0], lon[i]));
            }

            tmean = d.GetData<Int16[]>("tmean", DataSet.ReduceDim(0), DataSet.ReduceDim(latLen - 1), DataSet.FromToEnd(0));
            for (int i = 0; i < tmean.Length; ++i)
            {
                Assert.AreEqual(tmean[i] != missingValue, mask.HasData(lat[latLen - 1], lon[i]));
            }

            tmean = d.GetData<Int16[]>("tmean", DataSet.ReduceDim(0), DataSet.FromToEnd(0), DataSet.ReduceDim(0));
            for (int i = 0; i < tmean.Length; ++i)
            {
                Assert.AreEqual(tmean[i] != missingValue, mask.HasData(lat[i], lon[0]));
            }

            tmean = d.GetData<Int16[]>("tmean", DataSet.ReduceDim(0), DataSet.FromToEnd(0), DataSet.ReduceDim(lonLen - 1));
            for (int i = 0; i < tmean.Length; ++i)
            {
                Assert.AreEqual(tmean[i] != missingValue, mask.HasData(lat[i], lon[lonLen - 1]));
            }

            //cross in the middle
            tmean = d.GetData<Int16[]>("tmean", DataSet.ReduceDim(0), DataSet.FromToEnd(0), DataSet.ReduceDim(lonLen / 2));
            for (int i = 0; i < tmean.Length; ++i)
            {
                Assert.AreEqual(tmean[i] != missingValue, mask.HasData(lat[i], lon[lonLen / 2]));
            }

            tmean = d.GetData<Int16[]>("tmean", DataSet.ReduceDim(0), DataSet.ReduceDim(latLen / 2), DataSet.FromToEnd(0));
            for (int i = 0; i < tmean.Length; ++i)
            {
                Assert.AreEqual(tmean[i] != missingValue, mask.HasData(lat[latLen / 2], lon[i]));
            }

            Int16[,] tmeanSquare;

            tmeanSquare = d.GetData<Int16[,]>("tmean", DataSet.ReduceDim(0), DataSet.Range(10000, 11000), DataSet.Range(10000, 11000));
            int land = 0;
            for (int i = 0; i <= 1000; ++i)
                for (int j = 0; j <= 1000; ++j)
                    if (tmeanSquare[i, j] != missingValue) ++land;
            double ans = ((double) land) / (1001 * 1001);
            Assert.AreEqual(ans, mask.GetDataPercentage(lat[10000], lat[11000], lon[10000], lon[11000]));
        }
    }
}
