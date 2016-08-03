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
    class SimplePlaneStorageContext : IStorageContext, IRequestContext
    {
        DataSet data = DataSet.Open("msds:memory");
        FetchRequest request;

        double[] lats;
        double[] lons;
        double[] vals;

        public SimplePlaneStorageContext(double[] lats, double[] lons, double[] vals,FetchRequest request)
        {
            this.lats = lats;
            this.lons = lons;
            this.vals = vals;

            this.request = request;
            data.AddVariable<double>("lat", lats, "i");
            data.AddVariable<double>("lon", lons, "i");
            data.AddVariable<double>("val", vals, "i");
        }

        public DataStorageDefinition StorageDefinition
        {
            get
            {
                var definition = new DataStorageDefinition();
                definition.VariablesDimensions.Add("lat", new string[] { "i" });
                definition.VariablesDimensions.Add("lon", new string[] { "i" });
                definition.VariablesDimensions.Add("val", new string[] { "i" });

                definition.VariablesMetadata.Add("lat", new MetaDataDictionary());
                definition.VariablesMetadata.Add("lon", new MetaDataDictionary());
                definition.VariablesMetadata.Add("val", new MetaDataDictionary());

                definition.VariablesTypes.Add("lat", typeof(double));
                definition.VariablesTypes.Add("lon", typeof(double));
                definition.VariablesTypes.Add("val", typeof(double));
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
            set { request = value; }
        }

        public async Task<Array> GetMaskAsync(Array uncertainty)
        {
            return Enumerable.Repeat(true, uncertainty.Length).ToArray();
        }

        public Task<IFetchResponse[]> FetchDataAsync(params FetchRequest[] requests)
        {
            throw new NotImplementedException();
        }


        public IRequestContext CopyWithNewRequest(FetchRequest request)
        {
            throw new NotImplementedException();
        }
    }

    [TestClass]
    public class OnlySpatialTpsTests
    {
        [TestMethod]
        [TestCategory("Local")]
        public void PlaneInterpolationTest()
        {
            double[] lats = new double[] { 0.0, 5.0, 5.0 };
            double[] lons = new double[] { 0.0, 0.0, 6.0 };
            double[] vals = new double[] { 0.0, 3.0, 3.0 };


            FetchDomain fd = FetchDomain.CreatePoints(new double[] { -5.0 }, new double[] { 6.0 }, new TimeRegion());
            FetchRequest fr = new FetchRequest("val", fd);

            var storage = new SimplePlaneStorageContext(lats,lons,vals,fr);
            storage.Request = fr;
            SpatialOnlyTpsDataHandler sotdh = new SpatialOnlyTpsDataHandler(storage);


            var compContext = new ComputationalContext();
            sotdh.EvaluateAsync(storage, compContext);
            var result = (double[])sotdh.AggregateAsync(storage,compContext).Result;
            Assert.AreEqual(-3.0, result[0], TestConstants.DoublePrecision);
        }

        [TestMethod]
        [TestCategory("Local")]
        public void SparsePointsYieldNansOrPlane()
        {
            double[] lats = new double[] { 0.0, 5.0, 50.0 };
            double[] lons = new double[] { 0.0, 0.0, 0.0 };
            double[] vals = new double[] { 1.0, 1.0, 1.0 };


            FetchDomain fd = FetchDomain.CreatePoints(new double[] { 7.0 }, new double[] { 6.0 }, new TimeRegion());
            FetchRequest fr = new FetchRequest("val", fd);

            var storage = new SimplePlaneStorageContext(lats, lons, vals, fr);
            storage.Request = fr;
            SpatialOnlyTpsDataHandler sotdh = new SpatialOnlyTpsDataHandler(storage);

            ComputationalContext cc = new ComputationalContext();

            var unc = sotdh.EvaluateAsync(storage, cc).Result;

            var result = (double[])sotdh.AggregateAsync(storage, cc).Result;
            Assert.IsTrue(double.IsNaN(result[0]) || Math.Abs(1.0-result[0])<TestConstants.FloatPrecision);
        }
    }
}

