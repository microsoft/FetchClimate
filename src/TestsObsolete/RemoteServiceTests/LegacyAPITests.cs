using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Research.Science.Data;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Research.Science.FetchClimate2.Tests
{
    [TestClass]
    public class LegacyAPITests
    {
        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void LegacyFC_TEMERATURETest()
        {
            TestParameter(ClimateParameter.FC_TEMPERATURE, EnvironmentalDataSource.ANY);
            TestParameter(ClimateParameter.FC_TEMPERATURE, EnvironmentalDataSource.CRU_CL_2_0);
            TestParameter(ClimateParameter.FC_TEMPERATURE, EnvironmentalDataSource.WORLD_CLIM_1_4);
            TestParameter(ClimateParameter.FC_TEMPERATURE, EnvironmentalDataSource.NCEP_REANALYSIS_1);
            TestParameter(ClimateParameter.FC_TEMPERATURE, EnvironmentalDataSource.GHCNv2);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void LegacyFC_PRECIPITATIONTest()
        {
            TestParameter(ClimateParameter.FC_PRECIPITATION, EnvironmentalDataSource.ANY);
            TestParameter(ClimateParameter.FC_PRECIPITATION, EnvironmentalDataSource.CRU_CL_2_0);
            TestParameter(ClimateParameter.FC_PRECIPITATION, EnvironmentalDataSource.WORLD_CLIM_1_4);
            TestParameter(ClimateParameter.FC_PRECIPITATION, EnvironmentalDataSource.NCEP_REANALYSIS_1);
            TestParameter(ClimateParameter.FC_PRECIPITATION, EnvironmentalDataSource.GHCNv2);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void LegacyFC_LAND_AIR_TEMPERATURETest()
        {
            TestParameter(ClimateParameter.FC_LAND_AIR_TEMPERATURE, EnvironmentalDataSource.ANY);
            TestParameter(ClimateParameter.FC_LAND_AIR_TEMPERATURE, EnvironmentalDataSource.CRU_CL_2_0);
            TestParameter(ClimateParameter.FC_LAND_AIR_TEMPERATURE, EnvironmentalDataSource.WORLD_CLIM_1_4);            
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void LegacyFC_OCEAN_AIR_TEMPERATURETest()
        {
            TestParameter(ClimateParameter.FC_OCEAN_AIR_TEMPERATURE, EnvironmentalDataSource.ANY);            
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void LegacyFC_LAND_AIR_RELATIVE_HUMIDITYTest()
        {
            TestParameter(ClimateParameter.FC_LAND_AIR_RELATIVE_HUMIDITY, EnvironmentalDataSource.ANY);
            TestParameter(ClimateParameter.FC_LAND_AIR_RELATIVE_HUMIDITY, EnvironmentalDataSource.CRU_CL_2_0);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void LegacyFC_RELATIVE_HUMIDITYTest()
        {
            TestParameter(ClimateParameter.FC_RELATIVE_HUMIDITY, EnvironmentalDataSource.ANY);
            TestParameter(ClimateParameter.FC_RELATIVE_HUMIDITY, EnvironmentalDataSource.CRU_CL_2_0);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void LegacyFC_LAND_DIURNAL_TEMPERATURE_RANGETest()
        {
            TestParameter(ClimateParameter.FC_LAND_DIURNAL_TEMPERATURE_RANGE, EnvironmentalDataSource.ANY);
            TestParameter(ClimateParameter.FC_LAND_DIURNAL_TEMPERATURE_RANGE, EnvironmentalDataSource.CRU_CL_2_0);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void LegacyFC_LAND_WIND_SPEEDTest()
        {
            TestParameter(ClimateParameter.FC_LAND_WIND_SPEED, EnvironmentalDataSource.ANY);
            TestParameter(ClimateParameter.FC_LAND_WIND_SPEED, EnvironmentalDataSource.CRU_CL_2_0);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void LegacyFC_LAND_FROST_DAY_FREQUENCYTest()
        {
            TestParameter(ClimateParameter.FC_LAND_FROST_DAY_FREQUENCY, EnvironmentalDataSource.ANY);
            TestParameter(ClimateParameter.FC_LAND_FROST_DAY_FREQUENCY, EnvironmentalDataSource.CRU_CL_2_0);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void LegacyFC_LAND_WET_DAY_FREQUENCYTest()
        {
            TestParameter(ClimateParameter.FC_LAND_WET_DAY_FREQUENCY, EnvironmentalDataSource.ANY);
            TestParameter(ClimateParameter.FC_LAND_WET_DAY_FREQUENCY, EnvironmentalDataSource.CRU_CL_2_0);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void LegacyFC_LAND_SUN_PERCENTAGETest()
        {
            TestParameter(ClimateParameter.FC_LAND_SUN_PERCENTAGE, EnvironmentalDataSource.ANY);
            TestParameter(ClimateParameter.FC_LAND_SUN_PERCENTAGE, EnvironmentalDataSource.CRU_CL_2_0);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void LegacyFC_ELEVATIONTest()
        {
            TestParameter(ClimateParameter.FC_ELEVATION, EnvironmentalDataSource.ANY);
            TestParameter(ClimateParameter.FC_ELEVATION, EnvironmentalDataSource.ETOPO1_ICE_SHEETS);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void LegacyFC_OCEAN_DEPTHTest()
        {
            TestParameter(ClimateParameter.FC_OCEAN_DEPTH, EnvironmentalDataSource.ANY);            
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void LegacyFC_LAND_ELEVATIONTest()
        {
            TestParameter(ClimateParameter.FC_LAND_ELEVATION, EnvironmentalDataSource.ANY);            
            TestParameter(ClimateParameter.FC_LAND_ELEVATION, EnvironmentalDataSource.GTOPO30);
        }

        [TestMethod]
        [TestCategory("Uses remote Cloud deployment")]
        public void LegacyFC_SOIL_MOISTURETest()
        {
            TestParameter(ClimateParameter.FC_SOIL_MOISTURE, EnvironmentalDataSource.ANY);
            TestParameter(ClimateParameter.FC_SOIL_MOISTURE, EnvironmentalDataSource.CPC_SOIL_MOSITURE);
        }

        public void TestParameter(ClimateParameter p, EnvironmentalDataSource ds)
        {
            ClimateService.ServiceUrl = "http://fetchclimate2.cloudapp.net/";

            const double MoscowLat = 55.7;
            const double MoscowLon = 37.5;

            const double PacificLat = -20;
            const double PacificLon = 170;

            const double PacificLatA = -15;
            const double PacificLonA = 175;

            const double KrasnoyarskLat = 56.017;
            const double KrasnoyarskLon = 92.867;

            const double AroundKrasnoyarskLatMin = 55;
            const double AroundKrasnoyarskLonMin = 91;
                         
            const double AroundKrasnoyarskLatMax = 60;
            const double AroundKrasnoyarskLonMax = 95;

            const double SriLankaLatMin = 5;
            const double SriLankaLonMin = 70;
                         
            const double SriLankaLatMax = 20;
            const double SriLankaLonMax = 87;

            string varName = ClimateService.ClimateParameterToFC2VariableName(p);
            Assert.AreNotEqual("", varName, string.Format("Mapping for {0} does not exist.", p.ToString()));

            string[] sources = ClimateService.EnvironmentalDataSourceToArrayOfFC2DataSources(ds);

            //Single point fetch
            var tr1 = new TimeRegion(1961, 1990);
            var tr2 = new TimeRegion(1990, 2000);
            //Moscow
            var request1 = new FetchRequest(
                varName,
                FetchDomain.CreatePoints(
                    new double[] { MoscowLat },
                    new double[] { MoscowLon },
                    tr1),
                sources);
            var result1 = ClimateService.FetchAsync(request1).Result;
            string provenance1;
            if (sources == null || sources.Length > 1)
            {
                UInt16[] ids1 = (UInt16[])result1["provenance"].GetData();
                provenance1 = ClimateService.Configuration.DataSources.Single(x => x.ID == ids1[0]).Name;
            }
            else
                provenance1 = sources[0];
            double sd1 = ((double[])result1["sd"].GetData())[0];
            double value1 = ((double[])result1["values"].GetData())[0];
            Assert.AreEqual(provenance1, ClimateService.FetchClimateProvenance(p, MoscowLat, MoscowLat, MoscowLon, MoscowLon, dataSource: ds));
            Assert.AreEqual(sd1, ClimateService.FetchClimateUncertainty(p, MoscowLat, MoscowLat, MoscowLon, MoscowLon, dataSource: ds));
            Assert.AreEqual(value1, ClimateService.FetchClimate(p, MoscowLat, MoscowLat, MoscowLon, MoscowLon, dataSource: ds));

            //somewhere in Pacific Ocean
            var request2 = new FetchRequest(
                varName,
                FetchDomain.CreatePoints(
                    new double[] { PacificLat },
                    new double[] { PacificLon },
                    tr1),
                sources);
            var result2 = ClimateService.FetchAsync(request2).Result;
            string provenance2;
            if (sources == null || sources.Length > 1)
            {
                UInt16[] ids2 = (UInt16[])result2["provenance"].GetData();
                provenance2 = ClimateService.Configuration.DataSources.Single(x => x.ID == ids2[0]).Name;
            }
            else
                provenance2 = sources[0];
            double sd2 = ((double[])result2["sd"].GetData())[0];
            double value2 = ((double[])result2["values"].GetData())[0];
            Assert.AreEqual(provenance2, ClimateService.FetchClimateProvenance(p, PacificLat, PacificLat, PacificLon, PacificLon, dataSource: ds));
            Assert.AreEqual(sd2, ClimateService.FetchClimateUncertainty(p, PacificLat, PacificLat, PacificLon, PacificLon, dataSource: ds));
            Assert.AreEqual(value2, ClimateService.FetchClimate(p, PacificLat, PacificLat, PacificLon, PacificLon, dataSource: ds));

            //Cell around Krasnoyarsk
            var request3 = new FetchRequest(
                varName,
                FetchDomain.CreateCells(
                    new double[] { AroundKrasnoyarskLatMin },
                    new double[] { AroundKrasnoyarskLonMin },
                    new double[] { AroundKrasnoyarskLatMax },
                    new double[] { AroundKrasnoyarskLonMax },
                    tr2),
                sources);
            var result3 = ClimateService.FetchAsync(request3).Result;
            string provenance3;
            if (sources == null || sources.Length > 1)
            {
                UInt16[] ids3 = (UInt16[])result3["provenance"].GetData();
                provenance3 = ClimateService.Configuration.DataSources.Single(x => x.ID == ids3[0]).Name;
            }
            else
                provenance3 = sources[0];
            double sd3 = ((double[])result3["sd"].GetData())[0];
            double value3 = ((double[])result3["values"].GetData())[0];
            Assert.AreEqual(provenance3, ClimateService.FetchClimateProvenance(p, AroundKrasnoyarskLatMin, AroundKrasnoyarskLatMax, AroundKrasnoyarskLonMin, AroundKrasnoyarskLonMax, startyear: 1990, stopyear: 2000, dataSource: ds));
            Assert.AreEqual(sd3, ClimateService.FetchClimateUncertainty(p, AroundKrasnoyarskLatMin, AroundKrasnoyarskLatMax, AroundKrasnoyarskLonMin, AroundKrasnoyarskLonMax, startyear: 1990, stopyear: 2000, dataSource: ds));
            Assert.AreEqual(value3, ClimateService.FetchClimate(p, AroundKrasnoyarskLatMin, AroundKrasnoyarskLatMax, AroundKrasnoyarskLonMin, AroundKrasnoyarskLonMax, startyear: 1990, stopyear: 2000, dataSource: ds));

            //Cell somewhere in Pacific Ocean
            var request4 = new FetchRequest(
                varName,
                FetchDomain.CreateCells(
                    new double[] { PacificLat },
                    new double[] { PacificLon },
                    new double[] { PacificLatA },
                    new double[] { PacificLonA },
                    tr2),
                sources);
            var result4 = ClimateService.FetchAsync(request4).Result;
            string provenance4;
            if (sources == null || sources.Length > 1)
            {
                UInt16[] ids4 = (UInt16[])result4["provenance"].GetData();
                provenance4 = ClimateService.Configuration.DataSources.Single(x => x.ID == ids4[0]).Name;
            }
            else
                provenance4 = sources[0];
            double sd4 = ((double[])result4["sd"].GetData())[0];
            double value4 = ((double[])result4["values"].GetData())[0];
            Assert.AreEqual(provenance4, ClimateService.FetchClimateProvenance(p, PacificLat, PacificLatA, PacificLon, PacificLonA, startyear: 1990, stopyear: 2000, dataSource: ds));
            Assert.AreEqual(sd4, ClimateService.FetchClimateUncertainty(p, PacificLat, PacificLatA, PacificLon, PacificLonA, startyear: 1990, stopyear: 2000, dataSource: ds));
            Assert.AreEqual(value4, ClimateService.FetchClimate(p, PacificLat, PacificLatA, PacificLon, PacificLonA, startyear: 1990, stopyear: 2000, dataSource: ds));

            //batch request
            double[] batchLonMin = new double[] { PacificLon, AroundKrasnoyarskLonMin };
            double[] batchLonMax = new double[] { PacificLon, AroundKrasnoyarskLonMax };
            double[] batchLatMin = new double[] { PacificLat, AroundKrasnoyarskLatMin };
            double[] batchLatMax = new double[] { PacificLat, AroundKrasnoyarskLatMax };
            int[] batchStartYear = new int[] { 1961, 1990 };
            int[] batchStopYear = new int[] { 1990, 2000 };

            string[] provenanceGuess1 = ClimateService.FetchClimateProvenance(p, batchLatMin, batchLatMax, batchLonMin, batchLonMax, null, null, null, null, batchStartYear, batchStopYear, ds);
            double[] sdGuess1 = ClimateService.FetchClimateUncertainty(p, batchLatMin, batchLatMax, batchLonMin, batchLonMax, null, null, null, null, batchStartYear, batchStopYear, ds);
            double[] valueGuess1 = ClimateService.FetchClimate(p, batchLatMin, batchLatMax, batchLonMin, batchLonMax, null, null, null, null, batchStartYear, batchStopYear, ds);

            Assert.AreEqual(provenance2, provenanceGuess1[0]);
            Assert.AreEqual(provenance3, provenanceGuess1[1]);
            Assert.AreEqual(sd2, sdGuess1[0]);
            Assert.AreEqual(sd3, sdGuess1[1]);
            Assert.AreEqual(value2, valueGuess1[0]);
            Assert.AreEqual(value3, valueGuess1[1]);

            //grid request
            var request5 = new FetchRequest(
                varName,
                FetchDomain.CreateCellGrid(
                    Enumerable.Range(0, (int)Math.Round((SriLankaLatMax - SriLankaLatMin) / 1) + 1).Select(i => SriLankaLatMin + i).ToArray(),
                    Enumerable.Range(0, (int)Math.Round((SriLankaLonMax - SriLankaLonMin) / 1) + 1).Select(i => SriLankaLonMin + i).ToArray(),
                    tr2),
                sources);
            var result5 = ClimateService.FetchAsync(request5).Result;
            
            double[,] gridSds = (double[,])result5["sd"].GetData();
            double[,] gridValues = (double[,])result5["values"].GetData();
            string[,] gridProofs;
            int len0 = gridSds.GetLength(0), len1 = gridSds.GetLength(1);
            gridProofs = new string[len0, len1];
            if (sources == null || sources.Length > 1)
            {
                UInt16[,] gridProvIds = (UInt16[,])result5["provenance"].GetData();
                for (int i = 0; i < len0; ++i)
                    for (int j = 0; j < len1; ++j) gridProofs[i, j] = ClimateService.Configuration.DataSources.Single(x => x.ID == gridProvIds[i, j]).Name;
            }
            else
                for (int i = 0; i < len0; ++i)
                    for (int j = 0; j < len1; ++j) gridProofs[i, j] = sources[0];

            string[,] provenanceGuess2 = ClimateService.FetchProvenanceGrid(p, SriLankaLatMin, SriLankaLatMax, SriLankaLonMin, SriLankaLonMax, 1, 1, yearmin: 1990, yearmax: 2000, dataSource: ds);
            double[,] sdGuess2 = ClimateService.FetchUncertaintyGrid(p, SriLankaLatMin, SriLankaLatMax, SriLankaLonMin, SriLankaLonMax, 1, 1, yearmin: 1990, yearmax: 2000, dataSource: ds);
            double[,] valueGuess2 = ClimateService.FetchClimateGrid(p, SriLankaLatMin, SriLankaLatMax, SriLankaLonMin, SriLankaLonMax, 1, 1, yearmin: 1990, yearmax: 2000, dataSource: ds);

            //in FC2 grid is lon x lat while in FC1 it was lat x lon
            Assert.AreEqual(len0, provenanceGuess2.GetLength(1));
            Assert.AreEqual(len1, provenanceGuess2.GetLength(0));
            Assert.AreEqual(len0, sdGuess2.GetLength(1));
            Assert.AreEqual(len1, sdGuess2.GetLength(0));
            Assert.AreEqual(len0, valueGuess2.GetLength(1));
            Assert.AreEqual(len1, valueGuess2.GetLength(0));
            for (int i = 0; i < len0; ++i)
                for (int j = 0; j < len1; ++j)
                {
                    Assert.AreEqual(gridProofs[i, j], provenanceGuess2[j, i]);
                    Assert.AreEqual(gridSds[i, j], sdGuess2[j, i]);
                    Assert.AreEqual(gridValues[i, j], valueGuess2[j, i]);
                }

            //Yearly TimeSeries for Krasnoyarsk
            var tr3 = new TimeRegion(1990, 2000).GetYearlyTimeseries(1990, 2000);
            var request6 = new FetchRequest(
                varName,
                FetchDomain.CreatePoints(
                    new double[] { KrasnoyarskLat },
                    new double[] { KrasnoyarskLon },
                    tr3),
                sources);
            var result6 = ClimateService.FetchAsync(request6).Result;

            double[,] seriesSds1 = (double[,])result6["sd"].GetData();
            double[,] seriesValues1 = (double[,])result6["values"].GetData();
            string[] seriesProofs1 = new string[seriesValues1.GetLength(1)];
            if (sources == null || sources.Length > 1)
            {
                UInt16[,] seriesProvIds1 = (UInt16[,])result6["provenance"].GetData();
                for (int i = 0; i < seriesProvIds1.Length; ++i) seriesProofs1[i] = ClimateService.Configuration.DataSources.Single(x => x.ID == seriesProvIds1[0, i]).Name;
            }
            else
                for (int i = 0; i < seriesProofs1.Length; ++i) seriesProofs1[i] = sources[0];

            string[] seriesProofsGuess1 = ClimateService.FetchClimateYearlyProvenance(p, KrasnoyarskLat, KrasnoyarskLat, KrasnoyarskLon, KrasnoyarskLon, yearmin: 1990, yearmax: 2000, dataSource: ds);
            double[] seriesSdsGuess1 = ClimateService.FetchClimateYearlyUncertainty(p, KrasnoyarskLat, KrasnoyarskLat, KrasnoyarskLon, KrasnoyarskLon, yearmin: 1990, yearmax: 2000, dataSource: ds);
            double[] seriesValuesGuess1 = ClimateService.FetchClimateYearly(p, KrasnoyarskLat, KrasnoyarskLat, KrasnoyarskLon, KrasnoyarskLon, yearmin: 1990, yearmax: 2000, dataSource: ds);

            Assert.AreEqual(seriesProofs1.Length, seriesProofsGuess1.Length);
            Assert.AreEqual(seriesSds1.Length, seriesSdsGuess1.Length);
            Assert.AreEqual(seriesValues1.Length, seriesValuesGuess1.Length);
            for (int i = 0; i < seriesProofs1.Length; ++i)
            {
                Assert.AreEqual(seriesProofs1[i], seriesProofsGuess1[i]);
                Assert.AreEqual(seriesSds1[0, i], seriesSdsGuess1[i]);
                Assert.AreEqual(seriesValues1[0, i], seriesValuesGuess1[i]);
            }

            //Monthly TimeSeries for Krasnoyarsk
            var tr4 = new TimeRegion(1990, 1991).GetSeasonlyTimeseries(30, 40);
            var request7 = new FetchRequest(
                varName,
                FetchDomain.CreatePoints(
                    new double[] { KrasnoyarskLat },
                    new double[] { KrasnoyarskLon },
                    tr4),
                sources);
            var result7 = ClimateService.FetchAsync(request7).Result;

            double[,] seriesSds2 = (double[,])result7["sd"].GetData();
            double[,] seriesValues2 = (double[,])result7["values"].GetData();
            string[] seriesProofs2 = new string[seriesValues2.GetLength(1)];
            if (sources == null || sources.Length > 1)
            {
                UInt16[,] seriesProvIds2 = (UInt16[,])result7["provenance"].GetData();
                for (int i = 0; i < seriesProvIds2.Length; ++i) seriesProofs2[i] = ClimateService.Configuration.DataSources.Single(x => x.ID == seriesProvIds2[0, i]).Name;
            }
            else
                for (int i = 0; i < seriesProofs2.Length; ++i) seriesProofs2[i] = sources[0];

            string[] seriesProofsGuess2 = ClimateService.FetchClimateSeasonlyProvenance(p, KrasnoyarskLat, KrasnoyarskLat, KrasnoyarskLon, KrasnoyarskLon, daymin: 30, daymax: 40, yearmin: 1990, yearmax: 1991, dataSource: ds);
            double[] seriesSdsGuess2 = ClimateService.FetchClimateSeasonlyUncertainty(p, KrasnoyarskLat, KrasnoyarskLat, KrasnoyarskLon, KrasnoyarskLon, daymin: 30, daymax: 40, yearmin: 1990, yearmax: 1991, dataSource: ds);
            double[] seriesValuesGuess2 = ClimateService.FetchClimateSeasonly(p, KrasnoyarskLat, KrasnoyarskLat, KrasnoyarskLon, KrasnoyarskLon, daymin: 30, daymax: 40, yearmin: 1990, yearmax: 1991, dataSource: ds);

            Assert.AreEqual(seriesProofs2.Length, seriesProofsGuess2.Length);
            Assert.AreEqual(seriesSds2.Length, seriesSdsGuess2.Length);
            Assert.AreEqual(seriesValues2.Length, seriesValuesGuess2.Length);
            for (int i = 0; i < seriesProofs2.Length; ++i)
            {
                Assert.AreEqual(seriesProofs2[i], seriesProofsGuess2[i]);
                Assert.AreEqual(seriesSds2[0, i], seriesSdsGuess2[i]);
                Assert.AreEqual(seriesValues2[0, i], seriesValuesGuess2[i]);
            }

            //Hourly TimeSeries for Krasnoyarsk
            var tr5 = new TimeRegion(1990, 1991, 30, 31).GetHourlyTimeseries(isIntervalTimeseries: true);
            var request8 = new FetchRequest(
                varName,
                FetchDomain.CreatePoints(
                    new double[] { KrasnoyarskLat },
                    new double[] { KrasnoyarskLon },
                    tr5),
                sources);
            var result8 = ClimateService.FetchAsync(request8).Result;

            double[,] seriesSds3 = (double[,])result8["sd"].GetData();
            double[,] seriesValues3 = (double[,])result8["values"].GetData();
            string[] seriesProofs3 = new string[seriesValues3.Length];
            if (sources == null || sources.Length > 1)
            {
                UInt16[,] seriesProvIds3 = (UInt16[,])result8["provenance"].GetData();
                for (int i = 0; i < seriesProvIds3.Length; ++i) seriesProofs3[i] = ClimateService.Configuration.DataSources.Single(x => x.ID == seriesProvIds3[0, i]).Name;
            }
            else
                for (int i = 0; i < seriesProofs3.Length; ++i) seriesProofs3[i] = sources[0];

            string[] seriesProofsGuess3 = ClimateService.FetchClimateHourlyProvenance(p, KrasnoyarskLat, KrasnoyarskLat, KrasnoyarskLon, KrasnoyarskLon, daymin: 30, daymax: 31, yearmin: 1990, yearmax: 1991, dataSource: ds);
            double[] seriesSdsGuess3 = ClimateService.FetchClimateHourlyUncertainty(p, KrasnoyarskLat, KrasnoyarskLat, KrasnoyarskLon, KrasnoyarskLon, daymin: 30, daymax: 31, yearmin: 1990, yearmax: 1991, dataSource: ds);
            double[] seriesValuesGuess3 = ClimateService.FetchClimateHourly(p, KrasnoyarskLat, KrasnoyarskLat, KrasnoyarskLon, KrasnoyarskLon, daymin: 30, daymax: 31, yearmin: 1990, yearmax: 1991, dataSource: ds);

            Assert.AreEqual(seriesProofs3.Length, seriesProofsGuess3.Length);
            Assert.AreEqual(seriesSds3.Length, seriesSdsGuess3.Length);
            Assert.AreEqual(seriesValues3.Length, seriesValuesGuess3.Length);
            for (int i = 0; i < seriesProofs3.Length; ++i)
            {
                Assert.AreEqual(seriesProofs3[i], seriesProofsGuess3[i]);
                Assert.AreEqual(seriesSds3[0, i], seriesSdsGuess3[i]);
                Assert.AreEqual(seriesValues3[0, i], seriesValuesGuess3[i]);
            }
        }

        public void TestParameterIgnoringProvenance(ClimateParameter p, EnvironmentalDataSource ds)
        {
            ClimateService.ServiceUrl = "http://fetchclimate2.cloudapp.net/";

            const double MoscowLat = 55.7;
            const double MoscowLon = 37.5;

            const double PacificLat = -20;
            const double PacificLon = 170;

            const double PacificLatA = -15;
            const double PacificLonA = 175;

            const double KrasnoyarskLat = 56.017;
            const double KrasnoyarskLon = 92.867;

            const double AroundKrasnoyarskLatMin = 55;
            const double AroundKrasnoyarskLonMin = 91;

            const double AroundKrasnoyarskLatMax = 60;
            const double AroundKrasnoyarskLonMax = 95;

            const double SriLankaLatMin = 5;
            const double SriLankaLonMin = 70;

            const double SriLankaLatMax = 20;
            const double SriLankaLonMax = 87;

            string varName = ClimateService.ClimateParameterToFC2VariableName(p);
            Assert.AreNotEqual("", varName, string.Format("Mapping for {0} does not exist.", p.ToString()));

            string[] sources = ClimateService.EnvironmentalDataSourceToArrayOfFC2DataSources(ds);

            //Single point fetch
            var tr1 = new TimeRegion(1961, 1990);
            var tr2 = new TimeRegion(1990, 2000);
            //Moscow
            var request1 = new FetchRequest(
                varName,
                FetchDomain.CreatePoints(
                    new double[] { MoscowLat },
                    new double[] { MoscowLon },
                    tr1),
                sources);
            var result1 = ClimateService.FetchAsync(request1).Result;
            double sd1 = ((double[])result1["sd"].GetData())[0];
            double value1 = ((double[])result1["values"].GetData())[0];
            Assert.AreEqual(sd1, ClimateService.FetchClimateUncertainty(p, MoscowLat, MoscowLat, MoscowLon, MoscowLon, dataSource: ds));
            Assert.AreEqual(value1, ClimateService.FetchClimate(p, MoscowLat, MoscowLat, MoscowLon, MoscowLon, dataSource: ds));

            //somewhere in Pacific Ocean
            var request2 = new FetchRequest(
                varName,
                FetchDomain.CreatePoints(
                    new double[] { PacificLat },
                    new double[] { PacificLon },
                    tr1),
                sources);
            var result2 = ClimateService.FetchAsync(request2).Result;
            double sd2 = ((double[])result2["sd"].GetData())[0];
            double value2 = ((double[])result2["values"].GetData())[0];
            Assert.AreEqual(sd2, ClimateService.FetchClimateUncertainty(p, PacificLat, PacificLat, PacificLon, PacificLon, dataSource: ds));
            Assert.AreEqual(value2, ClimateService.FetchClimate(p, PacificLat, PacificLat, PacificLon, PacificLon, dataSource: ds));

            //Cell around Krasnoyarsk
            var request3 = new FetchRequest(
                varName,
                FetchDomain.CreateCells(
                    new double[] { AroundKrasnoyarskLatMin },
                    new double[] { AroundKrasnoyarskLonMin },
                    new double[] { AroundKrasnoyarskLatMax },
                    new double[] { AroundKrasnoyarskLonMax },
                    tr2),
                sources);
            var result3 = ClimateService.FetchAsync(request3).Result;
            double sd3 = ((double[])result3["sd"].GetData())[0];
            double value3 = ((double[])result3["values"].GetData())[0];
            Assert.AreEqual(sd3, ClimateService.FetchClimateUncertainty(p, AroundKrasnoyarskLatMin, AroundKrasnoyarskLatMax, AroundKrasnoyarskLonMin, AroundKrasnoyarskLonMax, startyear: 1990, stopyear: 2000, dataSource: ds));
            Assert.AreEqual(value3, ClimateService.FetchClimate(p, AroundKrasnoyarskLatMin, AroundKrasnoyarskLatMax, AroundKrasnoyarskLonMin, AroundKrasnoyarskLonMax, startyear: 1990, stopyear: 2000, dataSource: ds));

            //Cell somewhere in Pacific Ocean
            var request4 = new FetchRequest(
                varName,
                FetchDomain.CreateCells(
                    new double[] { PacificLat },
                    new double[] { PacificLon },
                    new double[] { PacificLatA },
                    new double[] { PacificLonA },
                    tr2),
                sources);
            var result4 = ClimateService.FetchAsync(request4).Result;
            double sd4 = ((double[])result4["sd"].GetData())[0];
            double value4 = ((double[])result4["values"].GetData())[0];
            Assert.AreEqual(sd4, ClimateService.FetchClimateUncertainty(p, PacificLat, PacificLatA, PacificLon, PacificLonA, startyear: 1990, stopyear: 2000, dataSource: ds));
            Assert.AreEqual(value4, ClimateService.FetchClimate(p, PacificLat, PacificLatA, PacificLon, PacificLonA, startyear: 1990, stopyear: 2000, dataSource: ds));

            //batch request
            double[] batchLonMin = new double[] { PacificLon, AroundKrasnoyarskLonMin };
            double[] batchLonMax = new double[] { PacificLon, AroundKrasnoyarskLonMax };
            double[] batchLatMin = new double[] { PacificLat, AroundKrasnoyarskLatMin };
            double[] batchLatMax = new double[] { PacificLat, AroundKrasnoyarskLatMax };
            int[] batchStartYear = new int[] { 1961, 1990 };
            int[] batchStopYear = new int[] { 1990, 2000 };

            double[] sdGuess1 = ClimateService.FetchClimateUncertainty(p, batchLatMin, batchLatMax, batchLonMin, batchLonMax, null, null, null, null, batchStartYear, batchStopYear, ds);
            double[] valueGuess1 = ClimateService.FetchClimate(p, batchLatMin, batchLatMax, batchLonMin, batchLonMax, null, null, null, null, batchStartYear, batchStopYear, ds);

            Assert.AreEqual(sd2, sdGuess1[0]);
            Assert.AreEqual(sd3, sdGuess1[1]);
            Assert.AreEqual(value2, valueGuess1[0]);
            Assert.AreEqual(value3, valueGuess1[1]);

            //grid request
            var request5 = new FetchRequest(
                varName,
                FetchDomain.CreateCellGrid(
                    Enumerable.Range(0, (int)Math.Round((SriLankaLatMax - SriLankaLatMin) / 1) + 1).Select(i => SriLankaLatMin + i).ToArray(),
                    Enumerable.Range(0, (int)Math.Round((SriLankaLonMax - SriLankaLonMin) / 1) + 1).Select(i => SriLankaLonMin + i).ToArray(),
                    tr2),
                sources);
            var result5 = ClimateService.FetchAsync(request5).Result;

            double[,] gridSds = (double[,])result5["sd"].GetData();
            double[,] gridValues = (double[,])result5["values"].GetData();
            int len0 = gridSds.GetLength(0), len1 = gridSds.GetLength(1);

            double[,] sdGuess2 = ClimateService.FetchUncertaintyGrid(p, SriLankaLatMin, SriLankaLatMax, SriLankaLonMin, SriLankaLonMax, 1, 1, yearmin: 1990, yearmax: 2000, dataSource: ds);
            double[,] valueGuess2 = ClimateService.FetchClimateGrid(p, SriLankaLatMin, SriLankaLatMax, SriLankaLonMin, SriLankaLonMax, 1, 1, yearmin: 1990, yearmax: 2000, dataSource: ds);

            //in FC2 grid is lon x lat while in FC1 it was lat x lon
            Assert.AreEqual(len0, sdGuess2.GetLength(1));
            Assert.AreEqual(len1, sdGuess2.GetLength(0));
            Assert.AreEqual(len0, valueGuess2.GetLength(1));
            Assert.AreEqual(len1, valueGuess2.GetLength(0));
            for (int i = 0; i < len0; ++i)
                for (int j = 0; j < len1; ++j)
                {
                    Assert.AreEqual(gridSds[i, j], sdGuess2[j, i]);
                    Assert.AreEqual(gridValues[i, j], valueGuess2[j, i]);
                }

            //Yearly TimeSeries for Krasnoyarsk
            var tr3 = new TimeRegion().GetYearlyTimeseries(1990, 2000);
            var request6 = new FetchRequest(
                varName,
                FetchDomain.CreatePoints(
                    new double[] { KrasnoyarskLat },
                    new double[] { KrasnoyarskLon },
                    tr3),
                sources);
            var result6 = ClimateService.FetchAsync(request6).Result;

            double[,] seriesSds1 = (double[,])result6["sd"].GetData();
            double[,] seriesValues1 = (double[,])result6["values"].GetData();

            double[] seriesSdsGuess1 = ClimateService.FetchClimateYearlyUncertainty(p, KrasnoyarskLat, KrasnoyarskLat, KrasnoyarskLon, KrasnoyarskLon, yearmin: 1990, yearmax: 2000, dataSource: ds);
            double[] seriesValuesGuess1 = ClimateService.FetchClimateYearly(p, KrasnoyarskLat, KrasnoyarskLat, KrasnoyarskLon, KrasnoyarskLon, yearmin: 1990, yearmax: 2000, dataSource: ds);

            Assert.AreEqual(seriesSds1.Length, seriesSdsGuess1.Length);
            Assert.AreEqual(seriesValues1.Length, seriesValuesGuess1.Length);
            for (int i = 0; i < seriesValues1.Length; ++i)
            {
                Assert.AreEqual(seriesSds1[0, i], seriesSdsGuess1[i]);
                Assert.AreEqual(seriesValues1[0, i], seriesValuesGuess1[i]);
            }

            //Monthly TimeSeries for Krasnoyarsk
            var tr4 = new TimeRegion(1990, 1991).GetSeasonlyTimeseries(30, 40);
            var request7 = new FetchRequest(
                varName,
                FetchDomain.CreatePoints(
                    new double[] { KrasnoyarskLat },
                    new double[] { KrasnoyarskLon },
                    tr4),
                sources);
            var result7 = ClimateService.FetchAsync(request7).Result;

            double[,] seriesSds2 = (double[,])result7["sd"].GetData();
            double[,] seriesValues2 = (double[,])result7["values"].GetData();

            double[] seriesSdsGuess2 = ClimateService.FetchClimateSeasonlyUncertainty(p, KrasnoyarskLat, KrasnoyarskLat, KrasnoyarskLon, KrasnoyarskLon, daymin: 30, daymax: 40, yearmin: 1990, yearmax: 1991, dataSource: ds);
            double[] seriesValuesGuess2 = ClimateService.FetchClimateSeasonly(p, KrasnoyarskLat, KrasnoyarskLat, KrasnoyarskLon, KrasnoyarskLon, daymin: 30, daymax: 40, yearmin: 1990, yearmax: 1991, dataSource: ds);

            Assert.AreEqual(seriesSds2.Length, seriesSdsGuess2.Length);
            Assert.AreEqual(seriesValues2.Length, seriesValuesGuess2.Length);
            for (int i = 0; i < seriesValues2.Length; ++i)
            {
                Assert.AreEqual(seriesSds2[0, i], seriesSdsGuess2[i]);
                Assert.AreEqual(seriesValues2[0, i], seriesValuesGuess2[i]);
            }

            //Hourly TimeSeries for Krasnoyarsk
            var tr5 = new TimeRegion(1990, 1991, 30, 31).GetHourlyTimeseries(isIntervalTimeseries: true);
            var request8 = new FetchRequest(
                varName,
                FetchDomain.CreatePoints(
                    new double[] { KrasnoyarskLat },
                    new double[] { KrasnoyarskLon },
                    tr5),
                sources);
            var result8 = ClimateService.FetchAsync(request8).Result;

            double[,] seriesSds3 = (double[,])result8["sd"].GetData();
            double[,] seriesValues3 = (double[,])result8["values"].GetData();

            double[] seriesSdsGuess3 = ClimateService.FetchClimateHourlyUncertainty(p, KrasnoyarskLat, KrasnoyarskLat, KrasnoyarskLon, KrasnoyarskLon, daymin: 30, daymax: 31, yearmin: 1990, yearmax: 1991, dataSource: ds);
            double[] seriesValuesGuess3 = ClimateService.FetchClimateHourly(p, KrasnoyarskLat, KrasnoyarskLat, KrasnoyarskLon, KrasnoyarskLon, daymin: 30, daymax: 31, yearmin: 1990, yearmax: 1991, dataSource: ds);

            Assert.AreEqual(seriesSds3.Length, seriesSdsGuess3.Length);
            Assert.AreEqual(seriesValues3.Length, seriesValuesGuess3.Length);
            for (int i = 0; i < seriesValues3.Length; ++i)
            {
                Assert.AreEqual(seriesSds3[0, i], seriesSdsGuess3[i]);
                Assert.AreEqual(seriesValues3[0, i], seriesValuesGuess3[i]);
            }
        }
    }
}
