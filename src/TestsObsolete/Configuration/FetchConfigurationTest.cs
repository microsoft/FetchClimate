using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Diagnostics;

namespace Microsoft.Research.Science.FetchClimate2.Tests.Configuration
{
    [TestClass]
    public class FetchConfigurationTest
    {
        private static int time = 100;
        private static string connectionString = SharedConstants.TestsConfigurationConnectionString;

        private FetchConfigurationDataClassesDataContext db = new FetchConfigurationDataClassesDataContext(connectionString);

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("Requires local DB deployed")]
        public void ConfigurationScenario()
        {

            //using (FetchConfigurationDataClassesDataContext db = new FetchConfigurationDataClassesDataContext(connectionString))
            //{
            //Mapping table uses DataSourceId & Timestamp pair like primary key. 
            //SQL server makes more than one insertion per 1 millisecond
            //Sleep required between SetMapping operation

            DateTime TIME_BEFORE_EVERYTHING = DateTime.UtcNow;
            DateTime TIME_BEFORE_WC_DISABLED;
            DateTime TIME_AFTER_WC_DISABLED;

            Thread.Sleep(time);

            db.TruncateTables();
            db.AddVariable("airt", "Air temperature near surface", "Degrees C");
            db.AddVariable("airt_land", "Air temperature near surface (land only area)", "Degrees C");
            db.AddVariable("prate", "Precipitation rate", "mm/month");
            db.AddVariable("relhum_land", "Relative humidity (land only area)", "percentage");

            db.AddDataSource(
                "NCEP/NCAR Reanalysis 1 (regular grid)",
                "The NCEP/NCAR Reanalysis 1 project is using a state-of-the-art analysis/forecast system to perform data assimilation using past data from 1948 to the present",
                "NCEP Reanalysis data provided by the NOAA/OAR/ESRL PSD, Boulder, Colorado, USA, from their Web site at http://www.esrl.noaa.gov/psd/",
                "Microsoft.Research.Science.FetchClimate2.DataSources.NCEPReanalysisRegularGridDataSource, NCEPReanalysisDataSource",
                "msds:az?name=ReanalysisRegular&DefaultEndpointsProtocol=http&AccountName=fetch&AccountKey=1Y0EOrnCX6ULY8c3iMHg9rrul2BWbPHKsHUceZ7SSh+ShM/q9K0ml49gQm+PE7G7i7zCvrpuT",
                null,null);

            db.SetMapping("NCEP/NCAR Reanalysis 1 (regular grid)", "airt", "air", true, true);

            db.AddDataSource(
                "NCEP/NCAR Reanalysis 1 Gauss T62 grid)",
                "The NCEP/NCAR Reanalysis 1 project is using a state-of-the-art analysis/forecast system to perform data assimilation using past data from 1948 to the present",
                "NCEP Reanalysis data provided by the NOAA/OAR/ESRL PSD, Boulder, Colorado, USA, from their Web site at http://www.esrl.noaa.gov/psd/", "Microsoft.Research.Science.FetchClimate2.DataSources.NCEPReanalysisGaussGridDataSource, NCEPReanalysisDataSource",
                "msds:az?name=ReanalysisGaussT62&DefaultEndpointsProtocol=http&AccountName=fetch&AccountKey=1Y0EOrnCX6ULY8c3iMHg9rrul2BWbPHKsHUceZ7SSh+ShM/q9K0ml49gQm+PE7G7i7zCvrpuT",
                null,null);
            db.SetMapping("NCEP/NCAR Reanalysis 1 Gauss T62 grid)", "prate", "prate", true, true);


            db.AddDataSource(
                "WorldClim 1.4",
                "A set of global climate layers (climate grids) with a spatial resolution of a square kilometer",
                "The database is documented in this article: Hijmans, R.J., S.E. Cameron, J.L. Parra, P.G. Jones and A. Jarvis, 2005. Very high resolution interpolated climate surfaces for global land areas. International Journal of Climatology 25: 1965-1978.", "Microsoft.Research.Science.FetchClimate2.DataSources.CRUProcessor, NCEPReanalysisDataSource",
                "msds:az?name=CRU_CL_2_0&DefaultEndpointsProtocol=http&AccountName=fetch&AccountKey=1Y0EOrnCX6ULY8c3iMHg9rrul2BWbPHKsHUceZ7SSh+ShM/q9K0ml49gQm+PE7G7i7zCvrpuT",
                null,null);

            db.SetMapping("WorldClim 1.4", "airt", "tmean", true, true);            
            db.SetMapping("WorldClim 1.4", "airt_land", "tmean_land", true, true);            
            db.SetMapping("WorldClim 1.4", "prate", "prec", true, true);

            db.AddDataSource(
                    "CRU CL 2.0",
                    "High-resolution grid of the average climate in the recent past.",
                    "Produced by Climatic Research Unit (University of East Anglia). http://www.cru.uea.ac.uk",
                    "Microsoft.Research.Science.FetchClimate2.DataSources.CRUProcessor, NCEPReanalysisDataSource", "msds:az?name=CRU_CL_2_0&DefaultEndpointsProtocol=http&AccountName=fetch&AccountKey=1Y0EOrnCX6ULY8c3iMHg9rrul2BWbPHKsHUceZ7SSh+ShM/q9K0ml49gQm+PE7G7i7zCvrpuT",
                    null,null);


            db.SetMapping("CRU CL 2.0", "airt", "tmp", true, true);
            db.SetMapping("CRU CL 2.0", "airt_land", "tmp", true, true);
            db.SetMapping("CRU CL 2.0", "prate", "pre", true, true);
            db.SetMapping("CRU CL 2.0", "relhum_land", "reh_land", true, true);
            
            Thread.Sleep(time);
            TIME_BEFORE_WC_DISABLED = (DateTime)db.GetLatestTimeStamp().First().TimeStamp;

            db.SetMapping("WorldClim 1.4", "airt", "tmean", true,false);
            db.SetMapping("WorldClim 1.4", "airt_land", "tmean_land", true, false);
            db.SetMapping("WorldClim 1.4", "prate", "prec", true, false);

            Thread.Sleep(time);
            TIME_AFTER_WC_DISABLED = (DateTime)db.GetLatestTimeStamp().First().TimeStamp;

            //must contain nothing
            var Sources = db.GetDataSources(TIME_BEFORE_EVERYTHING).ToArray();
            Assert.AreEqual(0, Sources.Length);
            
            String[] Expected = new String[] 
                {
                    "NCEP/NCAR Reanalysis 1 (regular grid)",
                    "NCEP/NCAR Reanalysis 1 Gauss T62 grid)",
                    "WorldClim 1.4",
                    "CRU CL 2.0"
                };

            Thread.Sleep(time);

            //must contain datasources with mappings and without mappings
            Sources = db.GetDataSources(TIME_BEFORE_WC_DISABLED).ToArray();
            Assert.AreEqual(Expected.Length, Sources.Length);

            foreach (var item in Sources)
            {
                Assert.IsTrue(Expected.Contains(item.Name));                
            }            

            Thread.Sleep(time);

            //must contain datasources with mappings and without mappings
            //the same as previous, as mappings don't affect the procedure
            Sources = db.GetDataSources(TIME_AFTER_WC_DISABLED).ToArray();
            Assert.AreEqual(Expected.Length, Sources.Length);
            foreach (var item in Sources)
            {
                Assert.IsTrue(Expected.Contains(item.Name));                
            }


            Expected = new String[]
                {
                    "NCEP/NCAR Reanalysis 1 (regular grid)",
                    "WorldClim 1.4",
                    "CRU CL 2.0"
                };

            Thread.Sleep(time);

            //mapping affects the following call
            var SourcesForVariable = db.GetDataSourcesForVariable(TIME_BEFORE_WC_DISABLED, "airt").ToArray();
            Assert.AreEqual(Expected.Length, SourcesForVariable.Length);
            foreach (var item in SourcesForVariable)
            {                
                Assert.IsTrue(Expected.Contains(item.Name));
            }

            Expected = new String[]
                {
                    "NCEP/NCAR Reanalysis 1 (regular grid)",
                    "CRU CL 2.0"
                };

            Thread.Sleep(time);
            
            //mapping affects the following call
            SourcesForVariable = db.GetDataSourcesForVariable(TIME_AFTER_WC_DISABLED, "airt").ToArray();
            Assert.AreEqual(Expected.Length, SourcesForVariable.Length);
            foreach (var item in SourcesForVariable)
            {                
                Assert.IsTrue(Expected.Contains(item.Name));
            }

            Thread.Sleep(time);

            //must contain nothing
            SourcesForVariable = db.GetDataSourcesForVariable(TIME_BEFORE_EVERYTHING, "relhum_land").ToArray();
            Assert.AreEqual(0, SourcesForVariable.Length);


            Expected = new String[]
                {
                    "CRU CL 2.0"
                };

            Thread.Sleep(time);

            SourcesForVariable = db.GetDataSourcesForVariable(TIME_AFTER_WC_DISABLED, "relhum_land").ToArray();
            Assert.AreEqual(Expected.Length, SourcesForVariable.Length);
            foreach (var item in SourcesForVariable)
            {                
                Assert.IsTrue(Expected.Contains(item.Name));
            }

            Expected = new String[]
                {
                    "CRU CL 2.0"
                };

            Thread.Sleep(time);

            SourcesForVariable = db.GetDataSourcesForVariable(TIME_BEFORE_WC_DISABLED, "relhum_land").ToArray();
            Assert.AreEqual(Expected.Length, SourcesForVariable.Length);
            foreach (var item in SourcesForVariable)
            {                
                Assert.IsTrue(Expected.Contains(item.Name));
            }

            
            Expected = new String[]
                {
                    "airt",
                    "prate",
                    "airt_land",
                    "relhum_land"
                };

            Thread.Sleep(time);

            var EnvVariables = db.GetEnvVariables(/*TIME_BEFORE_WC_DISABLED*/).ToArray();
            Assert.AreEqual(Expected.Length, EnvVariables.Length);
            foreach (var item in EnvVariables)
            {                
                Assert.IsTrue(Expected.Contains(item.DisplayName));

            }

            //EnvVariables = db.GetEnvVariables(TIME_AFTER_WC_DISABLED).ToArray();
            //Assert.AreEqual(Expected.Length, EnvVariables.Length);
            //foreach (var item in EnvVariables)
            //{
            //    //Debug.WriteLine(String.Format("{0} {1}", item.ID, item.Name));
            //    Assert.IsTrue(Expected.Contains(item.DisplayName));
            //}

            db.TruncateTables();
            //}
        }

        /// <summary>Tests connection to local database. 
        /// Database should be empty and zero timestamp is returned</summary>
        [TestMethod]
        [TestCategory("Local")]
        public void LocalConnectionTest()
        {
            var dc = new Microsoft.Research.Science.FetchClimate2.FetchConfigurationDataClassesDataContext(connectionString);            
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("Requires local DB deployed")]
        public void SetFetchEngineProcedureTest()
        {
            string[] Expected;

            db.TruncateTables();
            DateTime TIME_BEFORE_INSERTING = DateTime.UtcNow;
            Thread.Sleep(time);
            db.SetFetchEngine("Fetch1");
            Thread.Sleep(time);
            DateTime TIME_MIDDLE_INSERTING = DateTime.UtcNow;
            Thread.Sleep(time);
            db.SetFetchEngine("Fetch2");
            Thread.Sleep(time);
            DateTime TIME_AFTER_INSERTING = DateTime.UtcNow;
            Thread.Sleep(time);


            //STEP 1
            var Sources = db.GetFetchEngine(TIME_AFTER_INSERTING).ToArray();

            Expected = new string[]
            {
                "Fetch1",
                "Fetch2"
            };

            Assert.AreEqual(1, Sources.Length);
            foreach (var item in Sources)
            {
                Assert.IsTrue(Expected.Contains(item.FullClrTypeName));
            }

            //STEP 2

            Sources = db.GetFetchEngine(TIME_MIDDLE_INSERTING).ToArray();

            Expected = new string[]
            {
                "Fetch1"
            };

            Assert.AreEqual(1, Sources.Length);
            foreach (var item in Sources)
            {
                Assert.IsTrue(Expected.Contains(item.FullClrTypeName));
            }

            //STEP 3

            Sources = db.GetFetchEngine(TIME_BEFORE_INSERTING).ToArray();

            Expected = new string[] { };

            Assert.AreEqual(0, Sources.Length);

        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("Requires local DB deployed")]
        public void SetDataSourceUriProcedureTest()
        {         
            db.TruncateTables();

            db.AddDataSource(
                "NCEP/NCAR Reanalysis 1 (regular grid)",
                "The NCEP/NCAR Reanalysis 1 project is using a state-of-the-art analysis/forecast system to perform data assimilation using past data from 1948 to the present",
                "NCEP Reanalysis data provided by the NOAA/OAR/ESRL PSD, Boulder, Colorado, USA, from their Web site at http://www.esrl.noaa.gov/psd/",
                "Microsoft.Research.Science.FetchClimate2.DataSources.NCEPReanalysisGaussGridDataSource, NCEPReanalysisDataSource",
                "123",null,null);

            Thread.Sleep(time);            
            DateTime TIME_AFTER_INSERTING = DateTime.UtcNow;
            Thread.Sleep(time);
            db.SetDataSourceUri("NCEP/NCAR Reanalysis 1 (regular grid)", "TestUri");            
            Thread.Sleep(time);
            DateTime TIME_AFTER_URI_SET = DateTime.UtcNow;

            var Sources = db.GetDataSources(TIME_AFTER_INSERTING).ToArray();
            GetDataSourcesResult ExpectedSource = new GetDataSourcesResult()
            {
                Name = "NCEP/NCAR Reanalysis 1 (regular grid)",
                Description = "The NCEP/NCAR Reanalysis 1 project is using a state-of-the-art analysis/forecast system to perform data assimilation using past data from 1948 to the present",
                Copyright = "NCEP Reanalysis data provided by the NOAA/OAR/ESRL PSD, Boulder, Colorado, USA, from their Web site at http://www.esrl.noaa.gov/psd/",
                FullClrTypeName = "Microsoft.Research.Science.FetchClimate2.DataSources.NCEPReanalysisGaussGridDataSource, NCEPReanalysisDataSource",
                Uri = "123"
            };

            Assert.AreEqual(ExpectedSource.Name, Sources[0].Name);
            Assert.AreEqual(ExpectedSource.Copyright, Sources[0].Copyright);
            Assert.AreEqual(ExpectedSource.Description, Sources[0].Description);
            Assert.AreEqual(ExpectedSource.FullClrTypeName, Sources[0].FullClrTypeName);
            Assert.AreEqual(ExpectedSource.Uri, Sources[0].Uri);


            Sources = db.GetDataSources(TIME_AFTER_URI_SET).ToArray();
            ExpectedSource = new GetDataSourcesResult()
            {
                Name = "NCEP/NCAR Reanalysis 1 (regular grid)",
                Description = "The NCEP/NCAR Reanalysis 1 project is using a state-of-the-art analysis/forecast system to perform data assimilation using past data from 1948 to the present",
                Copyright = "NCEP Reanalysis data provided by the NOAA/OAR/ESRL PSD, Boulder, Colorado, USA, from their Web site at http://www.esrl.noaa.gov/psd/",
                FullClrTypeName = "Microsoft.Research.Science.FetchClimate2.DataSources.NCEPReanalysisGaussGridDataSource, NCEPReanalysisDataSource",
                Uri = "TestUri"
            };

            Assert.AreEqual(ExpectedSource.Name, Sources[0].Name);
            Assert.AreEqual(ExpectedSource.Copyright, Sources[0].Copyright);
            Assert.AreEqual(ExpectedSource.Description, Sources[0].Description);
            Assert.AreEqual(ExpectedSource.FullClrTypeName, Sources[0].FullClrTypeName);
            Assert.AreEqual(ExpectedSource.Uri, Sources[0].Uri);
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("Requires local DB deployed")]
        public void SetDataSourceHandlerProcedureTest()
        {
            db.TruncateTables();

            db.AddDataSource(
                "NCEP/NCAR Reanalysis 1 (regular grid)",
                "The NCEP/NCAR Reanalysis 1 project is using a state-of-the-art analysis/forecast system to perform data assimilation using past data from 1948 to the present",
                "NCEP Reanalysis data provided by the NOAA/OAR/ESRL PSD, Boulder, Colorado, USA, from their Web site at http://www.esrl.noaa.gov/psd/",
                "Microsoft.Research.Science.FetchClimate2.DataSources.NCEPReanalysisGaussGridDataSource, NCEPReanalysisDataSource",
                "123",null,null);

            Thread.Sleep(time);
            DateTime TIME_AFTER_INSERTING = DateTime.UtcNow;
            Thread.Sleep(time);
            db.SetDataSourceProcessor("NCEP/NCAR Reanalysis 1 (regular grid)", "testHandler",null,null);
            Thread.Sleep(time);
            DateTime TIME_AFTER_HANDLER_SET = DateTime.UtcNow;

            var Sources = db.GetDataSources(TIME_AFTER_INSERTING).ToArray();
            GetDataSourcesResult ExpectedSource = new GetDataSourcesResult()
            {
                Name = "NCEP/NCAR Reanalysis 1 (regular grid)",
                Description = "The NCEP/NCAR Reanalysis 1 project is using a state-of-the-art analysis/forecast system to perform data assimilation using past data from 1948 to the present",
                Copyright = "NCEP Reanalysis data provided by the NOAA/OAR/ESRL PSD, Boulder, Colorado, USA, from their Web site at http://www.esrl.noaa.gov/psd/",
                FullClrTypeName = "Microsoft.Research.Science.FetchClimate2.DataSources.NCEPReanalysisGaussGridDataSource, NCEPReanalysisDataSource",
                Uri = "123"
            };

            Assert.AreEqual(ExpectedSource.Name, Sources[0].Name);
            Assert.AreEqual(ExpectedSource.Copyright, Sources[0].Copyright);
            Assert.AreEqual(ExpectedSource.Description, Sources[0].Description);
            Assert.AreEqual(ExpectedSource.FullClrTypeName, Sources[0].FullClrTypeName);
            Assert.AreEqual(ExpectedSource.Uri, Sources[0].Uri);
            


            Sources = db.GetDataSources(TIME_AFTER_HANDLER_SET).ToArray();
            ExpectedSource = new GetDataSourcesResult()
            {
                Name = "NCEP/NCAR Reanalysis 1 (regular grid)",
                Description = "The NCEP/NCAR Reanalysis 1 project is using a state-of-the-art analysis/forecast system to perform data assimilation using past data from 1948 to the present",
                Copyright = "NCEP Reanalysis data provided by the NOAA/OAR/ESRL PSD, Boulder, Colorado, USA, from their Web site at http://www.esrl.noaa.gov/psd/",
                FullClrTypeName = "testHandler",
                Uri = "123"
            };

            Assert.AreEqual(ExpectedSource.Name, Sources[0].Name);
            Assert.AreEqual(ExpectedSource.Copyright, Sources[0].Copyright);
            Assert.AreEqual(ExpectedSource.Description, Sources[0].Description);
            Assert.AreEqual(ExpectedSource.FullClrTypeName, Sources[0].FullClrTypeName);
            Assert.AreEqual(ExpectedSource.Uri, Sources[0].Uri);


            //Test remote datasource updates
            db.SetDataSourceProcessor("NCEP/NCAR Reanalysis 1 (regular grid)", null,1,"TestRemoteName");
            Thread.Sleep(time);
            DateTime TIME_AFTER_REMOTE_HANDLER_SET = DateTime.UtcNow;

            ExpectedSource = new GetDataSourcesResult()
            {
                Name = "NCEP/NCAR Reanalysis 1 (regular grid)",
                Description = "The NCEP/NCAR Reanalysis 1 project is using a state-of-the-art analysis/forecast system to perform data assimilation using past data from 1948 to the present",
                Copyright = "NCEP Reanalysis data provided by the NOAA/OAR/ESRL PSD, Boulder, Colorado, USA, from their Web site at http://www.esrl.noaa.gov/psd/",
                FullClrTypeName = null,
                Uri = "123",
                RemoteID = 1,
                RemoteName = "TestRemoteName"
            };

            Sources = db.GetDataSources(TIME_AFTER_REMOTE_HANDLER_SET).ToArray();

            Assert.AreEqual(ExpectedSource.Name, Sources[0].Name);
            Assert.AreEqual(ExpectedSource.Copyright, Sources[0].Copyright);
            Assert.AreEqual(ExpectedSource.Description, Sources[0].Description);
            Assert.AreEqual(ExpectedSource.FullClrTypeName, Sources[0].FullClrTypeName);
            Assert.AreEqual(ExpectedSource.RemoteName, Sources[0].RemoteName);
            Assert.AreEqual(ExpectedSource.RemoteID, Sources[0].RemoteID);
            Assert.AreEqual(ExpectedSource.Uri, Sources[0].Uri);
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("Requires local DB deployed")]
        public void AddDataSourceProcedureTest()
        {
            string[] Expected;

            db.TruncateTables();

            DateTime TIME_BEFORE_INSERTING = DateTime.UtcNow;
            db.AddDataSource(
                "NCEP/NCAR Reanalysis 1 (regular grid)",
                "The NCEP/NCAR Reanalysis 1 project is using a state-of-the-art analysis/forecast system to perform data assimilation using past data from 1948 to the present",
                "NCEP Reanalysis data provided by the NOAA/OAR/ESRL PSD, Boulder, Colorado, USA, from their Web site at http://www.esrl.noaa.gov/psd/",
                "Microsoft.Research.Science.FetchClimate2.DataSources.NCEPReanalysisRegularGridDataSource, NCEPReanalysisDataSource",
                "msds:az?name=ReanalysisRegular&DefaultEndpointsProtocol=http&AccountName=fetch&AccountKey=1Y0EOrnCX6ULY8c3iMHg9rrul2BWbPHKsHUceZ7SSh+ShM/q9K0ml49gQm+PE7G7i7zCvrpuT",
                null,null);

            Thread.Sleep(time);
            DateTime TIME_MIDDLE_INSERTING = DateTime.UtcNow;
            db.AddDataSource(
                "NCEP/NCAR Reanalysis 1 Gauss T62 grid)",
                "The NCEP/NCAR Reanalysis 1 project is using a state-of-the-art analysis/forecast system to perform data assimilation using past data from 1948 to the present",
                "NCEP Reanalysis data provided by the NOAA/OAR/ESRL PSD, Boulder, Colorado, USA, from their Web site at http://www.esrl.noaa.gov/psd/",
                "Microsoft.Research.Science.FetchClimate2.DataSources.NCEPReanalysisGaussGridDataSource, NCEPReanalysisDataSource",
                "msds:az?name=ReanalysisGaussT62&DefaultEndpointsProtocol=http&AccountName=fetch&AccountKey=1Y0EOrnCX6ULY8c3iMHg9rrul2BWbPHKsHUceZ7SSh+ShM/q9K0ml49gQm+PE7G7i7zCvrpuT",
                null,null);

            DateTime TIME_AFTER_INSERTING = DateTime.UtcNow;

            //STEP 1

            var Sources = db.GetDataSources(TIME_AFTER_INSERTING).ToArray();
            Expected = new string[]
            {
                "NCEP/NCAR Reanalysis 1 (regular grid)",
                "NCEP/NCAR Reanalysis 1 Gauss T62 grid)"
            };

            Assert.AreEqual(Expected.Length, Sources.Length);
            foreach (var item in Sources)
            {
                Assert.IsTrue(Expected.Contains(item.Name));
            }            
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("Requires local DB deployed")]
        public void VariableTest()
        {
            db.TruncateTables();

            db.AddDataSource(
                "WorldClim 1.4",
                "A set of global climate layers (climate grids) with a spatial resolution of a square kilometer",
                "The database is documented in this article: Hijmans, R.J., S.E. Cameron, J.L. Parra, P.G. Jones and A. Jarvis, 2005. Very high resolution interpolated climate surfaces for global land areas. International Journal of Climatology 25: 1965-1978.", "Microsoft.Research.Science.FetchClimate2.DataSources.CRUProcessor, NCEPReanalysisDataSource",
                "msds:az?name=CRU_CL_2_0&DefaultEndpointsProtocol=http&AccountName=fetch&AccountKey=1Y0EOrnCX6ULY8c3iMHg9rrul2BWbPHKsHUceZ7SSh+ShM/q9K0ml49gQm+PE7G7i7zCvrpuT",
                null,null);
            db.AddVariable("MyVariable", "MyDescription", "MyUnits");
            db.AddVariable("MyVariable2", "MyDescription2", "MyUnits2");

            db.AddDataSource(
                "NCEP/NCAR Reanalysis 1 (regular grid)",
                "The NCEP/NCAR Reanalysis 1 project is using a state-of-the-art analysis/forecast system to perform data assimilation using past data from 1948 to the present",
                "NCEP Reanalysis data provided by the NOAA/OAR/ESRL PSD, Boulder, Colorado, USA, from their Web site at http://www.esrl.noaa.gov/psd/",
                "Microsoft.Research.Science.FetchClimate2.DataSources.NCEPReanalysisRegularGridDataSource, NCEPReanalysisDataSource",
                "msds:az?name=ReanalysisRegular&DefaultEndpointsProtocol=http&AccountName=fetch&AccountKey=1Y0EOrnCX6ULY8c3iMHg9rrul2BWbPHKsHUceZ7SSh+ShM/q9K0ml49gQm+PE7G7i7zCvrpuT",
                null,null);

            //STEP 1

            DateTime TIME_BEFORE_INSERTING = DateTime.UtcNow;
            Thread.Sleep(time);
            db.SetMapping("NCEP/NCAR Reanalysis 1 (regular grid)", "MyVariable", "TestDataSourceVariable", true, true);
            DateTime TIME_MIDDLE_INSERTING = DateTime.UtcNow;
            Thread.Sleep(time);
            db.SetMapping("WorldClim 1.4", "MyVariable", "TestDataSourceVariable", true, true);
            Thread.Sleep(time);
            db.SetMapping("WorldClim 1.4", "MyVariable2", "TestDataSourceVariable2", true, true);
            Thread.Sleep(time);
            db.SetMapping("WorldClim 1.4", "MyVariable", "TestDataSourceVariable", true, false);
            Thread.Sleep(time);
            var Variables = db.GetVariablesForDataSource(DateTime.UtcNow, "WorldClim 1.4").ToArray();

            Assert.AreEqual(1, Variables.Length);
            Assert.AreEqual("MyVariable2", Variables[0].DisplayName);
            Assert.AreEqual("MyDescription2", Variables[0].Description);
            Assert.AreEqual("MyUnits2", Variables[0].Units);

            //STEP 2

            //var EnvVariables = db.GetEnvVariables(TIME_MIDDLE_INSERTING).ToArray();
            //Assert.AreEqual(1, EnvVariables.Length);

            var EnvVariables = db.GetEnvVariables(/*DateTime.Now*/).ToArray();
            Assert.AreEqual(2, EnvVariables.Length);

            //STEP 3

            var DataSources = db.GetDataSourcesForVariable(DateTime.UtcNow, "MyVariable").ToArray();
            Assert.AreEqual(1, DataSources.Length);

            //STEP 4

            var Mappings = db.GetMapping(TIME_BEFORE_INSERTING, "NCEP/NCAR Reanalysis 1 (regular grid)").ToArray();
            Assert.AreEqual(0, Mappings.Length);

            Mappings = db.GetMapping(DateTime.UtcNow, "NCEP/NCAR Reanalysis 1 (regular grid)").ToArray();
            Assert.AreEqual(1, Mappings.Length);
            db.TruncateTables();
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("Requires local DB deployed")]
        public void GetExactTimeStampTest()
        {

            db.TruncateTables();
            
            //Part 1
            db.AddDataSource(
                "NCEP/NCAR Reanalysis 1 (regular grid)",
                "The NCEP/NCAR Reanalysis 1 project is using a state-of-the-art analysis/forecast system to perform data assimilation using past data from 1948 to the present",
                "NCEP Reanalysis data provided by the NOAA/OAR/ESRL PSD, Boulder, Colorado, USA, from their Web site at http://www.esrl.noaa.gov/psd/",
                "Microsoft.Research.Science.FetchClimate2.DataSources.NCEPReanalysisRegularGridDataSource, NCEPReanalysisDataSource",
                "msds:az?name=ReanalysisRegular&DefaultEndpointsProtocol=http&AccountName=fetch&AccountKey=1Y0EOrnCX6ULY8c3iMHg9rrul2BWbPHKsHUceZ7SSh+ShM/q9K0ml49gQm+PE7G7i7zCvrpuT",
                null,null);


            Thread.Sleep(time);

            DateTime TIME_MIDDLE_INSERTING = (DateTime)db.GetLatestTimeStamp().First().TimeStamp;
            db.AddDataSource(
                "NCEP/NCAR Reanalysis 1 Gauss T62 grid)",
                "The NCEP/NCAR Reanalysis 1 project is using a state-of-the-art analysis/forecast system to perform data assimilation using past data from 1948 to the present",
                "NCEP Reanalysis data provided by the NOAA/OAR/ESRL PSD, Boulder, Colorado, USA, from their Web site at http://www.esrl.noaa.gov/psd/",
                "Microsoft.Research.Science.FetchClimate2.DataSources.NCEPReanalysisGaussGridDataSource, NCEPReanalysisDataSource",
                "msds:az?name=ReanalysisGaussT62&DefaultEndpointsProtocol=http&AccountName=fetch&AccountKey=1Y0EOrnCX6ULY8c3iMHg9rrul2BWbPHKsHUceZ7SSh+ShM/q9K0ml49gQm+PE7G7i7zCvrpuT",
                null,null);

            DateTime NOW = DateTime.UtcNow;
            var t = db.GetExactTimeStamp(NOW).ToList();
            Assert.AreEqual(t.Count,1);

            if (t[0].TimeStamp > NOW)
                Assert.Fail();

            if (t[0].TimeStamp <= TIME_MIDDLE_INSERTING)
                Assert.Fail();

            //Part 2

            DateTime TIME_BEFORE_ENGINE_INSERTING = (DateTime)db.GetLatestTimeStamp().First().TimeStamp;
            Thread.Sleep(time);
            db.SetFetchEngine("Fetch1");
            Thread.Sleep(time);

            DateTime TIME_AFTER_ENGINE_INSERTING = (DateTime)db.GetLatestTimeStamp().First().TimeStamp;
            var t2 = db.GetExactTimeStamp(TIME_BEFORE_ENGINE_INSERTING).ToList();


            Assert.AreEqual(t2.Count,1);
            Assert.AreEqual(t[0].TimeStamp,t2[0].TimeStamp);

            //Part3
            Thread.Sleep(time);
            NOW = DateTime.UtcNow;
            var t3 = db.GetExactTimeStamp(NOW).ToList();
            if (t3[0].TimeStamp > NOW)
                Assert.Fail();

            if (t3[0].TimeStamp < TIME_BEFORE_ENGINE_INSERTING)
                Assert.Fail();

            if (t3[0].TimeStamp > TIME_AFTER_ENGINE_INSERTING)
                Assert.Fail();

            //Part4

            db.AddDataSource(
                "WorldClim 1.4",
                "A set of global climate layers (climate grids) with a spatial resolution of a square kilometer",
                "The database is documented in this article: Hijmans, R.J., S.E. Cameron, J.L. Parra, P.G. Jones and A. Jarvis, 2005. Very high resolution interpolated climate surfaces for global land areas. International Journal of Climatology 25: 1965-1978.", "Microsoft.Research.Science.FetchClimate2.DataSources.CRUProcessor, NCEPReanalysisDataSource",
                "msds:az?name=CRU_CL_2_0&DefaultEndpointsProtocol=http&AccountName=fetch&AccountKey=1Y0EOrnCX6ULY8c3iMHg9rrul2BWbPHKsHUceZ7SSh+ShM/q9K0ml49gQm+PE7G7i7zCvrpuT",
                null,null);
            db.AddVariable("MyVariable", "MyDescription", "MyUnits");
            db.AddVariable("MyVariable2", "MyDescription2", "MyUnits2");


            DateTime TIME_BEFORE_MAPPING_INSERTING = (DateTime)db.GetLatestTimeStamp().First().TimeStamp;
            Thread.Sleep(time);
            db.SetMapping("NCEP/NCAR Reanalysis 1 (regular grid)", "MyVariable", "TestDataSourceVariable", true, true);
            DateTime TIME_AFTER_MAPPING_INSERTING = (DateTime)db.GetLatestTimeStamp().First().TimeStamp;

            var t4 = db.GetExactTimeStamp(DateTime.UtcNow).ToList();
            Assert.AreEqual(t4.Count, 1);
            if (t4[0].TimeStamp > TIME_AFTER_MAPPING_INSERTING)
                Assert.Fail();
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("Requires local DB deployed")]
        public void GetLatestTimeStampTest()
        {
            db.TruncateTables();

            //Part 1
            var l = db.GetLatestTimeStamp().ToList();
            Assert.AreEqual(l.Count, 1);
            Assert.AreEqual(l[0].TimeStamp, null);

            db.AddDataSource(
                "WorldClim 1.4",
                "A set of global climate layers (climate grids) with a spatial resolution of a square kilometer",
                "The database is documented in this article: Hijmans, R.J., S.E. Cameron, J.L. Parra, P.G. Jones and A. Jarvis, 2005. Very high resolution interpolated climate surfaces for global land areas. International Journal of Climatology 25: 1965-1978.", "Microsoft.Research.Science.FetchClimate2.DataSources.CRUProcessor, NCEPReanalysisDataSource",
                "msds:az?name=CRU_CL_2_0&DefaultEndpointsProtocol=http&AccountName=fetch&AccountKey=1Y0EOrnCX6ULY8c3iMHg9rrul2BWbPHKsHUceZ7SSh+ShM/q9K0ml49gQm+PE7G7i7zCvrpuT",
                null,null);

            var l2 = db.GetLatestTimeStamp().ToList();
            Assert.AreEqual(l.Count, 1);

            DateTime dt = DateTime.UtcNow;
            if (l2[0].TimeStamp > dt)
                Assert.Fail(string.Format("expected {0} is lass then {1}", l2[0].TimeStamp, dt));


            //Part 2
            Thread.Sleep(100);
            DateTime BEFORE_INSERT = DateTime.UtcNow;
            Thread.Sleep(100);
            db.SetFetchEngine("MyFetch");

            var l4 = db.GetLatestTimeStamp().ToList();
            Assert.AreEqual(l.Count, 1);

            Thread.Sleep(100);
            dt = DateTime.UtcNow;
            if (l4[0].TimeStamp > dt)
                Assert.Fail(string.Format("expected {0} is lass then {1}", l4[0].TimeStamp, dt));

            if (l4[0].TimeStamp < BEFORE_INSERT)
                Assert.Fail(string.Format("expected {0} is lass then {1}",l4[0].TimeStamp,BEFORE_INSERT));

            //Part 3
            Thread.Sleep(100);
            db.AddVariable("MyVariable", "MyDescription", "MyUnits");
            DateTime TIME_MIDDLE_INSERTING = DateTime.UtcNow;

            Thread.Sleep(100);
            BEFORE_INSERT = DateTime.UtcNow;
            Thread.Sleep(100);
            db.SetMapping("WorldClim 1.4", "MyVariable", "TestDataSourceVariable", true, true);

            var l5 = db.GetLatestTimeStamp().ToList();
            Assert.AreEqual(l.Count, 1);

            Thread.Sleep(100);
            dt = DateTime.UtcNow;
            if (l5[0].TimeStamp > dt)
                Assert.Fail(string.Format("expected {0} is more than {1}", l5[0].TimeStamp, dt));

            if (l5[0].TimeStamp < BEFORE_INSERT)
                Assert.Fail(string.Format("expected {0} is less than {1}", l5[0].TimeStamp, BEFORE_INSERT));
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("Requires local DB deployed")]
        public void GetFirstTimeStampTest()
        {
            db.TruncateTables();

            var f = db.GetFirstTimeStamp().ToList();
            Assert.AreEqual(f.Count, 1);
            Assert.AreEqual(f[0].TimeStamp, null);

            db.AddDataSource(
                "NCEP/NCAR Reanalysis 1 (regular grid)",
                "The NCEP/NCAR Reanalysis 1 project is using a state-of-the-art analysis/forecast system to perform data assimilation using past data from 1948 to the present",
                "NCEP Reanalysis data provided by the NOAA/OAR/ESRL PSD, Boulder, Colorado, USA, from their Web site at http://www.esrl.noaa.gov/psd/",
                "Microsoft.Research.Science.FetchClimate2.DataSources.NCEPReanalysisRegularGridDataSource, NCEPReanalysisDataSource",
                "msds:az?name=ReanalysisRegular&DefaultEndpointsProtocol=http&AccountName=fetch&AccountKey=1Y0EOrnCX6ULY8c3iMHg9rrul2BWbPHKsHUceZ7SSh+ShM/q9K0ml49gQm+PE7G7i7zCvrpuT",
                null,null);
            Thread.Sleep(time*10);
            DateTime TIME_AFTER_FIRST = DateTime.UtcNow;
            db.AddVariable("MyVariable", "MyDescription", "MyUnits");
            Thread.Sleep(time * 10);
            db.SetMapping("NCEP/NCAR Reanalysis 1 (regular grid)", "MyVariable", "TestDataSourceVariable", true, true);


            db.SetFetchEngine("TestEngine");

            var f2 = db.GetFirstTimeStamp().ToList();
            Assert.AreEqual(f.Count,1);

            if (f2[0].TimeStamp > TIME_AFTER_FIRST)
                Assert.Fail();
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("Requires local DB deployed")]
        public void SetEnvVariableDescription()
        {
            db.TruncateTables();
            db.AddDataSource(
                "NCEP/NCAR Reanalysis 1 (regular grid)",
                "The NCEP/NCAR Reanalysis 1 project is using a state-of-the-art analysis/forecast system to perform data assimilation using past data from 1948 to the present",
                "NCEP Reanalysis data provided by the NOAA/OAR/ESRL PSD, Boulder, Colorado, USA, from their Web site at http://www.esrl.noaa.gov/psd/",
                "Microsoft.Research.Science.FetchClimate2.DataSources.NCEPReanalysisRegularGridDataSource, NCEPReanalysisDataSource",
                "msds:az?name=ReanalysisRegular&DefaultEndpointsProtocol=http&AccountName=fetch&AccountKey=1Y0EOrnCX6ULY8c3iMHg9rrul2BWbPHKsHUceZ7SSh+ShM/q9K0ml49gQm+PE7G7i7zCvrpuT",
                null,null);
            db.AddVariable("MyVariable", "MyDescription", "MyUnits");
            Thread.Sleep(time);
            db.SetMapping("NCEP/NCAR Reanalysis 1 (regular grid)", "MyVariable", "TestDataSourceVariable", true, true);


            var variable = db.GetEnvVariables(/*DateTime.UtcNow*/).Where(x => x.DisplayName == "MyVariable").ToList();
            Assert.AreEqual(variable.Count,1);
            Assert.AreEqual(variable[0].Description, "MyDescription");


            db.SetEnvVariableDescription("MyVariable", "NewMyDescription");

            var variable2 = db.GetEnvVariables(/*DateTime.UtcNow*/).Where(v => v.DisplayName == "MyVariable").ToList();
            Assert.AreEqual(variable2.Count, 1);
            Assert.AreEqual(variable2[0].Description, "NewMyDescription");
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("Requires local DB deployed")]
        public void SetEnvVariableUnitsTest()
        {
            db.TruncateTables();
            db.AddDataSource(
                "NCEP/NCAR Reanalysis 1 (regular grid)",
                "The NCEP/NCAR Reanalysis 1 project is using a state-of-the-art analysis/forecast system to perform data assimilation using past data from 1948 to the present",
                "NCEP Reanalysis data provided by the NOAA/OAR/ESRL PSD, Boulder, Colorado, USA, from their Web site at http://www.esrl.noaa.gov/psd/",
                "Microsoft.Research.Science.FetchClimate2.DataSources.NCEPReanalysisRegularGridDataSource, NCEPReanalysisDataSource",
                "msds:az?name=ReanalysisRegular&DefaultEndpointsProtocol=http&AccountName=fetch&AccountKey=1Y0EOrnCX6ULY8c3iMHg9rrul2BWbPHKsHUceZ7SSh+ShM/q9K0ml49gQm+PE7G7i7zCvrpuT",
                null, null);
            db.AddVariable("MyVariable", "MyDescription", "MyUnits");
            Thread.Sleep(time);
            db.SetMapping("NCEP/NCAR Reanalysis 1 (regular grid)", "MyVariable", "TestDataSourceVariable", true, true);


            var variable = db.GetEnvVariables(/*DateTime.UtcNow*/).Where(x => x.DisplayName == "MyVariable").ToList();
            Assert.AreEqual(variable.Count, 1);
            Assert.AreEqual(variable[0].Units, "MyUnits");


            db.SetEnvVariableUnits("MyVariable", "NewMyUnits");

            var variable2 = db.GetEnvVariables(/*DateTime.UtcNow*/).Where(v => v.DisplayName == "MyVariable").ToList();
            Assert.AreEqual(variable2.Count, 1);
            Assert.AreEqual(variable2[0].Units, "NewMyUnits");
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("Requires local DB deployed")]
        public void SetDataSourceDescriptionTest()
        {
            db.TruncateTables();
            db.AddDataSource(
                "NCEP/NCAR Reanalysis 1 (regular grid)",
                "MyDescription",
                "NCEP Reanalysis data provided by the NOAA/OAR/ESRL PSD, Boulder, Colorado, USA, from their Web site at http://www.esrl.noaa.gov/psd/",
                "Microsoft.Research.Science.FetchClimate2.DataSources.NCEPReanalysisRegularGridDataSource, NCEPReanalysisDataSource",
                "msds:az?name=ReanalysisRegular&DefaultEndpointsProtocol=http&AccountName=fetch&AccountKey=1Y0EOrnCX6ULY8c3iMHg9rrul2BWbPHKsHUceZ7SSh+ShM/q9K0ml49gQm+PE7G7i7zCvrpuT",
                null, null);

            var d = db.GetDataSources(DateTime.UtcNow).Where(x => x.Name == "NCEP/NCAR Reanalysis 1 (regular grid)").ToList();
            Assert.AreEqual(d.Count,1);
            Assert.AreEqual(d[0].Description,"MyDescription");

            db.SetDataSourceDescription("NCEP/NCAR Reanalysis 1 (regular grid)", "NewMyDescription");
            var d2 = db.GetDataSources(DateTime.UtcNow).Where(x => x.Name == "NCEP/NCAR Reanalysis 1 (regular grid)").ToList();
            Assert.AreEqual(d2.Count, 1);
            Assert.AreEqual(d2[0].Description, "NewMyDescription");
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("Requires local DB deployed")]
        public void SetDataSourceCopyrightTest()
        {
            db.TruncateTables();
            db.AddDataSource(
                "NCEP/NCAR Reanalysis 1 (regular grid)",
                "MyDescription",
                "MyCopyright",
                "Microsoft.Research.Science.FetchClimate2.DataSources.NCEPReanalysisRegularGridDataSource, NCEPReanalysisDataSource",
                "msds:az?name=ReanalysisRegular&DefaultEndpointsProtocol=http&AccountName=fetch&AccountKey=1Y0EOrnCX6ULY8c3iMHg9rrul2BWbPHKsHUceZ7SSh+ShM/q9K0ml49gQm+PE7G7i7zCvrpuT",
                null, null);

            var d = db.GetDataSources(DateTime.UtcNow).Where(x => x.Name == "NCEP/NCAR Reanalysis 1 (regular grid)").ToList();
            Assert.AreEqual(d.Count, 1);
            Assert.AreEqual(d[0].Copyright, "MyCopyright");

            db.SetDataSourceCopyright("NCEP/NCAR Reanalysis 1 (regular grid)", "NewMyCopyright");
            var d2 = db.GetDataSources(DateTime.UtcNow).Where(x => x.Name == "NCEP/NCAR Reanalysis 1 (regular grid)").ToList();
            Assert.AreEqual(d2.Count, 1);
            Assert.AreEqual(d2[0].Copyright, "NewMyCopyright");
        }
    }
}
