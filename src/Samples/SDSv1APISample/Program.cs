using Microsoft.Research.Science.Data;
using Microsoft.Research.Science.Data.Imperative;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDSv1APISample
{
    class Program
    {
        static void Main(string[] args)
        {
            //// Fetching climate parameters for different months for years from 1961 to 1990
            using (var ds = DataSet.Open("msds:memory"))
            {

                Console.WriteLine("Filling dataset with pointset...");
                Random rand = new Random(1);
                double[] lats = new double[1000];
                double[] lons = new double[1000];
                for (int i = 0; i < 1000; i++)
                {
                    //generating pointset around Amazonas
                    lats[i] = rand.NextDouble() * 21.0 - 13;
                    lons[i] = rand.NextDouble() * 35.0 - 80;
                }
                ds.Add("Lat", lats, "points");
                ds.Add("Lon", lons, "points");

                ds.AddClimatologyAxisMonthly();

                // Fetching data from the Fetch Climate service
                Console.WriteLine("Filling dataset with corresponding data...");
                ds.Fetch(ClimateParameter.FC_PRECIPITATION, "prateAny"); // prate is 3d and depends on lat,lon,time
                ds.Fetch(ClimateParameter.FC_PRECIPITATION, "prateCRU", dataSource: EnvironmentalDataSource.CRU_CL_2_0);
                ds.Fetch(ClimateParameter.FC_PRECIPITATION, "prateNCEP", dataSource: EnvironmentalDataSource.NCEP_REANALYSIS_1);
                ds.Fetch(ClimateParameter.FC_LAND_AIR_RELATIVE_HUMIDITY, "relhum");

                Console.WriteLine("Running DataSet Viewer...");
                ds.View();
            }

            // Fetching climate parameters for fixed time moment in grid points
            using (var ds = DataSet.Open("msds:memory"))
            {
                Console.WriteLine("Filling dataset with grid...");
                ds.AddAxis("lon", "degrees East", -12.5, 20.0, 0.5);//grid represents single points
                ds.AddAxis("lat", "degrees North", 35.0, 60.0, 0.5);

                // Fetching data from the Fetch Climate service
                ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt", new DateTime(2000, 7, 19, 11, 0, 0), nameUncertainty: "airt-uncert", nameProvenance: "airt-prov"); // time is fixed hence airt is 2d (depends on lat and lon)
                ds.Fetch(ClimateParameter.FC_SOIL_MOISTURE, "soilm", new DateTime(2000, 7, 19, 11, 0, 0), nameUncertainty: "soilm-uncert");

                double value = ds.GetValue<double>("airt", "lat,lon", 37, 0.15);
                Console.WriteLine("{0} at (37, 0.12) is {1} {2}", ds["airt"].Metadata["DisplayName"], value, ds["airt"].Metadata["Units"]);

                Console.WriteLine("Running DataSet Viewer...");
                ds.View(@"airt(lon,lat) Style:Colormap; Palette:-5=Blue,White=0,#F4EF2F=5,Red=20; MapType:Aerial; Transparency:0.57;;soilm(lon,lat) Style:Colormap; Palette:0=#00000000,#827DAAEC=300,#0000A0=800; Transparency:0; MapType:Aerial");
            }

            // Fetching mean wind speed near surface for 30 years (1961-1990) in grid cells
            using (var ds = DataSet.Open("msds:memory"))
            {
                Console.WriteLine("Filling dataset with grid...");
                //The united States
                ds.AddAxisCells("lon", "degrees East", -126.8, -67.8, 1.0);
                ds.AddAxisCells("lat", "degrees North", 24.2, 48.2, 1.0);

                //Adding time axis depicting single continuous interval from 1961 till 1990
                ds.AddClimatologyAxisYearly(yearmin: 1961, yearmax: 1990, yearStep: 29);

                ds.Fetch(ClimateParameter.FC_LAND_WIND_SPEED, "wnd");
                ds.Commit();

                //do some processing                
                int lowWindCells = 0;
                int highWindCells = 0;
                int nonMissingValues = 0;

                double[, ,] values = ds.GetData<double[, ,]>("wnd");

                for (int i = 0; i < ds.Variables["wnd"].Dimensions[0].Length; i++)
                    for (int j = 0; j < ds.Variables["wnd"].Dimensions[1].Length; j++)
                    {
                        double value = values[i, j, 1];
                        if (!double.IsNaN(value))
                        {
                            nonMissingValues++;
                            if (value > 5)
                                highWindCells++;
                            if (value < 0.5)
                                lowWindCells++;
                        }
                    }
                Console.WriteLine("{0:F1}% of cells have high wind values and {1:F1}% of cells have low wind values",
                    ((double)highWindCells) / nonMissingValues * 100.0,
                    ((double)lowWindCells) / nonMissingValues * 100.0);
            }
        }
    }
}
