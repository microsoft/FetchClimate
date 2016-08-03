using Microsoft.Research.Science.Data;
using Microsoft.Research.Science.FetchClimate2.DataHandlers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Tests
{
    /// <summary>
    /// Provides simple plane generating observations:
    /// for 1970 Jan Feb Mar May Temp=(lat>1)?(3/5*lat-1):(3/5*lat)  Prate=3/5*lat
    /// for 1970 Apr Temp=(lat>1)?(6/5*lat-1):(6/5*lat) Prate=6/5*lat
    /// 
    /// elevation:
    /// for lat>0 6490 meters (1 deg cooler)
    /// else 0 meters
    /// </summary>
    class GhcnStorageContext : IStorageContext, IRequestContext
    {
        DataSet data = DataSet.Open("msds:memory");
        FetchRequest request;

        double[] lats = new double[] { -5.0, 5.0, 5.0 };
        double[] lons = new double[] { 0.0, 0.0, 6.0 };
        double[,] tvals = new double[,] {
        { -3.0, 2.0, 2.0 },
        { -3.0, 2.0, 2.0 },
        { -3.0, 2.0, 2.0 },
        { -6.0, 5.0, 5.0 },
        { -3.0, 2.0, 2.0 }
        };
        double[,] pvals = new double[,] {
        { -3.0, 3.0, 3.0 },
        { -3.0, 3.0, 3.0 },
        { -3.0, 3.0, 3.0 },
        { -6.0, 6.0, 6.0 },
        { -3.0, 3.0, 3.0 }
        };
        DateTime[] times = new DateTime[] {
            new DateTime(1970,1,1),
            new DateTime(1970,2,1),
            new DateTime(1970,3,1),
            new DateTime(1970,4,1),
            new DateTime(1970,5,1)
        };

        public GhcnStorageContext(FetchRequest request)
        {
            this.request = request;
            data.AddVariable<double>("lat", lats, "i");
            data.AddVariable<double>("lon", lons, "i");
            data.AddVariable<double>("temp", tvals, "t", "i");
            data.AddVariable<double>("prate", pvals, "t", "i");
            data.AddVariable<DateTime>("time", times, "t");
        }

        public DataStorageDefinition StorageDefinition
        {
            get
            {
                var definition = new DataStorageDefinition();
                definition.VariablesDimensions.Add("lat", new string[] { "i" });
                definition.VariablesDimensions.Add("lon", new string[] { "i" });
                definition.VariablesDimensions.Add("temp", new string[] { "t", "i" });
                definition.VariablesDimensions.Add("prate", new string[] { "t", "i" });
                definition.VariablesDimensions.Add("time", new string[] { "t" });

                definition.VariablesMetadata.Add("lat", new MetaDataDictionary());
                definition.VariablesMetadata.Add("lon", new MetaDataDictionary());
                definition.VariablesMetadata.Add("temp", new MetaDataDictionary());
                definition.VariablesMetadata.Add("prate", new MetaDataDictionary());
                definition.VariablesMetadata.Add("time", new MetaDataDictionary());

                definition.VariablesTypes.Add("lat", typeof(double));
                definition.VariablesTypes.Add("lon", typeof(double));
                definition.VariablesTypes.Add("temp", typeof(double));
                definition.VariablesTypes.Add("prate", typeof(double));
                definition.VariablesTypes.Add("time", typeof(DateTime));
                return definition;
            }
        }

        public async Task<StorageResponse[]> GetDataAsync(params StorageRequest[] requests)
        {
            StorageResponse[] sr = new StorageResponse[requests.Length];
            for (int i = 0; i < requests.Length; i++)
            {
                sr[i] = new StorageResponse(requests[i], data.Variables[requests[i].VariableName].GetData(requests[i].Origin, requests[i].Shape));
            }
            return sr;
        }

        public FetchRequest Request
        {
            get { return request; }
        }

        public async Task<Array> GetMaskAsync(Array uncertainty)
        {
            return Enumerable.Repeat(true, uncertainty.Length).ToArray();
        }

        public async Task<FetchResponse[]> FetchDataAsync(params FetchRequest[] requests)
        {
            FetchResponse[] res = new FetchResponse[requests.Length];
            for (int i = 0; i < requests.Length; i++)
            {
                if (requests[i].Domain.SpatialRegionType == SpatialRegionSpecification.Points)
                {
                    double[] elevations = new double[requests[i].Domain.Lats.Length];
                    for (int j = 0; j < elevations.Length; j++)
                    {
                        elevations[j] = requests[i].Domain.Lats[j] > 1 ? 1 / 0.00649 : 0.0;
                    }
                    res[i] = new FetchResponse(requests[i], elevations, Enumerable.Repeat(1.0, elevations.Length).ToArray());
                }
                else if (requests[i].Domain.SpatialRegionType == SpatialRegionSpecification.PointGrid)
                {
                    double[,] elevations = new double[requests[i].Domain.Lons.Length, requests[i].Domain.Lats.Length];
                    for (int j = 0; j < elevations.GetLength(0); j++)
                    {
                        for (int k = 0; k < elevations.GetLength(1); k++)
                        {
                            elevations[j, k] = requests[i].Domain.Lats[k] > 1 ? 1 / 0.00649 : 0.0;                            
                        }
                    }
                    res[i] = new FetchResponse(requests[i], elevations, new double[requests[i].Domain.Lons.Length, requests[i].Domain.Lats.Length]);
                }
                else
                    throw new NotSupportedException("only points and point grid is supported");

            } return res;
        }




        public IRequestContext CopyWithNewRequest(FetchRequest request)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// TODO: merge duplicating code with class above
    /// 
    /// 
    /// Provides simple plane generating observations:
    /// for 1970 Jan Feb Mar May Temp=(lat>1)?(-4):(-3)  Prate= -3
    /// for 1970 Apr Temp=(lat>1)?(-7):(-6) Prate= -3
    /// 
    /// elevation:
    /// for lat>0 6490 meters (1 deg cooler)
    /// else 0 meters
    /// </summary>
    class GhcnPlaneStorageContext : IStorageContext, IRequestContext
    {
        DataSet data = DataSet.Open("msds:memory");
        FetchRequest request;

        double[] lats = new double[] { -5.0, 5.0, 5.0 };
        double[] lons = new double[] { 0.0, 0.0, 6.0 };
        double[,] tvals = new double[,] {
        { -3.0, -4.0, -4.0 },
        { -3.0, -4.0, -4.0 },
        { -3.0, -4.0, -4.0 },
        { -6.0, -7.0, -7.0 },
        { -3.0, -4.0, -4.0 }
        };
        double[,] pvals = new double[,] {
        { -3.0, -3.0,-3.0 },
        { -3.0, -3.0,-3.0 },
        { -3.0, -3.0,-3.0 },
        { -6.0, -6.0, -6.0 },
        { -3.0, -3.0, -3.0 }
        };
        DateTime[] times = new DateTime[] {
            new DateTime(1970,1,1),
            new DateTime(1970,2,1),
            new DateTime(1970,3,1),
            new DateTime(1970,4,1),
            new DateTime(1970,5,1)
        };

        public GhcnPlaneStorageContext(FetchRequest request)
        {
            this.request = request;
            data.AddVariable<double>("lat", lats, "i");
            data.AddVariable<double>("lon", lons, "i");
            data.AddVariable<double>("temp", tvals, "t", "i");
            data.AddVariable<double>("prate", pvals, "t", "i");
            data.AddVariable<DateTime>("time", times, "t");
        }

        public DataStorageDefinition StorageDefinition
        {
            get
            {
                var definition = new DataStorageDefinition();
                definition.VariablesDimensions.Add("lat", new string[] { "i" });
                definition.VariablesDimensions.Add("lon", new string[] { "i" });
                definition.VariablesDimensions.Add("temp", new string[] { "t", "i" });
                definition.VariablesDimensions.Add("prate", new string[] { "t", "i" });
                definition.VariablesDimensions.Add("time", new string[] { "t" });

                definition.VariablesMetadata.Add("lat", new MetaDataDictionary());
                definition.VariablesMetadata.Add("lon", new MetaDataDictionary());
                definition.VariablesMetadata.Add("temp", new MetaDataDictionary());
                definition.VariablesMetadata.Add("prate", new MetaDataDictionary());
                definition.VariablesMetadata.Add("time", new MetaDataDictionary());

                definition.VariablesTypes.Add("lat", typeof(double));
                definition.VariablesTypes.Add("lon", typeof(double));
                definition.VariablesTypes.Add("temp", typeof(double));
                definition.VariablesTypes.Add("prate", typeof(double));
                definition.VariablesTypes.Add("time", typeof(DateTime));
                return definition;
            }
        }

        public async Task<StorageResponse[]> GetDataAsync(params StorageRequest[] requests)
        {
            StorageResponse[] sr = new StorageResponse[requests.Length];
            for (int i = 0; i < requests.Length; i++)
            {
                sr[i] = new StorageResponse(requests[i], data.Variables[requests[i].VariableName].GetData(requests[i].Origin, requests[i].Shape));
            }
            return sr;
        }

        public FetchRequest Request
        {
            get { return request; }
        }

        public async Task<Array> GetMaskAsync(Array uncertainty)
        {
            return Enumerable.Repeat(true, uncertainty.Length).ToArray();
        }

        public async Task<FetchResponse[]> FetchDataAsync(params FetchRequest[] requests)
        {
            FetchResponse[] res = new FetchResponse[requests.Length];
            for (int i = 0; i < requests.Length; i++)
            {
                if (requests[i].Domain.SpatialRegionType == SpatialRegionSpecification.Points || requests[i].Domain.SpatialRegionType == SpatialRegionSpecification.Cells)
                {
                    double[] elevations = new double[requests[i].Domain.Lats.Length];
                    for (int j = 0; j < elevations.Length; j++)
                    {
                        elevations[j] = requests[i].Domain.Lats[j] > 1 ? 1 / 0.00649 : 0.0;
                    }
                    res[i] = new FetchResponse(requests[i], elevations, Enumerable.Repeat(1.0, elevations.Length).ToArray());
                }
                else if (requests[i].Domain.SpatialRegionType == SpatialRegionSpecification.PointGrid || requests[i].Domain.SpatialRegionType == SpatialRegionSpecification.CellGrid)
                {
                    double[,] elevations = new double[requests[i].Domain.Lons.Length, requests[i].Domain.Lats.Length];
                    for (int j = 0; j < elevations.GetLength(0); j++)
                    {
                        for (int k = 0; k < elevations.GetLength(1); k++)
                        {
                            elevations[j, k] = requests[i].Domain.Lats[k] > 1 ? 1 / 0.00649 : 0.0;
                        }
                    }
                    res[i] = new FetchResponse(requests[i], elevations, new double[requests[i].Domain.Lons.Length, requests[i].Domain.Lats.Length]);
                }
                else
                    throw new NotSupportedException("only points and point grid is supported");

            } return res;
        }




        public IRequestContext CopyWithNewRequest(FetchRequest request)
        {
            throw new NotImplementedException();
        }
    }

    [TestClass]
    public class GhcnTests
    {
        [TestMethod]
        [TestCategory("Local")]
        public void GHCNprateInterpolationPointsTest()
        {
            FetchDomain fd = FetchDomain.CreatePoints(new double[] { 0.5 }, new double[] { 1.0 }, new TimeRegion(firstYear: 1970, lastYear: 1970, firstDay: 91, lastDay: 120)); //april
            FetchRequest fr = new FetchRequest("prate", fd);

            GhcnStorageContext storage = new GhcnStorageContext(fr);
            GHCNDataHandler sotdh = new GHCNDataHandler(storage);

            var compCont = new ComputationalContext();

            var evRes = sotdh.EvaluateAsync(storage, compCont).Result;

            var result = (double[])sotdh.AggregateAsync(storage, compCont).Result; //val = 6/5*lat = 6/5*(0.5) = -3
            Assert.AreEqual(0.6, result[0], TestConstants.DoublePrecision);

            fd = FetchDomain.CreatePoints(new double[] { -5.0 }, new double[] { 1.0 }, new TimeRegion(firstYear: 1970, lastYear: 1970, firstDay: 91, lastDay: 120)); //april
            fr = new FetchRequest("prate", fd);

            storage = new GhcnStorageContext(fr);
            sotdh = new GHCNDataHandler(storage);

            compCont = new ComputationalContext();

            evRes = sotdh.EvaluateAsync(storage, compCont).Result;

            result = (double[])sotdh.AggregateAsync(storage, compCont).Result; //val = 6/5*lat = 6/5*(-2.5) = -3
            Assert.AreEqual(0.0, result[0], TestConstants.DoublePrecision); //as prate can't be negative, it is coerced to zero
        }

        [TestMethod]
        [TestCategory("Local")]
        public void GHCNnanForOutOfObsConvexHullTest()
        {
            FetchDomain fd = FetchDomain.CreatePoints(new double[] { -10 }, new double[] { 1.0 }, new TimeRegion(firstYear: 1970, lastYear: 1970, firstDay: 60, lastDay: 90)); //mar
            FetchRequest fr = new FetchRequest("prate", fd);

            GhcnStorageContext storage = new GhcnStorageContext(fr);
            GHCNDataHandler sotdh = new GHCNDataHandler(storage);

            var compCont = new ComputationalContext();

            var evRes = (double[])sotdh.EvaluateAsync(storage, compCont).Result;

            Assert.IsTrue(double.IsNaN(evRes[0]));

            var result = (double[])sotdh.AggregateAsync(storage, compCont).Result; //must be NAN as requested point out of observations convex hull
            Assert.IsTrue(double.IsNaN(result[0]));
        }

        [TestMethod]
        [TestCategory("Local")]
        public void GHCNtempInterpolationPointsTest()
        {
            FetchDomain fd = FetchDomain.CreatePoints(new double[] { 1.5 }, new double[] { 1.0 }, new TimeRegion(firstYear: 1970, lastYear: 1970, firstDay: 60, lastDay: 90)); //mar
            FetchRequest fr = new FetchRequest("temp", fd);

            GhcnStorageContext storage = new GhcnStorageContext(fr);
            GHCNDataHandler sotdh = new GHCNDataHandler(storage);

            var compCont = new ComputationalContext();

            var evRes = sotdh.EvaluateAsync(storage, compCont).Result;

            var result = (double[])sotdh.AggregateAsync(storage, compCont).Result; //val = 3/5*lat-1 = 3/5*(1.5)-1 = -0.1
            Assert.AreEqual(-0.1, result[0], TestConstants.DoublePrecision);

            fd = FetchDomain.CreatePoints(new double[] { -2.5 }, new double[] { 1.0 }, new TimeRegion(firstYear: 1970, lastYear: 1970, firstDay: 60, lastDay: 90)); //mar
            fr = new FetchRequest("temp", fd);

            storage = new GhcnStorageContext(fr);
            sotdh = new GHCNDataHandler(storage);

            compCont = new ComputationalContext();

            evRes = sotdh.EvaluateAsync(storage, compCont).Result;

            result = (double[])sotdh.AggregateAsync(storage, compCont).Result; //val =  3/5*lat =  3/5*(-2.5) = -1.5
            Assert.AreEqual(-1.5, result[0], TestConstants.DoublePrecision);
        }

        [TestMethod]
        [TestCategory("Local")]
        public void GHCNplaneInterpolationCellTest()
        {
            //with lapse rate correction
            FetchDomain fd = FetchDomain.CreateCells(new double[] { 1.5 }, new double[] { 1.0 }, new double[] { 1.6 }, new double[] { 1.1 }, new TimeRegion(firstYear: 1970, lastYear: 1970, firstDay: 91, lastDay: 120)); //april
            FetchRequest fr = new FetchRequest("temp", fd);

            GhcnPlaneStorageContext storage = new GhcnPlaneStorageContext(fr);
            GHCNDataHandler sotdh = new GHCNDataHandler(storage);

            var compCont = new ComputationalContext();

            var evRes = sotdh.EvaluateAsync(storage, compCont).Result;

            var result = (double[])sotdh.AggregateAsync(storage, compCont).Result;
            Assert.AreEqual(-7.0, result[0], TestConstants.DoublePrecision);


            //without lapse rate correction
            fd = FetchDomain.CreateCells(new double[] { -2.5 }, new double[] { 1.0 }, new double[] { -2.4 }, new double[] { 1.1 }, new TimeRegion(firstYear: 1970, lastYear: 1970, firstDay: 91, lastDay: 120)); //april
            fr = new FetchRequest("temp", fd);
            storage = new GhcnPlaneStorageContext(fr);
            sotdh = new GHCNDataHandler(storage);

            compCont = new ComputationalContext();

            evRes = sotdh.EvaluateAsync(storage, compCont).Result;

            result = (double[])sotdh.AggregateAsync(storage, compCont).Result;
            Assert.AreEqual(-6.0, result[0], TestConstants.DoublePrecision);
        }

        /// <summary>
        /// Manual comparison
        /// </summary>
        [TestMethod]
        [TestCategory("Local")]
        public void GhcnPrateValuesTest()
        {
            System.Diagnostics.Trace.WriteLine(TestConstants.UriGHCN);                        
            var storage = TestDataStorageFactory.GetStorage(TestConstants.UriGHCN);
            GHCNDataHandler handler = new GHCNDataHandler(storage);

            TimeRegion tr = new TimeRegion(firstYear: 1914, lastYear: 1914).GetMonthlyTimeseries(firstMonth: 12, lastMonth: 12);//data index 2567
            FetchDomain domain = FetchDomain.CreatePoints(
                new double[] { -25.18 }, //exact station. data index 18398
                new double[] { 151.65 },
                 tr);

            FetchRequest prateRequest = new FetchRequest("prate", domain);

            var compCont = new ComputationalContext();

            var reqContext = RequestContextStub.GetStub(storage, prateRequest);
            var evRes = handler.EvaluateAsync(reqContext, compCont).Result;

            Assert.AreEqual(85.1, (double)handler.AggregateAsync(reqContext, compCont).Result.GetValue(0), 1e-3); //manual data comparison.
        }

        /// <summary>
        /// Manual comparison
        /// </summary>
        [TestMethod]
        [TestCategory("Local")]
        public void GhcnPrateUncertaintyTest()
        {
            System.Diagnostics.Trace.WriteLine(TestConstants.UriGHCN);
            var storage = TestDataStorageFactory.GetStorage(TestConstants.UriGHCN);
            GHCNDataHandler handler = new GHCNDataHandler(storage);

            TimeRegion tr = new TimeRegion(firstYear: 1914, lastYear: 1914).GetMonthlyTimeseries(firstMonth: 12, lastMonth: 12);//data index 2567
            FetchDomain domain = FetchDomain.CreatePoints(
                new double[] { -25.18 }, //exact station. data index 18398
                new double[] { 151.65 },
                 tr);

            FetchRequest prateRequest = new FetchRequest("prate", domain);

            var compCont = new ComputationalContext();

            var reqContext = RequestContextStub.GetStub(storage, prateRequest);

            Assert.AreEqual(0.0, (double)handler.EvaluateAsync(reqContext, compCont).Result.GetValue(0), 1e-6); //manual data comparison.
        }

        /// <summary>
        /// Manual comparison
        /// </summary>
        [TestMethod]
        [TestCategory("Local")]
        public void GhcnTempValuesTest()
        {
            System.Diagnostics.Trace.WriteLine(TestConstants.UriGHCN);
            string etopoLocalUri = TestConstants.UriEtopo;

            var ghcnStorage = TestDataStorageFactory.GetStorage(TestConstants.UriGHCN);
            var etopoStorage = TestDataStorageFactory.GetStorage(etopoLocalUri);
            GHCNDataHandler handler = new GHCNDataHandler(ghcnStorage);
            ETOPO1DataSource.ETOPO1DataHandler elevationHandler = new ETOPO1DataSource.ETOPO1DataHandler(etopoStorage);

            TimeRegion tr = new TimeRegion(firstYear: 1921, lastYear: 1921).GetMonthlyTimeseries(firstMonth: 3, lastMonth: 3);//data index 2642
            FetchDomain domain = FetchDomain.CreatePoints(
                new double[] { 36.27 }, //exact station. data index 3776
                new double[] { -90.97 },
                 tr);

            FetchRequest tempRequest = new FetchRequest("temp", domain);

            Func<FetchRequest, Array> elevHandling = req =>
                {
                    var rewrittenReq = new FetchRequest("Elevation", req.Domain);
                    return elevationHandler.AggregateAsync(RequestContextStub.GetStub(etopoStorage, rewrittenReq), null).Result;
                };

            var reqContext = RequestContextStub.GetStub(ghcnStorage, tempRequest, elevHandling);
            var compCont = new ComputationalContext();
            var evRes = handler.EvaluateAsync(reqContext, compCont).Result;

            Assert.AreEqual(15.6, (double)handler.AggregateAsync(reqContext, compCont).Result.GetValue(0), 1e-5); //manual data comparison.
        }

        /// <summary>
        /// Manual comparison
        /// </summary>
        [TestMethod]
        [TestCategory("Local")]
        public void GhcnTempUncertaintyTest()
        {
            string etopoLocalUri = TestConstants.UriEtopo;

            var ghcnStorage = TestDataStorageFactory.GetStorage(TestConstants.UriGHCN);
            var etopoStorage = TestDataStorageFactory.GetStorage(etopoLocalUri);
            GHCNDataHandler handler = new GHCNDataHandler(ghcnStorage);
            ETOPO1DataSource.ETOPO1DataHandler elevationHandler = new ETOPO1DataSource.ETOPO1DataHandler(etopoStorage);

            TimeRegion tr = new TimeRegion(firstYear: 1921, lastYear: 1921).GetMonthlyTimeseries(firstMonth: 3, lastMonth: 3);//data index 2642
            FetchDomain domain = FetchDomain.CreatePoints(
                new double[] { 36.27 }, //exact station. data index 3776
                new double[] { -90.97 },
                 tr);

            FetchRequest tempRequest = new FetchRequest("temp", domain);

            Func<FetchRequest, Array> elevHandling = req =>
            {
                var rewrittenReq = new FetchRequest("Elevation", req.Domain);
                return elevationHandler.AggregateAsync(RequestContextStub.GetStub(etopoStorage, rewrittenReq), null).Result;
            };

            var reqContext = RequestContextStub.GetStub(ghcnStorage, tempRequest, elevHandling);
            var compCont = new ComputationalContext();
            Assert.AreEqual(0.0, (double)handler.EvaluateAsync(reqContext, compCont).Result.GetValue(0), 1e-6); //manual data comparison.
        }

        /// <summary>
        /// Test FetchTestairtFromGHCN fails with -85 degC
        /// </summary>
        [TestMethod]
        [TestCategory("Local")]
        public void Bug1691()
        {
            double BayOfBiscaySELat = 44.5;
            double BayOfBiscaySELon = -3.5;

            double InFranceLat = 47;
            double InFranceLon = 1;

            Random r = new Random(1);
            var eps = r.NextDouble() / 10.0;
            double latDelta = InFranceLat - BayOfBiscaySELat;
            double lonDelta = InFranceLon - BayOfBiscaySELon;
            var tr = new TimeRegion(1990, 2001, 1, -1, 0, 24, true, false, true);

            var request = new FetchRequest(
                "temp",
                FetchDomain.CreateCellGrid(
                Enumerable.Range(0, 31).Select(i => eps + BayOfBiscaySELat + i * latDelta / 31.0).ToArray(),
                Enumerable.Range(0, 21).Select(i => eps + BayOfBiscaySELon + i * lonDelta / 21.0).ToArray(),
                tr));


            string etopoLocalUri = TestConstants.UriEtopo;

            var ghcnStorage = TestDataStorageFactory.GetStorage(TestConstants.UriGHCN);
            var etopoStorage = TestDataStorageFactory.GetStorage(etopoLocalUri);
            GHCNDataHandler handler = new GHCNDataHandler(ghcnStorage);
            ETOPO1DataSource.ETOPO1DataHandler elevationHandler = new ETOPO1DataSource.ETOPO1DataHandler(etopoStorage);

            Func<FetchRequest, Array> elevHandling = req =>
            {
                var rewrittenReq = new FetchRequest("Elevation", req.Domain);
                return elevationHandler.AggregateAsync(RequestContextStub.GetStub(etopoStorage, rewrittenReq), null).Result;
            };

            var reqContext = RequestContextStub.GetStub(ghcnStorage, request, elevHandling);
            var compCont = new ComputationalContext();
            var evRes = handler.EvaluateAsync(reqContext, compCont).Result;


            Assert.IsTrue(-70.0 < (double)handler.AggregateAsync(reqContext, compCont).Result.GetValue(16,2,0)); //manual data comparison.            
        }
    }
}
