using Microsoft.Research.Science.Data;
using Microsoft.Research.Science.Data.Imperative;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;

namespace Microsoft.Research.Science.FetchClimate2
{
    [TestClass]
    public class LocalFetchTests
    {
        //[TestMethod]
        //public void FetchPointTest()
        //{
        //    InProcessContext ctx = new InProcessContext();
        //    LocalFetchClient lfc = new LocalFetchClient(ctx);

        //    DataSet request = DataSet.Open("msds:memory");
        //    request.AddVariable<double>("Lon", new double[] { 2.0 }, "i");
        //    request.AddVariable<double>("Lat", new double[] { 2.0 }, "i");
        //    request.SetTimeRegion();
        //    request.Commit();

        //    DataSet result = lfc.FetchAsync(request).Result;
        //    Assert.IsTrue(result.Variables.Contains("Values"));
        //    Assert.IsTrue(result.Variables.Contains("SD"));
        //}

        //[TestMethod]
        //public void FetchCubeTest()
        //{
        //    InProcessContext ctx = new InProcessContext();
        //    LocalFetchClient lfc = new LocalFetchClient(ctx);

        //    using (DataSet request = DataSet.Open("msds:memory"))
        //    {
        //        // Around Moscow
        //        //request.AddAxis("Lon", "Degrees east", 30, 44, 1.0); // 15 points
        //        //request.AddAxis("Lat", "Degrees north", 50, 60, 1.0); // 11 points
        //        //request.SetTimeseriesMonthly(firstMonth: 2, lastMonth: 3);

        //        // Sri Lanka
        //        request.AddAxis("Lon", "Degrees east", 69.0, 86.0, 0.25);
        //        request.AddAxis("Lat", "Degrees north", 5.0, 20.0, 0.25);
        //        //request.SetTimeseriesSeasonly(firstYear: 2000, lastYear: 2001, firstDay: 334, lastDay: 31);
        //        request.SetTimeseriesMonthly(firstMonth: 2, lastMonth: 12, firstYear: 2000, lastYear: 2001);

        //        request.Metadata["VariableName"] = "air0";
        //        request.Metadata["Timestamp"] = DateTime.Now;
        //        request.Commit();

        //        Stopwatch sw = new Stopwatch();
        //        sw.Start();
        //        DataSet result = lfc.FetchAsync(request).Result;
        //        sw.Start();
        //        Trace.WriteLine("Fetch takes " + sw.Elapsed);

        //        Assert.IsTrue(result.Variables.Contains("Values"));
        //        Assert.IsTrue(result.Variables.Contains("SD"));
        //        Assert.IsTrue(result.Variables.Contains("ID"));

        //        Assert.AreEqual(result.Dimensions["Lon"].Length, 69);
        //        Assert.AreEqual(result.Dimensions["Lat"].Length, 61);
        //        Assert.AreEqual(result.Dimensions["t"].Length, 11);

        //    }
        //}
    }
}