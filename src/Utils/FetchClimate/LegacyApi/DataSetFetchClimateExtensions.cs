using Microsoft.Research.Science.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.Data.Imperative
{
    public static partial class DataSetFetchClimateExtensions
    {
        /// <summary>
        /// Creates new variable with data from the FetchClimate service for the current lat/lon coordinate system and given time.
        /// </summary>
        /// <param name="ds">Target DataSet.</param>
        /// <param name="parameter">A climate parameter to fetch.</param>
        /// <param name="name">Name of the variable.</param>
        /// <param name="time">Time moment to fetch data for.</param>
        /// <returns>New variable instance.</returns>
        /// <remarks>
        /// <para>
        /// The method looks up the <paramref name="ds"/> for the lat/lon coordinate system.
        /// An axis is considered as a latitude grid if at least one of the following conditions are satisfied 
        /// (case is ignored in all rules):
        /// <list type="bullet">
        /// <item><description>axis name starts with either "lat" or "_lat";</description></item>
        /// <item><description>axis name contains substring "latitude";</description></item>
        /// <item><description>axis has attribute Units containing substring "degree" and ends with "n" or "north".</description></item>
        /// </list>
        /// Similar rules for longitude axis:
        /// <list type="bullet">
        /// <item><description>axis name starts with either "lon" or "_lon";</description></item>
        /// <item><description>axis name contains substring "longitude";</description></item>
        /// <item><description>axis has attribute Units containing substring "degree" and ends with "e" or "east".</description></item>
        /// </list>       
        /// </para>
        /// <para>If the axes not found, an exception is thrown.</para>
        /// <para>When a coordinate system is found, the Fetch Climate service is requested using a single batch request;
        /// result is added to the DataSet as 2d-variable depending on lat/lon axes. 
        /// The DisplayName, long_name, Units, MissingValue, Provenance and Time attributes of the variable are set.
        /// </para>
        /// <example>
        /// <code>
        ///  // Fetching climate parameters for fixed time moment
        ///  using (var ds = DataSet.Open("msds:memory"))
        ///  {
        ///      Console.WriteLine("Filling dataset...");
        ///      ds.AddAxis("lon", "degrees East", -12.5, 20.0, 0.5);
        ///      ds.AddAxis("lat", "degrees North", 35.0, 60.0, 0.5);
        ///      
        ///      ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt", new DateTime(2000, 7, 19, 11, 0, 0)); // time is fixed hence airt is 2d (depends on lat and lon)
        ///      ds.Fetch(ClimateParameter.FC_SOIL_MOISTURE, "soilm", new DateTime(2000, 7, 19, 11, 0, 0));
        ///
        ///      Console.WriteLine("Running DataSet Viewer...");
        ///      ds.View(@"airt(lon,lat) Style:Colormap; Palette:-5=Blue,White=0,#F4EF2F=5,Red=20; MapType:Aerial; Transparency:0.57000000000000006;;soilm(lon,lat) Style:Colormap; Palette:0=#00000000,#827DAAEC=300,#0000A0=800; Transparency:0; MapType:Aerial");
        ///  }
        ///
        ///  // Fetching climate parameters for different time moments
        ///  using (var ds = DataSet.Open("msds:memory"))
        ///  {
        ///      Console.WriteLine("Filling dataset...");
        ///      ds.AddAxis("lon", "degrees East", -12.5, 20.0, 2.0);
        ///      ds.AddAxis("lat", "degrees North", 35.0, 60.0, 2.0);
        ///      ds.AddAxis("time", new DateTime(2000, 7, 19, 0, 0, 0), new DateTime(2000, 7, 19, 23, 0, 0), TimeSpan.FromHours(2));
        ///
        ///      ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt"); // airt depends on lat,lon,time
        ///      ds.Fetch(ClimateParameter.FC_SOIL_MOISTURE, "soilm");
        ///
        ///      Console.WriteLine("Running DataSet Viewer...");
        ///      ds.View(@"airt(lon,lat) Style:Colormap; Palette:-5=Blue,White=0,#F4EF2F=5,Red=20; MapType:Aerial; Transparency:0.57000000000000006;;soilm(lon,lat) Style:Colormap; Palette:0=#00000000,#827DAAEC=300,#0000A0=800; Transparency:0; MapType:Aerial");
        ///  }
        /// </code>
        /// </example>
        /// </remarks>
        public static Variable Fetch(this DataSet ds, ClimateParameter parameter, string name, DateTime time, string nameUncertainty = null, string nameProvenance = null, EnvironmentalDataSource dataSource = EnvironmentalDataSource.ANY)
        {
            return Fetch(ds, ClimateService.ClimateParameterToFC2VariableName(parameter), name, time, nameUncertainty, nameProvenance, ClimateService.EnvironmentalDataSourceToArrayOfFC2DataSources(dataSource));
        }

        /// <summary>
        /// Creates new variable with data from the FetchClimate service for the current lat/lon/time coordinate system.
        /// </summary>
        /// <param name="ds">Target DataSet.</param>
        /// <param name="parameter">A climate parameter to fetch.</param>
        /// <param name="name">Name of the variable.</param>
        /// <returns>New variable instance.</returns>
        /// <remarks>
        /// <para>
        /// The method looks up the <paramref name="ds"/> for the lat/lon/time coordinate system.
        /// An axis is considered as a latitude grid if at least one of the following conditions are satisfied 
        /// (case is ignored in all rules):
        /// <list type="bullet">
        /// <item><description>axis name starts with either "lat" or "_lat";</description></item>
        /// <item><description>axis name contains substring "latitude";</description></item>
        /// <item><description>axis has attribute Units containing substring "degree" and ends with "n" or "north".</description></item>
        /// </list></para>
        /// <para>
        /// Similar rules for the longitude axis:
        /// <list type="bullet">
        /// <item><description>axis name starts with either "lon" or "_lon";</description></item>
        /// <item><description>axis name contains substring "longitude";</description></item>
        /// <item><description>axis has attribute Units containing substring "degree" and ends with "e" or "east".</description></item>
        /// </list></para>
        /// <para>
        /// Rules for the time axis:
        /// <list type="bullet">
        /// <item><description>axis name starts with "time".</description></item>
        /// </list>       
        /// </para>
        /// <para>If the axes not found, an exception is thrown.</para>
        /// <para>When a coordinate system is found, the Fetch Climate service is requested using a single batch request;
        /// result is added to the DataSet as 3d-variable depending on time,lat,lon axes.
        /// The DisplayName, long_name, Units, MissingValue and Provenance attributes of the variable are set.
        /// </para>
        /// <example>
        /// <code>
        ///  // Fetching climate parameters for fixed time moment
        ///  using (var ds = DataSet.Open("msds:memory"))
        ///  {
        ///      Console.WriteLine("Filling dataset...");
        ///      ds.AddAxis("lon", "degrees East", -12.5, 20.0, 0.5);
        ///      ds.AddAxis("lat", "degrees North", 35.0, 60.0, 0.5);
        ///      
        ///      ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt", new DateTime(2000, 7, 19, 11, 0, 0)); // time is fixed hence airt is 2d (depends on lat and lon)
        ///      ds.Fetch(ClimateParameter.FC_SOIL_MOISTURE, "soilm", new DateTime(2000, 7, 19, 11, 0, 0));
        ///
        ///      Console.WriteLine("Running DataSet Viewer...");
        ///      ds.View(@"airt(lon,lat) Style:Colormap; Palette:-5=Blue,White=0,#F4EF2F=5,Red=20; MapType:Aerial; Transparency:0.57000000000000006;;soilm(lon,lat) Style:Colormap; Palette:0=#00000000,#827DAAEC=300,#0000A0=800; Transparency:0; MapType:Aerial");
        ///  }
        ///
        ///  // Fetching climate parameters for different time moments
        ///  using (var ds = DataSet.Open("msds:memory"))
        ///  {
        ///      Console.WriteLine("Filling dataset...");
        ///      ds.AddAxis("lon", "degrees East", -12.5, 20.0, 2.0);
        ///      ds.AddAxis("lat", "degrees North", 35.0, 60.0, 2.0);
        ///      ds.AddAxis("time", new DateTime(2000, 7, 19, 0, 0, 0), new DateTime(2000, 7, 19, 23, 0, 0), TimeSpan.FromHours(2));
        ///
        ///      ds.Fetch(ClimateParameter.FC_TEMPERATURE, "airt"); // airt depends on lat,lon,time
        ///      ds.Fetch(ClimateParameter.FC_SOIL_MOISTURE, "soilm");
        ///
        ///      Console.WriteLine("Running DataSet Viewer...");
        ///      ds.View(@"airt(lon,lat) Style:Colormap; Palette:-5=Blue,White=0,#F4EF2F=5,Red=20; MapType:Aerial; Transparency:0.57000000000000006;;soilm(lon,lat) Style:Colormap; Palette:0=#00000000,#827DAAEC=300,#0000A0=800; Transparency:0; MapType:Aerial");
        ///  }
        /// </code>
        /// </example>
        /// </remarks>
        public static Variable Fetch(this DataSet ds, ClimateParameter parameter, string name, string nameUncertainty = null, string nameProvenance = null, EnvironmentalDataSource dataSource = EnvironmentalDataSource.ANY)
        {
            return Fetch(ds, ClimateService.ClimateParameterToFC2VariableName(parameter), name, nameUncertainty, nameProvenance, ClimateService.EnvironmentalDataSourceToArrayOfFC2DataSources(dataSource));
        }

        /// <summary>
        /// Creates new variable with data from the FetchClimate service for the current lat/lon coordinate system and given time.
        /// </summary>
        /// <param name="ds">Target DataSet.</param>
        /// <param name="parameter">A climate parameter to fetch.</param>
        /// <param name="name">Name of the variable.</param>
        /// <param name="lat">Name of the variable that is a latutude axis.</param>
        /// <param name="lon">Name of the variable that is a longitude axis.</param>
        /// <param name="time">Time moment to fetch data for.</param>
        /// <returns>New variable instance.</returns>
        /// <remarks>
        /// <para>
        /// The method allows to explicitly specify axes of the coordinate system for the new variable.
        /// See remarks for <see cref="Fetch(DataSet, ClimateParameter, string, DateTime)"/>
        /// </para>
        /// </remarks>
        public static Variable Fetch(this DataSet ds, ClimateParameter parameter, string name, string lat, string lon, DateTime time, string nameUncertainty = null, string nameProvenance = null, EnvironmentalDataSource dataSource = EnvironmentalDataSource.ANY)
        {
            return Fetch(ds, ClimateService.ClimateParameterToFC2VariableName(parameter), name, lat, lon, time, nameUncertainty, nameProvenance, ClimateService.EnvironmentalDataSourceToArrayOfFC2DataSources(dataSource));
        }

        /// <summary>
        /// Creates new variable with data from the FetchClimate service for the current lat/lon coordinate system and given time.
        /// </summary>
        /// <param name="ds">Target DataSet.</param>
        /// <param name="parameter">A climate parameter to fetch.</param>
        /// <param name="name">Name of the variable.</param>
        /// <param name="latID">ID of the variable that is a latutude axis.</param>
        /// <param name="lonID">ID of the variable that is a longitude axis.</param>
        /// <param name="time">Time moment to fetch data for.</param>
        /// <returns>New variable instance.</returns>
        /// <remarks>
        /// <para>
        /// The method allows to explicitly specify axes of the coordinate system for the new variable.
        /// See remarks for <see cref="Fetch(DataSet, ClimateParameter, string, DateTime)"/>
        /// </para>
        /// </remarks>
        public static Variable Fetch(this DataSet ds, ClimateParameter parameter, string name, int latID, int lonID, DateTime time, string nameUncertainty = null, string nameProvenance = null, EnvironmentalDataSource dataSource = EnvironmentalDataSource.ANY)
        {
            return Fetch(ds, ClimateService.ClimateParameterToFC2VariableName(parameter), name, latID, lonID, time, nameUncertainty, nameProvenance, ClimateService.EnvironmentalDataSourceToArrayOfFC2DataSources(dataSource));
        }

        /// <summary>
        /// Creates new variable with data from the FetchClimate service for the current lat/lon coordinate system and given time.
        /// </summary>
        /// <param name="ds">Target DataSet.</param>
        /// <param name="parameter">A climate parameter to fetch.</param>
        /// <param name="name">Name of the variable.</param>
        /// <param name="lat">Name of the variable that is a latutude axis.</param>
        /// <param name="lon">Name of the variable that is a longitude axis.</param>
        /// <param name="times">Name of the variable that is a time axis.</param>
        /// <returns>New variable instance.</returns>
        /// <remarks>
        /// <para>
        /// The method allows to explicitly specify axes of the coordinate system for the new variable.
        /// See remarks for <see cref="Fetch(DataSet, ClimateParameter, string)"/>
        /// </para>
        /// </remarks>
        public static Variable Fetch(this DataSet ds, ClimateParameter parameter, string name, string lat, string lon, string times, string nameUncertainty = null, string nameProvenance = null, EnvironmentalDataSource dataSource = EnvironmentalDataSource.ANY)
        {
            return Fetch(ds, ClimateService.ClimateParameterToFC2VariableName(parameter), name, lat, lon, times, nameUncertainty, nameProvenance, ClimateService.EnvironmentalDataSourceToArrayOfFC2DataSources(dataSource));
        }

        /// <summary>
        /// Creates new variable with data from the FetchClimate service for the current lat/lon coordinate system and given time.
        /// </summary>
        /// <param name="ds">Target DataSet.</param>
        /// <param name="parameter">A climate parameter to fetch.</param>
        /// <param name="name">Name of the variable.</param>
        /// <param name="latID">ID of the variable that is a latutude axis.</param>
        /// <param name="lonID">ID of the variable that is a longitude axis.</param>
        /// <param name="timeID">ID of the variable that is a time axis.</param>
        /// <returns>New variable instance.</returns>
        /// <remarks>
        /// <para>
        /// The method allows to explicitly specify axes of the coordinate system for the new variable.
        /// See remarks for <see cref="Fetch(DataSet, ClimateParameter, string, string, string)"/>
        /// </para>
        /// </remarks>
        public static Variable Fetch(this DataSet ds, ClimateParameter parameter, string name, int latID, int lonID, int timeID, string nameUncertainty = null, string nameProvenance = null, EnvironmentalDataSource dataSource = EnvironmentalDataSource.ANY)
        {
            return Fetch(ds, ClimateService.ClimateParameterToFC2VariableName(parameter), name, latID, lonID, timeID, nameUncertainty, nameProvenance, ClimateService.EnvironmentalDataSourceToArrayOfFC2DataSources(dataSource));
        }
    }
}
