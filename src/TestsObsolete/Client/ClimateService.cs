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
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void WebService_UseSampleFetchConfiguration_CanProperlySerializeAndDeserilizeFetchConfiguration()
        {
            // Arrange
            var config1 = new Microsoft.Research.Science.FetchClimate2.Serializable.FetchConfiguration()
            {
                TimeStamp = new DateTime(2012, 11, 15, 0, 0, 0, DateTimeKind.Utc),
                DataSources = new Microsoft.Research.Science.FetchClimate2.Serializable.DataSourceDefinition[] {
                    new Microsoft.Research.Science.FetchClimate2.Serializable.DataSourceDefinition()
                    {
                         ID = 1,
                          Name=  "one",
                          Description=  "description",
                          Copyright =  "copyright",
                           ProvidedVariables =  new string [] {
                        "var1"
                    }}
                },
                EnvironmentalVariables = new Microsoft.Research.Science.FetchClimate2.Serializable.VariableDefinition[] {
                    new Microsoft.Research.Science.FetchClimate2.Serializable.VariableDefinition()
                    {
                         Name= "var2",
                          Units = "units",
                           Description =  "description"
                    }
                }
            };

            // Act
            var json = JsonConvert.SerializeObject(config1);
            var config2 = JsonConvert.DeserializeObject<Microsoft.Research.Science.FetchClimate2.Serializable.FetchConfiguration>(json).ConvertFromSerializable();

            // Assert
            Assert.IsNotNull(config2, "Error: Deserilized object is null");
            Assert.AreEqual<DateTime>(config1.TimeStamp, config2.TimeStamp);
            Assert.IsNotNull(config2.DataSources);
            Assert.AreEqual<int>(config1.DataSources.Length, config2.DataSources.Length);
            Assert.IsNotNull(config2.DataSources[0]);
            Assert.AreEqual<ushort>(config1.DataSources[0].ID, config2.DataSources[0].ID);
            Assert.AreEqual<string>(config1.DataSources[0].Name, config2.DataSources[0].Name);
            Assert.AreEqual<string>(config1.DataSources[0].Description, config2.DataSources[0].Description);
            Assert.AreEqual<string>(config1.DataSources[0].Copyright, config2.DataSources[0].Copyright);
            Assert.IsNotNull(config2.DataSources[0].ProvidedVariables);
            Assert.AreEqual<int>(config1.DataSources[0].ProvidedVariables.Length, config2.DataSources[0].ProvidedVariables.Length);
            Assert.IsNotNull(config2.DataSources[0].ProvidedVariables[0]);
            Assert.AreEqual<string>(config1.DataSources[0].ProvidedVariables[0], config2.DataSources[0].ProvidedVariables[0]);
            Assert.IsNotNull(config2.EnvironmentalVariables);
            Assert.AreEqual<int>(config1.EnvironmentalVariables.Length, config2.EnvironmentalVariables.Length);
            Assert.IsNotNull(config2.EnvironmentalVariables[0]);
            Assert.AreEqual<string>(config1.EnvironmentalVariables[0].Name, config2.EnvironmentalVariables[0].Name);
            Assert.AreEqual<string>(config1.EnvironmentalVariables[0].Units, config2.EnvironmentalVariables[0].Units);
            Assert.AreEqual<string>(config1.EnvironmentalVariables[0].Description, config2.EnvironmentalVariables[0].Description);
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void WebService_UseEmptyFetchConfiguration_CanProperlySerializeAndDeserilizeFetchConfiguration()
        {
            // Arrange
            var config1 = new FetchConfiguration(
                DateTime.MinValue,
                new DataSourceDefinition[] {
                    new DataSourceDefinition(0, "", "", "","", new string [0])
                },
                new VariableDefinition[] {
                    new VariableDefinition("", "", "")
                });

            // Act
            var json = JsonConvert.SerializeObject(config1);
            var config2 = JsonConvert.DeserializeObject<Microsoft.Research.Science.FetchClimate2.Serializable.FetchConfiguration>(json).ConvertFromSerializable();

            // Assert
            Assert.IsNotNull(config2, "Error: Deserilized object is null");
            Assert.AreEqual<DateTime>(config1.TimeStamp, config2.TimeStamp);
            Assert.IsNotNull(config2.DataSources);
            Assert.AreEqual<int>(config1.DataSources.Length, config2.DataSources.Length);
            Assert.IsNotNull(config2.DataSources[0]);
            Assert.AreEqual<ushort>(config1.DataSources[0].ID, config2.DataSources[0].ID);
            Assert.AreEqual<string>(config1.DataSources[0].Name, config2.DataSources[0].Name);
            Assert.AreEqual<string>(config1.DataSources[0].Description, config2.DataSources[0].Description);
            Assert.AreEqual<string>(config1.DataSources[0].Copyright, config2.DataSources[0].Copyright);
            Assert.IsNotNull(config2.DataSources[0].ProvidedVariables);
            Assert.AreEqual<int>(config1.DataSources[0].ProvidedVariables.Length, config2.DataSources[0].ProvidedVariables.Length);
            Assert.IsNotNull(config2.EnvironmentalVariables);
            Assert.AreEqual<int>(config1.EnvironmentalVariables.Length, config2.EnvironmentalVariables.Length);
            Assert.IsNotNull(config2.EnvironmentalVariables[0]);
            Assert.AreEqual<string>(config1.EnvironmentalVariables[0].Name, config2.EnvironmentalVariables[0].Name);
            Assert.AreEqual<string>(config1.EnvironmentalVariables[0].Units, config2.EnvironmentalVariables[0].Units);
            Assert.AreEqual<string>(config1.EnvironmentalVariables[0].Description, config2.EnvironmentalVariables[0].Description);
        }


        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void WebService_UseSampleFetchRequest_CanProperlySerializeAndDeSerializeFetchRequest()
        {
            // Arrange
            var request1 = new Microsoft.Research.Science.FetchClimate2.Serializable.FetchRequest(new FetchRequest(
                "evn_var_name",
                FetchDomain.CreateCells(
                    new double[] { 1.0, 3.4 },
                    new double[] { 2.0, 4.5 },
                    new double[] { 1.0, 3.4 },
                    new double[] { 2.0, 4.5 },
                    new TimeRegion()
                    )                 
                ,new DateTime(2012, 11, 15, 0, 0, 0, DateTimeKind.Utc))
            );

            // Act
            var json = JsonConvert.SerializeObject(request1);
            var request2 = JsonConvert.DeserializeObject<Microsoft.Research.Science.FetchClimate2.Serializable.FetchRequest>(json).ConvertFromSerializable();

            // Assert
            Assert.IsNotNull(request2, "Error: Deserilized object is null");
            Assert.AreEqual<string>(request1.EnvironmentVariableName, request2.EnvironmentVariableName);
            Assert.IsNotNull(request2.Domain);
            Assert.IsNotNull(request2.Domain.Lats);
            Assert.AreEqual<int>(request1.Domain.Lats.Length, request2.Domain.Lats.Length);
            Assert.IsNotNull(request2.Domain.Lats[0]);
            Assert.AreEqual<double>(request1.Domain.Lats[0], request2.Domain.Lats[0]);
            Assert.IsNotNull(request2.Domain.Lons);
            Assert.AreEqual<int>(request1.Domain.Lons.Length, request2.Domain.Lons.Length);
            Assert.IsNotNull(request2.Domain.Lons[0]);
            Assert.AreEqual<double>(request1.Domain.Lons[0], request2.Domain.Lons[0]);
            Assert.IsNotNull(request2.Domain.Lats2);
            Assert.AreEqual<int>(request1.Domain.Lats2.Length, request2.Domain.Lats2.Length);
            Assert.IsNotNull(request2.Domain.Lats2[0]);
            Assert.AreEqual<double>(request1.Domain.Lats2[0], request2.Domain.Lats2[0]);
            Assert.IsNotNull(request2.Domain.Lons2);
            Assert.AreEqual<int>(request1.Domain.Lons2.Length, request2.Domain.Lons2.Length);
            Assert.IsNotNull(request2.Domain.Lons2[0]);
            Assert.AreEqual<double>(request1.Domain.Lons2[0], request2.Domain.Lons2[0]);
            Assert.IsNotNull(request2.Domain.TimeRegion);
            Assert.AreEqual<int>(request1.Domain.TimeRegion.Years[0], request2.Domain.TimeRegion.Years[0]);
            Assert.AreEqual<int>(request1.Domain.TimeRegion.Years[1], request2.Domain.TimeRegion.Years[1]);
            Assert.AreEqual<int>(request1.Domain.TimeRegion.Days[0], request2.Domain.TimeRegion.Days[0]);
            Assert.AreEqual<int>(request1.Domain.TimeRegion.Days[1], request2.Domain.TimeRegion.Days[1]);
            Assert.AreEqual<int>(request1.Domain.TimeRegion.Hours[0], request2.Domain.TimeRegion.Hours[0]);
            Assert.AreEqual<int>(request1.Domain.TimeRegion.Hours[1], request2.Domain.TimeRegion.Hours[1]);
            Assert.AreEqual<SpatialRegionSpecification>(SpatialRegionSpecification.Cells, request2.Domain.SpatialRegionType);
            Assert.AreEqual<DateTime>(request1.ReproducibilityTimestamp, request2.ReproducibilityTimestamp);
        }

        
    }
}
