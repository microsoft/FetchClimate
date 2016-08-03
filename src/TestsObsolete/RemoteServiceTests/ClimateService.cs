using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Research.Science.FetchClimate2;
using Newtonsoft.Json;
using Microsoft.Research.Science.FetchClimate2.Tests;
using System.IO;
using Microsoft.Research.Science.Data;

namespace FetchClimate2.Tests.Client
{
    [TestClass]
    public class ClimateServiceTests
    {
        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void CacheTest()
        {
            if (File.Exists("cache.csv"))
                File.Delete("cache.csv");

            var result = ClimateService.FetchAsync(new FetchRequest("airt",
                FetchDomain.CreatePoints(new double[] { 57 }, new double[] { 0 }, new TimeRegion())), null, "cache.csv").Result;
            Assert.IsTrue(result.Variables.Contains("values"));

            try
            {
                ClimateService.ServiceUrl = "http://notexistentfetchclimateservice.localhost";
                var result2 = ClimateService.FetchAsync(new FetchRequest("airt",
                    FetchDomain.CreatePoints(new double[] { 57 }, new double[] { 0 }, new TimeRegion())), null, "cache.csv").Result;
                Assert.IsTrue(result2.Variables.Contains("values"));

                try
                {
                    var result3 = ClimateService.FetchAsync(new FetchRequest("airt",
                        FetchDomain.CreatePoints(new double[] { 57 }, new double[] { 0 }, new TimeRegion()))).Result;
                    Assert.Fail();
                }
                catch
                {
                    // It is OK to be here
                }
            }
            finally
            {
                ClimateService.ServiceUrl = "http://fetchclimate2.cloudapp.net";
            }
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void WebService_RequestFetchConfiguration_CanProperlyDeserilizeJsonResult()
        {
            // Arrange
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(TestConstants.CloudServiceURI);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json")); // Add an accept header for JSON format

            HttpResponseMessage response = null;
            FetchConfiguration config = null;

            // Act
            response = client.GetAsync("api/configuration").Result;  // Blocking call
            if (response.IsSuccessStatusCode)
            {
                // Parse the response body. Blocking.
                config = response.Content.ReadAsAsync<Microsoft.Research.Science.FetchClimate2.Serializable.FetchConfiguration>().Result.ConvertFromSerializable();
            }

            // Assert
            Assert.IsTrue(response.IsSuccessStatusCode, "Error: (" + response.StatusCode + ") " + response.ReasonPhrase);
            Assert.IsNotNull(config, "Error: Deserilized object is null");
        }
        
    }
}
