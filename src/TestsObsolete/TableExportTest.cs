using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Research.Science.Data;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics;

namespace Microsoft.Research.Science.FetchClimate2.Tests
{
    [TestClass]
    [DeploymentItem("Data\\sampleconfig.json")]
    public class TableExportTest
    {
        static FetchConfiguration config;

        [ClassInitialize]
        public static void ClassInit(TestContext ctx)
        {
            config = JsonConvert.DeserializeObject<Serializable.FetchConfiguration>(File.ReadAllText("sampleconfig.json")).ConvertFromSerializable();
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        [DeploymentItem("Data\\airt_10x10_monthly.csv")]
        public void ExportMonthyGridTest()
        {
            using (var result = DataSet.Open("airt_10x10_monthly.csv"))
            {
                using (var table = DataSet.Open("msds:memory"))
                {
                    table.IsAutocommitEnabled = false;
                    TableExportHelper.MergeTable(config, table, new Tuple<DataSet, string[]>[] {
                        new Tuple<DataSet, string[]>(result, new string[] { "Region 1" })
                    });
                    Trace.WriteLine(table.ToString());
                    Assert.IsTrue(table.Dimensions["i"].Length == 9 * 9 * 12);
                    Assert.IsTrue(((DateTime[])table.Variables["start"].GetData())[0] == new DateTime(2000, 1, 1));
                    Assert.IsTrue(((DateTime[])table.Variables["end"].GetData())[9 * 9 * 12 - 1] == new DateTime(2011, 1, 1));
                    Assert.IsTrue(((string[])table.Variables["airt_provenance"].GetData())[100] == "NCEP/NCAR Reanalysis 1 (regular grid)");
                }
            }
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        [DeploymentItem("Data\\airt_10x10_monthly.csv")]
        [DeploymentItem("Data\\relhum_10x10_monthly.csv")]
        [DeploymentItem("Data\\prate_10x10_monthly.csv")]
        public void ExportMonthyGridTestFor2Vars()
        {
            if (File.Exists("output_table.csv"))
                File.Delete("output_table.csv");

            using (DataSet result1 = DataSet.Open("airt_10x10_monthly.csv"),
                result2 = DataSet.Open("relhum_10x10_monthly.csv"),
                result3 = DataSet.Open("prate_10x10_monthly.csv"))
            {
                using (var table = DataSet.Open("msds:memory"))
                {
                    table.IsAutocommitEnabled = false;
                    TableExportHelper.MergeTable(config, table, new Tuple<DataSet, string[]>[] {
                        new Tuple<DataSet, string[]>(result1, null),
                        new Tuple<DataSet, string[]>(result2, null),
                        new Tuple<DataSet, string[]>(result3, null),
                    });
                    Trace.WriteLine(table.ToString());
                    Assert.IsTrue(table.Dimensions["i"].Length == 9 * 9 * 12);
                    Assert.IsTrue(((string[])table.Variables["relhum_provenance"].GetData())[100] == "GFDLRelHum");
                    Assert.IsTrue(table.Variables.Contains("airt_uncertainty"));
                    Assert.IsTrue(table.Variables.Contains("relhum_uncertainty"));
                    Assert.IsTrue(table.Variables.Contains("prate_uncertainty"));
                }
            }

        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        [DeploymentItem("Data\\prate_10x10.csv")]
        [DeploymentItem("Data\\prate_5_points.csv")]
        public void ExportGridsAndPointsTest()
        {
            using (DataSet result1 = DataSet.Open("prate_10x10.csv"),
                result2 = DataSet.Open("prate_5_points.csv"))
            {
                using (var table = DataSet.Open("msds:memory"))
                {
                    table.IsAutocommitEnabled = false;
                    TableExportHelper.MergeTable(config, table, new Tuple<DataSet, string[]>[] {
                        new Tuple<DataSet, string[]>(result1, null),
                        new Tuple<DataSet, string[]>(result2, new string[] { "P1", "P2","P3","P4","P5" })
                    });
                    Trace.WriteLine(table.ToString());
                    Assert.IsTrue(table.Dimensions["i"].Length == 9 * 9 + 5);
                    Assert.IsTrue(((string[])table.Variables["prate_provenance"].GetData())[9 * 9 + 4] == "WorldClim 1.4");
                    Assert.IsTrue(((string[])table.Variables["region"].GetData())[81] == "P1");
                    Assert.IsTrue(((string[])table.Variables["region"].GetData())[9 * 9 + 4] == "P5");
                    Assert.IsTrue(((string[])table.Variables["region"].GetData())[0] == null);
                }
            }
        }

        [TestMethod]
        [TestCategory("BVT")]
        [TestCategory("Local")]
        [DeploymentItem("Data\\prate_41x21_monthly.csv")]
        public void ExportRectGridTest()
        {
            using (DataSet result1 = DataSet.Open("prate_41x21_monthly.csv"))
            {
                using (var table = DataSet.Open("msds:memory"))
                {
                    table.IsAutocommitEnabled = false;
                    TableExportHelper.MergeTable(config, table, new Tuple<DataSet, string[]>[] {
                        new Tuple<DataSet, string[]>(result1, new string[] { "Europe" })
                    });
                    Trace.WriteLine(table.ToString());
                    Assert.IsTrue(table.Dimensions["i"].Length == 40 * 20 * 12);
                }
            }
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        [DeploymentItem("Data\\airt_5d.csv")]
        public void Export5dTest()
        {
            using (DataSet result1 = DataSet.Open("airt_5d.csv"))
            {
                using (var table = DataSet.Open("msds:memory"))
                {
                    table.IsAutocommitEnabled = false;
                    TableExportHelper.MergeTable(config, table, new Tuple<DataSet, string[]>[] {
                        new Tuple<DataSet, string[]>(result1, new string[] { "Europe" })
                    });
                    Trace.WriteLine(table.ToString());
                    Assert.IsTrue(table.Dimensions["i"].Length == 4 * 4 * 6 * 4 * 12);
                }
            }
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        [DeploymentItem("Data\\elevation.csv")]
        public void ExportElevationTest()
        {
            using (DataSet result1 = DataSet.Open("elevation.csv"))
            {
                using (var table = DataSet.Open("msds:memory"))
                {
                    table.IsAutocommitEnabled = false;
                    TableExportHelper.MergeTable(config, table, new Tuple<DataSet, string[]>[] {
                        new Tuple<DataSet, string[]>(result1, new string[] { "Europe" })
                    });
                    Trace.WriteLine(table.ToString());
                }
            }
//            using (var ds = new AzureBlobDataSet(Uri.UnescapeDataString("msds%3Aab%3FAccountName%3Dfetchclimate2%26Container%3Drequests%26Blob%3Deac53851f2b91e94f804c7a054d78462d73d99b5")))
//                ds.Clone("elev.csv");
        }
    }
}
