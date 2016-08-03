using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Research.Science.Data.Utilities;
using Microsoft.Research.Science.Data.Climate;
using Microsoft.Research.Science.FetchClimate2;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.Data.Imperative
{
    /// <summary>
    /// Extends SDS Imperative API to enable fetching climate data.
    /// </summary>
    public static partial class DataSetFetchClimateExtensions
    {
        private static Dictionary<int, string> getDataSources()
        {
            var _dataSources = new Dictionary<int, string>();
            _dataSources.Add(65535, "");
            foreach (var i in ClimateService.Configuration.DataSources) _dataSources.Add(i.ID, i.Name);
            return _dataSources;
        }

        #region AddAxisCells related
        /// <summary>
        /// Creates new axis variable of type double and and adds it to the dataset. The values of the variable correspond to centers of the cell. Also the method adds another variable depicting cells bounds according to CF conventions 1.5
        /// </summary>
        /// <param name="ds">Target datas set</param>
        /// <param name="name">Name of the axis</param>
        /// <param name="units">Units of measurement of values of the axis</param>
        /// <param name="min">The lower bound of the first cell</param>
        /// <param name="max">The upper bound of the last cell</param>
        /// <param name="delta">The size of cells</param>
        /// <param name="boundsVariableName">A name of varaible containing bounds of the cell. If it is omited the name will be chosen automaticly</param>
        /// <returns>New axis variable</returns>
        public static Variable AddAxisCells(this DataSet ds, string name, string units, double min, double max, double delta, string boundsVariableName = null)
        {
            var names = EnsureAddAxisCellsNames(ds, name, boundsVariableName);

            var axis = ds.AddAxis(name, units, min + delta / 2, max - delta / 2, delta);
            axis.Metadata["bounds"] = names.Item1;
            int n = axis.Dimensions[0].Length;
            double[,] boundsData = new double[n, 2];
            for (int i = 0; i < n; i++)
            {
                boundsData[i, 0] = min + delta * i;
                boundsData[i, 1] = min + delta * (i + 1);
            }
            ds.AddVariable<double>(names.Item1, boundsData, axis.Dimensions[0].Name, names.Item2);

            return axis;

        }

        /// <summary>
        /// Creates new axis variable of type float and and adds it to the dataset. The values of the variable correspond to centers of the cell. Also the method adds another variable depicting cells bounds according to CF conventions 1.5
        /// </summary>
        /// <param name="ds">Target datas set</param>
        /// <param name="name">Name of the axis</param>
        /// <param name="units">Units of measurement of values of the axis</param>
        /// <param name="min">The lower bound of the first cell</param>
        /// <param name="max">The upper bound of the last cell</param>
        /// <param name="delta">The size of cells</param>
        /// <param name="boundsVariableName">A name of varaible containing bounds of the cell. If it is omited the name will be chosen automaticly</param>
        /// <returns>New axis variable</returns>
        public static Variable AddAxisCells(this DataSet ds, string name, string units, float min, float max, float delta, string boundsVariableName = null)
        {
            var names = EnsureAddAxisCellsNames(ds, name, boundsVariableName);

            var axis = ds.AddAxis(name, units, min + delta / 2, max - delta / 2, delta);
            axis.Metadata["bounds"] = names.Item1;
            int n = axis.Dimensions[0].Length;
            float[,] boundsData = new float[n, 2];
            for (int i = 0; i < n; i++)
            {
                boundsData[i, 0] = min + delta * i;
                boundsData[i, 1] = min + delta * (i + 1);
            }
            ds.AddVariable<float>(names.Item1, boundsData, axis.Dimensions[0].Name, names.Item2);

            return axis;

        }
        /// <summary>
        /// Returns a name for the CellBoundsVariable * ServiceDimensionForCellBoundsVar
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="name"></param>
        /// <param name="boundsVariableName"></param>
        /// <returns></returns>
        private static Tuple<string, string> EnsureAddAxisCellsNames(DataSet ds, string name, string boundsVariableName)
        {
            string boundsVarName = boundsVariableName;
            if (boundsVarName == null)
            {
                boundsVarName = string.Format("{0}_bnds", name);
                if (ds.Variables.Contains(boundsVarName))
                {
                    int tryNum = 1;
                    while (ds.Variables.Contains(string.Format("{0}_bnds{1}", boundsVarName, tryNum)))
                        tryNum++;
                    boundsVarName = string.Format("{0}_bnds{1}", boundsVarName, tryNum);
                }
            }

            if (ds.Variables.Contains(boundsVarName))
                throw new ArgumentException(string.Format("The dataset is already contains a variable with a name {0}. Please specyfy another one or omit it for automatic name choice", boundsVarName));

            string sndDim = "nv";
            if (ds.Dimensions.Contains(sndDim) && ds.Dimensions[sndDim].Length != 2)
            {
                int num = 1;
                while (ds.Dimensions.Contains(string.Format("{0}{1}", sndDim, num)))
                    num++;
                sndDim = string.Format("{0}{1}", sndDim, num);
            }
            return Tuple.Create(boundsVarName, sndDim);
        }

        #endregion

        /// <summary>
        /// Creates new variable with data from the FetchClimate service for the current lat/lon coordinate system and given time.
        /// </summary>
        /// <param name="ds">Target DataSet.</param>
        /// <param name="fcVariableName">Name of the FC2 variable to fetch.</param>
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
        ///      ds.Fetch("airt", "airt", new DateTime(2000, 7, 19, 11, 0, 0)); // time is fixed hence airt is 2d (depends on lat and lon)
        ///      ds.Fetch("soilmoist", "soilm", new DateTime(2000, 7, 19, 11, 0, 0));
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
        ///      ds.Fetch("airt", "airt"); // airt depends on lat,lon,time
        ///      ds.Fetch("soilmoist", "soilm");
        ///
        ///      Console.WriteLine("Running DataSet Viewer...");
        ///      ds.View(@"airt(lon,lat) Style:Colormap; Palette:-5=Blue,White=0,#F4EF2F=5,Red=20; MapType:Aerial; Transparency:0.57000000000000006;;soilm(lon,lat) Style:Colormap; Palette:0=#00000000,#827DAAEC=300,#0000A0=800; Transparency:0; MapType:Aerial");
        ///  }
        /// </code>
        /// </example>
        /// </remarks>
        public static Variable Fetch(this DataSet ds, string fcVariableName, string name, DateTime time, string nameUncertainty = null, string nameProvenance = null, string[] dataSources = null)
        {
            Variable lat = null, lon = null;
            var axisVars = GetDefaultCoordinateSystem(ds);
            if (axisVars.Length > 1)
            {
                for (int i = 0; i < axisVars.Length; i++)
                {
                    var var = axisVars[i];
                    if (lat == null && GeoConventions.IsLatitude(var))
                        lat = var;
                    else if (lon == null && GeoConventions.IsLongitude(var))
                        lon = var;
                }
            }
            else
            {//looking for pointset
                var vars = ds.Variables.Where(v => v.Rank == 1).ToArray();

                for (int i = 0; i < vars.Length; i++)
                {
                    if (!GeoConventions.IsLatitude(vars[i]))
                        continue;
                    lat = vars[i];
                    foreach (Variable posLon in vars.Where((u, j) => j != i))
                        if (GeoConventions.IsLongitude(posLon))
                        {
                            lon = posLon;
                            break;
                        }
                }
            }
            return Fetch(ds, fcVariableName, name, lat, lon, time, nameUncertainty: nameUncertainty, nameProvenance: nameProvenance, dataSources: dataSources);
        }

        /// <summary>
        /// Creates new variable with data from the FetchClimate service for the current lat/lon/time coordinate system.
        /// </summary>
        /// <param name="ds">Target DataSet.</param>
        /// <param name="fcVariableName">Name of the FC2 variable to fetch.</param>
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
        ///      ds.Fetch("airt", "airt", new DateTime(2000, 7, 19, 11, 0, 0)); // time is fixed hence airt is 2d (depends on lat and lon)
        ///      ds.Fetch("soilmoist", "soilm", new DateTime(2000, 7, 19, 11, 0, 0));
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
        ///      ds.Fetch("airt", "airt"); // airt depends on lat,lon,time
        ///      ds.Fetch("soilmoist", "soilm");
        ///
        ///      Console.WriteLine("Running DataSet Viewer...");
        ///      ds.View(@"airt(lon,lat) Style:Colormap; Palette:-5=Blue,White=0,#F4EF2F=5,Red=20; MapType:Aerial; Transparency:0.57000000000000006;;soilm(lon,lat) Style:Colormap; Palette:0=#00000000,#827DAAEC=300,#0000A0=800; Transparency:0; MapType:Aerial");
        ///  }
        /// </code>
        /// </example>
        /// </remarks>
        public static Variable Fetch(this DataSet ds, string fcVariableName, string name, string nameUncertainty = null, string nameProvenance = null, string[] dataSources = null)
        {
            Variable lat = null, lon = null, times = null;
            TimeBounds[] climBounds = GetClimatologyBounds(ds);
            var axisVars = GetDefaultCoordinateSystem(ds);
            if (axisVars.Length > 1)
            {
                for (int i = 0; i < axisVars.Length; i++)
                {
                    var var = axisVars[i];
                    if (lat == null && GeoConventions.IsLatitude(var))
                        lat = var;
                    else if (lon == null && GeoConventions.IsLongitude(var))
                        lon = var;
                    else if (times == null && IsTimes(var))
                        times = var;
                }
            }
            else
            {//looking for pointset
                var vars = ds.Variables.Where(v => v.Rank == 1).ToArray();

                for (int i = 0; i < vars.Length; i++)
                {
                    if (times == null && IsTimes(vars[i]))
                    {
                        times = vars[i];
                        continue;
                    }
                    if (!GeoConventions.IsLatitude(vars[i]))
                        continue;
                    lat = vars[i];
                    foreach (Variable posLon in vars.Take(i).Union(vars.Skip(i + 1)))
                        if (GeoConventions.IsLongitude(posLon))
                        {
                            lon = posLon;
                            break;
                        }
                }
            }
            if (times == null)
                throw new InvalidOperationException("The dataset doesn't contain a time information. Please add time axis and climatology bounds if needed");
            if (lat == null)
                throw new InvalidOperationException("The dataset doesn't contain a latitude information. Please add latitude axis to the dataset beforehand");
            if (lon == null)
                throw new InvalidOperationException("The dataset doesn't contain a longitude information. Please add longitude axis to the dataset beforehand");
            if (climBounds.Length == 0)
                return Fetch(ds, fcVariableName, name, lat, lon, times, nameUncertainty, nameProvenance, dataSources);
            else
                return Fetch(ds, fcVariableName, name, lat, lon, times, nameUncertainty, nameProvenance, dataSources, climatologyBounds: climBounds);
        }

        /// <summary>
        /// Creates new variable with data from the FetchClimate service for the current lat/lon coordinate system and given time.
        /// </summary>
        /// <param name="ds">Target DataSet.</param>
        /// <param name="fcVariableName">Name of the FC2 variable to fetch.</param>
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
        public static Variable Fetch(this DataSet ds, string fcVariableName, string name, string lat, string lon, DateTime time, string nameUncertainty = null, string nameProvenance = null, string[] dataSources = null)
        {
            return Fetch(ds, fcVariableName, name, ds[lat], ds[lon], time, nameUncertainty, nameProvenance, dataSources);
        }

        /// <summary>
        /// Creates new variable with data from the FetchClimate service for the current lat/lon coordinate system and given time.
        /// </summary>
        /// <param name="ds">Target DataSet.</param>
        /// <param name="fcVariableName">Name of the FC2 variable to fetch.</param>
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
        public static Variable Fetch(this DataSet ds, string fcVariableName, string name, int latID, int lonID, DateTime time, string nameUncertainty = null, string nameProvenance = null, string[] dataSources = null)
        {
            return Fetch(ds, fcVariableName, name, ds.Variables.GetByID(latID), ds.Variables.GetByID(lonID), time, nameUncertainty, nameProvenance, dataSources);
        }
        /// <summary>
        /// Creates new variable with data from the FetchClimate service for the current lat/lon coordinate system and given time.
        /// </summary>
        /// <param name="ds">Target DataSet.</param>
        /// <param name="fcVariableName">Name of the FC2 variable to fetch.</param>
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
        public static Variable Fetch(this DataSet ds, string fcVariableName, string name, string lat, string lon, string times, string nameUncertainty = null, string nameProvenance = null, string[] dataSources = null)
        {
            TimeBounds[] tb = GetClimatologyBounds(ds, ds[times].ID);
            return Fetch(ds, fcVariableName, name, ds[lat], ds[lon], ds[times], nameUncertainty, nameProvenance, dataSources, tb.Length == 0 ? null : tb);
        }
        /// <summary>
        /// Creates new variable with data from the FetchClimate service for the current lat/lon coordinate system and given time.
        /// </summary>
        /// <param name="ds">Target DataSet.</param>
        /// <param name="fcVariableName">Name of the FC2 variable to fetch.</param>
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
        public static Variable Fetch(this DataSet ds, string fcVariableName, string name, int latID, int lonID, int timeID, string nameUncertainty = null, string nameProvenance = null, string[] dataSources = null)
        {
            TimeBounds[] tb = GetClimatologyBounds(ds, timeID);
            if (tb.Length == 0) tb = null;
            return Fetch(ds, fcVariableName, name, ds.Variables.GetByID(latID), ds.Variables.GetByID(lonID), ds.Variables.GetByID(timeID), climatologyBounds: tb, nameUncertainty: nameUncertainty, nameProvenance: nameProvenance, dataSources: dataSources);
        }

        private static Variable Fetch(this DataSet ds, string fcVariableName, string name, Variable lat, Variable lon, DateTime time, string nameUncertainty, string nameProvenance, string[] dataSources = null)
        {
            if (ds == null) throw new ArgumentNullException("ds");
            if (String.IsNullOrWhiteSpace(name)) throw new ArgumentException("name is incorrect");
            if (lat == null) throw new ArgumentNullException("lat");
            if (lon == null) throw new ArgumentNullException("lon");
            if (lat.Rank != 1) throw new ArgumentException("lat is not one-dimensional");
            if (lon.Rank != 1) throw new ArgumentException("lon is not one-dimensional");

            DataRequest[] req = new DataRequest[2];
            req[0] = DataRequest.GetData(lat);
            req[1] = DataRequest.GetData(lon);
            var resp = ds.GetMultipleData(req);

            double[] _lats = GetDoubles(resp[lat.ID].Data);
            double[] _lons = GetDoubles(resp[lon.ID].Data);
            return Fetch(ds, fcVariableName, name, _lats, _lons, lat.Dimensions[0].Name, lon.Dimensions[0].Name, nameUncertainty: nameUncertainty, nameProvenance: nameProvenance, dataSources: dataSources, timeSlices: new DateTime[] { time });
        }

        private static Variable Fetch(this DataSet ds, string fcVariableName, string name, Variable lat, Variable lon, Variable timeSlices, string nameUncertainty, string nameProvenance, string[] dataSources = null, TimeBounds[] climatologyBounds = null)
        {
            if (ds == null) throw new ArgumentNullException("ds");
            if (String.IsNullOrWhiteSpace(name)) throw new ArgumentException("name is incorrect");
            if (lat == null) throw new ArgumentNullException("lat");
            if (lon == null) throw new ArgumentNullException("lon");
            if (timeSlices == null) throw new ArgumentNullException("timeSlices");
            if (lat.Rank != 1) throw new ArgumentException("lat is not one-dimensional");
            if (lon.Rank != 1) throw new ArgumentException("lon is not one-dimensional");
            if (timeSlices.Rank != 1) throw new ArgumentException("time is not one-dimensional");

            DataRequest[] req = null;
            MultipleDataResponse resp = null;
            if (climatologyBounds == null)
            {
                req = new DataRequest[3];
                req[0] = DataRequest.GetData(lat);
                req[1] = DataRequest.GetData(lon);
                req[2] = DataRequest.GetData(timeSlices);
                resp = ds.GetMultipleData(req);
            }
            else
            {
                req = new DataRequest[2];
                req[0] = DataRequest.GetData(lat);
                req[1] = DataRequest.GetData(lon);
                resp = ds.GetMultipleData(req);
            }

            double[] _latmaxs = null;
            double[] _lonmaxs = null;
            double[] _latmins = null;
            double[] _lonmins = null;
            if (lat.Metadata.ContainsKey("bounds") && lon.Metadata.ContainsKey("bounds")) //case of cells
            {
                Variable latBounds = ds.Variables[(string)lat.Metadata["bounds"]];
                Variable lonBounds = ds.Variables[(string)lon.Metadata["bounds"]];
                if (latBounds.Rank == 2 && lonBounds.Rank == 2
                    && lonBounds.Dimensions[0].Name == lon.Dimensions[0].Name
                    && latBounds.Dimensions[0].Name == lat.Dimensions[0].Name)
                {
                    Array latBoundsData = latBounds.GetData();
                    Array lonBoundsData = lonBounds.GetData();
                    int dimLatLen = latBounds.Dimensions[0].Length;
                    int dimLonLen = lonBounds.Dimensions[0].Length;
                    _latmins = new double[dimLatLen];
                    _latmaxs = new double[dimLatLen];
                    _lonmins = new double[dimLonLen];
                    _lonmaxs = new double[dimLonLen];
                    for (int i = 0; i < dimLatLen; i++)
                    {
                        _latmins[i] = Convert.ToDouble(latBoundsData.GetValue(i, 0));
                        _latmaxs[i] = Convert.ToDouble(latBoundsData.GetValue(i, 1));
                    }
                    for (int i = 0; i < dimLonLen; i++)
                    {
                        _lonmins[i] = Convert.ToDouble(lonBoundsData.GetValue(i, 0));
                        _lonmaxs[i] = Convert.ToDouble(lonBoundsData.GetValue(i, 1));
                    }
                }
            }
            if (_latmins == null || _lonmins == null) //case of grid without cells
            {
                _latmins = GetDoubles(resp[lat.ID].Data);
                _lonmins = GetDoubles(resp[lon.ID].Data);
            }
            DateTime[] _times = null;
            if (climatologyBounds == null)
                _times = (DateTime[])resp[timeSlices.ID].Data;

            if (climatologyBounds != null)
                return Fetch(ds, fcVariableName, name, _latmins, _lonmins, lat.Dimensions[0].Name, lon.Dimensions[0].Name, dimTime: timeSlices.Dimensions[0].Name, latmaxs: _latmaxs, lonmaxs: _lonmaxs, climatologyIntervals: climatologyBounds, nameUncertainty: nameUncertainty, nameProvenance: nameProvenance, dataSources: dataSources);
            else
                return Fetch(ds, fcVariableName, name, _latmins, _lonmins, lat.Dimensions[0].Name, lon.Dimensions[0].Name, dimTime: timeSlices.Dimensions[0].Name, latmaxs: _latmaxs, lonmaxs: _lonmaxs, timeSlices: _times, nameUncertainty: nameUncertainty, nameProvenance: nameProvenance, dataSources: dataSources);
        }

        private static Variable Fetch(this DataSet ds, string fcVariableName, string name, double[] latmins, double[] lonmins, string dimLat, string dimLon, double[] latmaxs = null, double[] lonmaxs = null, string dimTime = null, DateTime[] timeSlices = null, TimeBounds[] climatologyIntervals = null, string nameUncertainty = null, string nameProvenance = null, string[] dataSources = null)
        {
            if (timeSlices == null && climatologyIntervals == null)
                throw new ArgumentNullException("Both timeSlices and ClimatologyIntervals are null");
            if (latmaxs == null ^ lonmaxs == null)
                throw new ArgumentException("Only one of latmax and lonmax is set. please set both of them or none");
            object mv = double.NaN;
            string longName = GetLongName(fcVariableName);

            bool isFetchingGrid = dimLat != dimLon; // otherwise, fetching point set, when all axes depend on the same dimension

            // Preparing FetchClimate method parameters

            int timesN = (climatologyIntervals != null) ? (climatologyIntervals.Length) : (timeSlices.Length);

            //Build time region
            TimeRegion[] trs = new TimeRegion[0];
            bool needIndividualTrs = false;
            if (climatologyIntervals != null)
            {
                bool isDayMinConst = climatologyIntervals.All(x => x.MinDay == climatologyIntervals[0].MinDay);
                bool isDayMaxConst = climatologyIntervals.All(x => x.MaxDay == climatologyIntervals[0].MaxDay);
                bool isHourMinConst = climatologyIntervals.All(x => x.MinHour == climatologyIntervals[0].MinHour);
                bool isHourMaxConst = climatologyIntervals.All(x => x.MaxHour == climatologyIntervals[0].MaxHour);
                bool isYearMinConst = climatologyIntervals.All(x => x.MinYear == climatologyIntervals[0].MinYear);
                bool isYearMaxConst = climatologyIntervals.All(x => x.MaxYear == climatologyIntervals[0].MaxYear);
                if (isDayMinConst && isDayMaxConst && isHourMinConst && isHourMaxConst)
                {
                    //may be yearly timeseries
                    for (int i = 1; i < climatologyIntervals.Length; ++i) if (climatologyIntervals[i].MinYear != climatologyIntervals[i - 1].MaxYear + 1) needIndividualTrs = true;
                    if (!needIndividualTrs)
                    {
                        trs = new TimeRegion[] { new TimeRegion(climatologyIntervals.Select(x => x.MinYear).Concat(new int[] {climatologyIntervals[climatologyIntervals.Length - 1].MaxYear + 1 }).ToArray(),
                            new int[] { climatologyIntervals[0].MinDay, climatologyIntervals[0].MaxDay + 1 }, new int[] { climatologyIntervals[0].MinHour, climatologyIntervals[0].MaxHour }) };
                    }
                }
                else if (isDayMinConst && isDayMaxConst && isYearMinConst && isYearMaxConst)
                {
                    //may be hourly timeseries
                    for (int i = 1; i < climatologyIntervals.Length; ++i) if (climatologyIntervals[i].MinHour != climatologyIntervals[i - 1].MaxHour + 1) needIndividualTrs = true;
                    if (!needIndividualTrs)
                    {
                        trs = new TimeRegion[] { new TimeRegion(new int[] { climatologyIntervals[0].MinYear, climatologyIntervals[0].MaxYear + 1 }, new int[] { climatologyIntervals[0].MinDay,
                            climatologyIntervals[0].MaxDay + 1 }, climatologyIntervals.Select(x => x.MinHour).Concat(new int[] { climatologyIntervals[climatologyIntervals.Length - 1].MaxHour }).ToArray()) };
                    }
                }
                else if (isHourMinConst && isHourMaxConst && isYearMinConst && isYearMaxConst)
                {
                    //may be seasonly timeseries
                    for (int i = 1; i < climatologyIntervals.Length; ++i) if (climatologyIntervals[i].MinDay != climatologyIntervals[i - 1].MaxDay + 1) needIndividualTrs = true;
                    if (!needIndividualTrs)
                    {
                        trs = new TimeRegion[] { new TimeRegion(new int[] { climatologyIntervals[0].MinYear, climatologyIntervals[0].MaxYear + 1 },
                            climatologyIntervals.Select(x => x.MinDay).Concat(new int[] { climatologyIntervals[climatologyIntervals.Length - 1].MaxDay + 1 }).ToArray(),
                            new int[] { climatologyIntervals[0].MinHour, climatologyIntervals[0].MaxHour } ) };
                    }
                }
                else
                    needIndividualTrs = true;

                if (needIndividualTrs)
                {
                    trs = new TimeRegion[timesN];
                    for (int i = 0; i < timesN; ++i)
                    {
                        trs[i] = new TimeRegion(climatologyIntervals[i].MinYear, climatologyIntervals[i].MaxYear, climatologyIntervals[i].MinDay,
                            climatologyIntervals[i].MaxDay, climatologyIntervals[i].MinHour, climatologyIntervals[i].MaxHour);
                    }
                }
            }
            else
            {
                bool isDaysConst = timeSlices.All(x => x.DayOfYear == timeSlices[0].DayOfYear);
                bool isHoursConst = timeSlices.All(x => x.Hour == timeSlices[0].Hour);
                bool isYearsConst = timeSlices.All(x => x.Year == timeSlices[0].Year);
                if (isDaysConst && isHoursConst)
                {
                    //yearly series
                    trs = new TimeRegion[] { new TimeRegion(timeSlices.Select(x => x.Year).ToArray(), new int[] { timeSlices[0].DayOfYear }, new int[] { timeSlices[0].Hour },
                        false, false, false) };
                }
                else if (isDaysConst && isYearsConst)
                {
                    //hourly timeseries
                    trs = new TimeRegion[] { new TimeRegion(new int[] { timeSlices[0].Year }, new int[] { timeSlices[0].DayOfYear }, timeSlices.Select(x => x.Hour).ToArray(),
                        false, false, false) };
                }
                else if (isHoursConst && isYearsConst)
                {
                    //seasonly timeseries
                    trs = new TimeRegion[] { new TimeRegion(new int[] { timeSlices[0].Year }, timeSlices.Select(x => x.DayOfYear).ToArray(), new int[] { timeSlices[0].Hour },
                        false, false, false) };
                }
                else
                {
                    needIndividualTrs = true;
                    trs = new TimeRegion[timesN];
                    for (int i = 0; i < timesN; ++i)
                    {
                        trs[i] = new TimeRegion(new int[] { timeSlices[i].Year }, new int[] { timeSlices[i].DayOfYear }, new int[] { timeSlices[i].Hour },
                            false, false, false);
                    }
                }
            }

            //analyze spatial region type
            SpatialRegionSpecification spatialType;
            if (isFetchingGrid)
            {
                if (latmaxs == null)
                {
                    spatialType = SpatialRegionSpecification.PointGrid;
                }
                else
                {
                    bool isGrid = true;
                    for (int i = 1; i < latmins.Length; ++i) if (latmins[i] != latmaxs[i - 1]) isGrid = false;
                    for (int i = 1; i < lonmins.Length; ++i) if (lonmins[i] != lonmaxs[i - 1]) isGrid = false;
                    if (isGrid)
                        spatialType = SpatialRegionSpecification.CellGrid;
                    else
                        spatialType = SpatialRegionSpecification.Cells;
                }
            }
            else
                spatialType = SpatialRegionSpecification.Points;

            FetchRequest[] requests = new FetchRequest[trs.Length];

            if (spatialType == SpatialRegionSpecification.Points)
            {
                for (int i = 0; i < trs.Length; ++i)
                    requests[i] = new FetchRequest(fcVariableName, FetchDomain.CreatePoints(latmins, lonmins, trs[i]), dataSources);
            }
            else if (spatialType == SpatialRegionSpecification.PointGrid)
            {
                for (int i = 0; i < trs.Length; ++i)
                    requests[i] = new FetchRequest(fcVariableName, FetchDomain.CreatePointGrid(latmins, lonmins, trs[i]), dataSources);
            }
            else if (spatialType == SpatialRegionSpecification.CellGrid)
            {
                double[] lats = latmins.Concat(new double[] { latmaxs[latmaxs.Length - 1] }).ToArray();
                double[] lons = lonmins.Concat(new double[] { lonmaxs[lonmaxs.Length - 1] }).ToArray();
                for (int i = 0; i < trs.Length; ++i)
                    requests[i] = new FetchRequest(fcVariableName, FetchDomain.CreateCellGrid(lats, lons, trs[i]), dataSources);
            }
            else
            {
                int cellsN = latmins.Length * lonmins.Length;
                double[] lats = new double[cellsN], lats2 = new double[cellsN], lons = new double[cellsN], lons2 = new double[cellsN];
                int index = 0;
                for (int i = 0; i < latmins.Length; ++i)
                    for (int j = 0; j < lonmins.Length; ++j)
                    {
                        lats[index] = latmins[i];
                        lats2[index] = latmaxs[i];
                        lons[index] = lonmins[j];
                        lons2[index] = lonmaxs[j];
                        ++index;
                    }
                for (int i = 0; i < trs.Length; ++i)
                    requests[i] = new FetchRequest(fcVariableName, FetchDomain.CreateCells(lats, lons, lats2, lons2, trs[i]), dataSources);
            }

            // Fetching the data
            Task<DataSet>[] resultTasks = new Task<DataSet>[trs.Length];

            for (int i = 0; i < trs.Length; ++i) resultTasks[i] = ClimateService.FetchAsync(requests[i]);

            DataSet[] results = new DataSet[trs.Length];

            for (int i = 0; i < trs.Length; ++i) results[i] = resultTasks[i].Result;

            // Saving result in the dataset

            Variable varData = null, varUncertainty = null, varProv = null;
            bool saveUncertainty = !String.IsNullOrWhiteSpace(nameUncertainty);
            bool saveProv = !String.IsNullOrWhiteSpace(nameProvenance);
            string units = ClimateService.Configuration.EnvironmentalVariables.First(x => x.Name == fcVariableName).Units;
            bool isDataSourceSingle = dataSources != null && dataSources.Length == 1;
            var sources = getDataSources();
            string[] usedDataSources;
            HashSet<int> usedDataSourceIds = new HashSet<int>();

            if (spatialType == SpatialRegionSpecification.Points)
            {
                if (dimTime != null)
                {
                    double[,] data = new double[timesN, latmins.Length];
                    double[,] uncertainty = saveUncertainty ? new double[timesN, latmins.Length] : null;
                    string[,] provenance = saveProv ? new string[timesN, latmins.Length] : null;
                    if (needIndividualTrs || timesN == 1)
                    {
                        for (int i = 0; i < trs.Length; ++i)
                        {
                            double[] line = (double[])results[i]["values"].GetData();
                            for (int j = 0; j < latmins.Length; ++j)
                                data[i, j] = line[j];
                            if (saveUncertainty)
                            {
                                double[] lineUnc = (double[])results[i]["sd"].GetData();
                                for (int j = 0; j < latmins.Length; ++j)
                                    uncertainty[i, j] = lineUnc[j];
                            }
                            if (!isDataSourceSingle)
                            {
                                UInt16[] lineProv = (UInt16[])results[i]["provenance"].GetData();
                                foreach (var t in lineProv) usedDataSourceIds.Add(t);
                                if (saveProv)
                                {
                                    for (int j = 0; j < latmins.Length; ++j)
                                        provenance[i, j] = sources[lineProv[j]];
                                }
                            }
                            else if (saveProv)
                            {
                                for (int j = 0; j < latmins.Length; ++j)
                                    provenance[i, j] = dataSources[0];
                            }
                        }
                    }
                    else
                    {
                        double[,] tempData = (double[,])results[0]["values"].GetData();
                        for (int i = 0; i < timesN; ++i)
                            for (int j = 0; j < latmins.Length; ++j)
                                data[i, j] = tempData[j, i];
                        if (saveUncertainty)
                        {
                            double[,] lineUnc = (double[,])results[0]["sd"].GetData();
                            for (int i = 0; i < timesN; ++i)
                                for (int j = 0; j < latmins.Length; ++j)
                                    uncertainty[i, j] = lineUnc[j, i];
                        }
                        if (!isDataSourceSingle)
                        {
                            UInt16[,] lineProv = (UInt16[,])results[0]["provenance"].GetData();
                            foreach (var t in lineProv) usedDataSourceIds.Add(t);
                            if (saveProv)
                            {
                                for (int i = 0; i < timesN; ++i)
                                    for (int j = 0; j < latmins.Length; ++j)
                                        provenance[i, j] = sources[lineProv[j, i]];
                            }
                        }
                        else if (saveProv)
                        {
                            for (int i = 0; i < timesN; ++i)
                                for (int j = 0; j < latmins.Length; ++j)
                                    provenance[i, j] = dataSources[0];
                        }
                    }
                    varData = ds.Add(name, units, mv, data, dimTime, dimLat);
                    if (saveUncertainty) varUncertainty = ds.Add(nameUncertainty, units, mv, uncertainty, dimTime, dimLat);
                    if (saveProv) varProv = ds.Add(nameProvenance, provenance, dimTime, dimLat);
                }
                else
                {
                    double[] data = (double[])results[0]["values"].GetData(); //new double[latmins.Length];
                    double[] uncertainty = saveUncertainty ? (double[])results[0]["sd"].GetData()/*new double[latmins.Length]*/ : null;
                    string[] provenance = saveProv ? new string[latmins.Length] : null;
                    if (!isDataSourceSingle)
                    {
                        UInt16[] lineProv = (UInt16[])results[0]["provenance"].GetData();
                        foreach (var t in lineProv) usedDataSourceIds.Add(t);
                        if (saveProv)
                        {
                            for (int j = 0; j < latmins.Length; ++j)
                                provenance[j] = sources[lineProv[j]];
                        }
                    }
                    else if (saveProv)
                    {
                        for (int j = 0; j < latmins.Length; ++j)
                            provenance[j] = dataSources[0];
                    }

                    varData = ds.Add(name, units, mv, data, dimLat);
                    varData.Metadata["Time"] = timeSlices[0];
                    if (saveUncertainty)
                    {
                        varUncertainty = ds.Add(nameUncertainty, units, mv, uncertainty, dimLat);
                        varUncertainty.Metadata["Time"] = timeSlices[0];
                    }
                    if (saveProv)
                    {
                        varProv = ds.Add(nameProvenance, provenance, dimLat);
                        varProv.Metadata["Time"] = timeSlices[0];
                    }
                }
            }
            else
            {
                if (dimTime != null)
                {
                    double[, ,] data = new double[timesN, latmins.Length, lonmins.Length];
                    double[, ,] uncertainty = saveUncertainty ? new double[timesN, latmins.Length, lonmins.Length] : null;
                    string[, ,] provenance = saveProv ? new string[timesN, latmins.Length, lonmins.Length] : null;
                    
                    if (spatialType == SpatialRegionSpecification.Cells)
                    {
                        if (needIndividualTrs || timesN == 1)
                        {
                            for (int i = 0; i < trs.Length; ++i)
                            {
                                double[] slice = (double[])results[i]["values"].GetData();
                                int index = 0;
                                for (int j = 0; j < latmins.Length; ++j)
                                    for (int k = 0; k < lonmins.Length; ++k)
                                    {
                                        data[i, j, k] = slice[index];
                                        ++index;
                                    }
                                if (saveUncertainty)
                                {
                                    double[] sliceUnc = (double[])results[i]["sd"].GetData();
                                    index = 0;
                                    for (int j = 0; j < latmins.Length; ++j)
                                        for (int k = 0; k < lonmins.Length; ++k)
                                        {
                                            uncertainty[i, j, k] = sliceUnc[index];
                                            ++index;
                                        }
                                }
                                if (!isDataSourceSingle)
                                {
                                    UInt16[] lineProv = (UInt16[])results[i]["provenance"].GetData();
                                    foreach (var t in lineProv) usedDataSourceIds.Add(t);
                                    if (saveProv)
                                    {
                                        index = 0;
                                        for (int j = 0; j < latmins.Length; ++j)
                                            for (int k = 0; k < lonmins.Length; ++k)
                                            {
                                                provenance[i, j, k] = sources[lineProv[index]];
                                                ++index;
                                            }
                                    }
                                }
                                else if (saveProv)
                                {
                                    for (int j = 0; j < latmins.Length; ++j)
                                        for (int k = 0; k < lonmins.Length; ++k)
                                        {
                                            provenance[i, j, k] = dataSources[0];
                                        }
                                }
                            }
                        }
                        else
                        {
                            double[,] tempData = (double[,])results[0]["values"].GetData();
                            int index = 0;
                            for (int i = 0; i < timesN; ++i)
                            {
                                index = 0;
                                for (int j = 0; j < latmins.Length; ++j)
                                    for (int k = 0; k < lonmins.Length; ++k)
                                    {
                                        data[i, j, k] = tempData[index, i];
                                        ++index;
                                    }
                            }
                            if (saveUncertainty)
                            {
                                double[,] lineUnc = (double[,])results[0]["sd"].GetData();
                                index = 0;
                                for (int i = 0; i < timesN; ++i)
                                {
                                    index = 0;
                                    for (int j = 0; j < latmins.Length; ++j)
                                        for (int k = 0; k < lonmins.Length; ++k)
                                        {
                                            uncertainty[i, j, k] = lineUnc[index, i];
                                            ++index;
                                        }
                                }
                            }
                            if (!isDataSourceSingle)
                            {
                                UInt16[,] lineProv = (UInt16[,])results[0]["provenance"].GetData();
                                foreach (var t in lineProv) usedDataSourceIds.Add(t);
                                if (saveProv)
                                {
                                    index = 0;
                                    for (int i = 0; i < timesN; ++i)
                                    {
                                        index = 0;
                                        for (int j = 0; j < latmins.Length; ++j)
                                            for (int k = 0; k < lonmins.Length; ++k)
                                            {
                                                provenance[i, j, k] = sources[lineProv[index, i]];
                                                ++index;
                                            }
                                    }
                                }
                            }
                            else if (saveProv)
                            {
                                for (int i = 0; i < timesN; ++i)
                                {
                                    for (int j = 0; j < latmins.Length; ++j)
                                        for (int k = 0; k < lonmins.Length; ++k)
                                        {
                                            provenance[i, j, k] = dataSources[0];
                                        }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (needIndividualTrs || timesN == 1)
                        {
                            for (int i = 0; i < trs.Length; ++i)
                            {
                                double[,] slice = (double[,])results[i]["values"].GetData();
                                for (int j = 0; j < latmins.Length; ++j)
                                    for (int k = 0; k < lonmins.Length; ++k)
                                        data[i, j, k] = slice[k, j];
                                if (saveUncertainty)
                                {
                                    double[,] lineUnc = (double[,])results[i]["sd"].GetData();
                                    for (int j = 0; j < latmins.Length; ++j)
                                        for (int k = 0; k < lonmins.Length; ++k)
                                            uncertainty[i, j, k] = lineUnc[k, j];
                                }
                                if (!isDataSourceSingle)
                                {
                                    UInt16[,] lineProv = (UInt16[,])results[i]["provenance"].GetData();
                                    foreach (var t in lineProv) usedDataSourceIds.Add(t);
                                    if (saveProv)
                                    {
                                        for (int j = 0; j < latmins.Length; ++j)
                                            for (int k = 0; k < lonmins.Length; ++k)
                                                provenance[i, j, k] = sources[lineProv[k, j]];
                                    }
                                }
                                else if (saveProv)
                                {
                                    for (int j = 0; j < latmins.Length; ++j)
                                        for (int k = 0; k < lonmins.Length; ++k)
                                            provenance[i, j, k] = dataSources[0];
                                }
                            }
                        }
                        else
                        {
                            double[, ,] tempData = (double[, ,])results[0]["values"].GetData();
                            for (int i = 0; i < timesN; ++i)
                                for (int j = 0; j < latmins.Length; ++j)
                                    for (int k = 0; k < lonmins.Length; ++k)
                                        data[i, j, k] = tempData[k, j, i];
                            if (saveUncertainty)
                            {
                                double[, ,] lineUnc = (double[, ,])results[0]["sd"].GetData();
                                for (int i = 0; i < timesN; ++i)
                                    for (int j = 0; j < latmins.Length; ++j)
                                        for (int k = 0; k < lonmins.Length; ++k)
                                            uncertainty[i, j, k] = lineUnc[k, j, i];
                            }
                            if (!isDataSourceSingle)
                            {
                                UInt16[, ,] lineProv = (UInt16[, ,])results[0]["provenance"].GetData();
                                foreach (var t in lineProv) usedDataSourceIds.Add(t);
                                if (saveProv)
                                {
                                    for (int i = 0; i < timesN; ++i)
                                        for (int j = 0; j < latmins.Length; ++j)
                                            for (int k = 0; k < lonmins.Length; ++k)
                                                provenance[i, j, k] = sources[lineProv[k, j, i]];
                                }
                            }
                            else if (saveProv)
                            {
                                for (int i = 0; i < timesN; ++i)
                                    for (int j = 0; j < latmins.Length; ++j)
                                        for (int k = 0; k < lonmins.Length; ++k)
                                            provenance[i, j, k] = dataSources[0];
                            }
                        }
                    }

                    varData = ds.Add(name, units, mv, data, dimTime, dimLat, dimLon);
                    if (saveUncertainty) varUncertainty = ds.Add(nameUncertainty, units, mv, uncertainty, dimTime, dimLat, dimLon);
                    if (saveProv) varProv = ds.Add(nameProvenance, provenance, dimTime, dimLat, dimLon);
                }
                else
                {
                    double[,] data = new double[latmins.Length, lonmins.Length];
                    double[,] uncertainty = saveUncertainty ? new double[latmins.Length, lonmins.Length] : null;
                    string[,] provenance = saveProv ? new string[latmins.Length, lonmins.Length] : null;
                    
                    if (spatialType == SpatialRegionSpecification.Cells)
                    {
                        double[] slice = (double[])results[0]["values"].GetData();
                        int index = 0;
                        for (int j = 0; j < latmins.Length; ++j)
                            for (int k = 0; k < lonmins.Length; ++k)
                            {
                                data[j, k] = slice[index];
                                ++index;
                            }
                        if (saveUncertainty)
                        {
                            double[] sliceUnc = (double[])results[0]["sd"].GetData();
                            index = 0;
                            for (int j = 0; j < latmins.Length; ++j)
                                for (int k = 0; k < lonmins.Length; ++k)
                                {
                                    uncertainty[j, k] = sliceUnc[index];
                                    ++index;
                                }
                        }
                        if (!isDataSourceSingle)
                        {
                            UInt16[] lineProv = (UInt16[])results[0]["provenance"].GetData();
                            foreach (var t in lineProv) usedDataSourceIds.Add(t);
                            if (saveProv)
                            {
                                index = 0;
                                for (int j = 0; j < latmins.Length; ++j)
                                    for (int k = 0; k < lonmins.Length; ++k)
                                    {
                                        provenance[j, k] = sources[lineProv[index]];
                                        ++index;
                                    }
                            }
                        }
                        else if (saveProv)
                        {
                            for (int j = 0; j < latmins.Length; ++j)
                                for (int k = 0; k < lonmins.Length; ++k)
                                {
                                    provenance[j, k] = dataSources[0];
                                }
                        }
                    }
                    else
                    {
                        double[,] tempData = (double[,])results[0]["values"].GetData();
                        for (int i = 0; i < latmins.Length; ++i)
                            for (int j = 0; j < lonmins.Length; ++j)
                                data[i, j] = tempData[j, i];
                        if (saveUncertainty)
                        {
                            double[,] lineUnc = (double[,])results[0]["sd"].GetData();
                            for (int i = 0; i < latmins.Length; ++i)
                                for (int j = 0; j < lonmins.Length; ++j)
                                    uncertainty[i, j] = lineUnc[j, i];
                        }
                        if (!isDataSourceSingle)
                        {
                            UInt16[,] lineProv = (UInt16[,])results[0]["provenance"].GetData();
                            foreach (var t in lineProv) usedDataSourceIds.Add(t);
                            if (saveProv)
                            {
                                for (int i = 0; i < latmins.Length; ++i)
                                    for (int j = 0; j < lonmins.Length; ++j)
                                        provenance[i, j] = sources[lineProv[j, i]];
                            }
                        }
                        else if (saveProv)
                        {
                            for (int i = 0; i < latmins.Length; ++i)
                                for (int j = 0; j < lonmins.Length; ++j)
                                    provenance[i, j] = dataSources[0];
                        }
                    }

                    varData = ds.Add(name, units, mv, data, dimLat, dimLon);
                    varData.Metadata["Time"] = timeSlices[0];
                    if (saveUncertainty)
                    {
                        varUncertainty = ds.Add(nameUncertainty, units, mv, uncertainty, dimLat, dimLon);
                        varUncertainty.Metadata["Time"] = timeSlices[0];
                    }
                    if (saveProv)
                    {
                        varProv = ds.Add(nameProvenance, provenance, dimLat, dimLon);
                        varProv.Metadata["Time"] = timeSlices[0];
                    }
                }
            }

            if (isDataSourceSingle)
            {
                usedDataSources = new string[] { dataSources[0] };
            }
            else
            {
                usedDataSources = usedDataSourceIds.Select(x => sources[x]).ToArray();
            }

            FillMetadata(varData, climatologyIntervals, usedDataSources, longName);
            if (saveUncertainty) FillUncertaintyMetadata(varUncertainty, usedDataSources, longName);
            if (saveProv) FillProvenanceMetadata(varProv, usedDataSources, longName);

            return varData;
        }

        private static void FillMetadata(Variable v, TimeBounds[] climatlogyIntervals, string[] dataSources, string longName)
        {
            if (climatlogyIntervals != null)
                v.Metadata["cell_methods"] = "time: mean within days  time: mean over days  time: mean over years";

            StringBuilder bld = new StringBuilder();
            bool firstDataSource = true;
            for (int i = 0; i < dataSources.Length; i++)
            {
                if (firstDataSource)
                    firstDataSource = false;
                else
                    bld.Append(", ");
                bld.Append(dataSources[i]);
            }
            string sources = bld.ToString();

            string addr = ClimateService.ServiceUrl;

            v.Metadata["long_name"] = longName;
            v.Metadata["DisplayName"] = longName;
            v.Metadata["Provenance"] = String.Format("Data sources: {0}; served by FetchClimate2 Service ({1})", sources, addr);
            v.Metadata["references"] = addr;
            v.Metadata["source"] = string.Format("Interpolated from {0}", sources);
            v.Metadata["institution"] = string.Format("FetchClimate2 service ({0} with timestamp: {1})", addr, ClimateService.Configuration.TimeStamp);
        }

        private static void FillUncertaintyMetadata(Variable v, string[] dataSources, string longName)
        {
            StringBuilder bld = new StringBuilder();
            bool firstDataSource = true;
            for (int i = 0; i < dataSources.Length; i++)
            {
                if (firstDataSource)
                    firstDataSource = false;
                else
                    bld.Append(", ");
                bld.Append(dataSources[i]);
            }
            string sources = bld.ToString();
            longName = String.Format("Uncertainty ({0})", longName);
            string addr = ClimateService.ServiceUrl;
            v.Metadata["long_name"] = longName;
            v.Metadata["DisplayName"] = longName;
            v.Metadata["Provenance"] = String.Format("Data sources: {0}; served by FetchClimate2 Service ({1})", sources, addr);
            v.Metadata["references"] = addr;
            v.Metadata["source"] = string.Format("Interpolated from {0}", sources);
            v.Metadata["institution"] = string.Format("FetchClimate2 service ({0} with timestamp: {1})", addr, ClimateService.Configuration.TimeStamp);
        }

        private static void FillProvenanceMetadata(Variable v, string[] dataSources, string longName)
        {
            StringBuilder bld = new StringBuilder();
            bool firstDataSource = true;
            for (int i = 0; i < dataSources.Length; i++)
            {
                if (firstDataSource)
                    firstDataSource = false;
                else
                    bld.Append(", ");
                bld.Append(dataSources[i]);
            }
            string sources = bld.ToString();
            longName = String.Format("Provenance ({0})", longName);
            string addr = ClimateService.ServiceUrl;
            v.Metadata["long_name"] = longName;
            v.Metadata["DisplayName"] = longName;
            v.Metadata["Provenance"] = String.Format("Data sources: {0}; served by FetchClimate2 Service ({1})", sources, addr);
            v.Metadata["references"] = addr;
            v.Metadata["source"] = "FetchClimate2 service (" + addr + ")";
            v.Metadata["institution"] = string.Format("FetchClimate2 service ({0} with timestamp: {1})", addr, ClimateService.Configuration.TimeStamp);
        }

        private static string GetLongName(string variableName)
        {
            return ClimateService.Configuration.EnvironmentalVariables.Single(x => x.Name == variableName).Description;
        }

        private static double[] GetDoubles(Array array)
        {
            if (array.Rank != 1) throw new ArgumentException("array is not 1d");
            var type = array.GetType();
            if (type == typeof(double[])) return (double[])array;

            int n = array.Length;
            double[] res = new double[n];
            for (int i = 0; i < n; i++)
                res[i] = Convert.ToDouble(array.GetValue(i));
            return res;
        }

        /// <summary>
        /// Adds a climatology bounds information according to "Climate and Forecast Conventions". Climatology bounds will depict timeseries enumerating hourss within each day from <paramref name="hourmin"/> to <paramref name="hourmax"/> inclusivly while keeping fixed years interval and days subinterval within each year
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="yearmin">An inclusive earlier bound of years to cover with timeseries</param>
        /// <param name="yearmax">An inclusive latter bound of years to cover with timeseries</param>
        /// <param name="daymin">An inclusive earlier bound of days in year to cover with timeseries</param>
        /// <param name="daymax">An inclusive latter bound of days in year to cover with timeseries</param>
        /// <param name="hourmin">An inclusive earlier bound of hours in day to cover with timeseries</param>
        /// <param name="hourmax">An inclusive latter bound of hours in day to cover with timeseries</param>
        /// <param name="hourStep">A step of hours to enumarate through <paramref name="hourmin"/> - <paramref name="hourmax"/>interval. The last interval might be trimmed not to exceed the <paramref name="hourmax"/> limit</param>
        /// <param name="timeAxisName">A name of the variable that would be used as time variable. Passing NULL will cause an automatic detection of a sutable varaible</param>
        /// <param name="climatologyAxisName">A name of the new variable that would be used as climatology axis</param>
        public static void AddClimatologyAxisHourly(this DataSet ds, int yearmin = 1961, int yearmax = 1990, int daymin = 1, int daymax = 365, int hourmin = 0, int hourmax = 24, int hourStep = 1, string timeAxisName = null, string climatologyAxisName = "climatology_bounds")
        {
            ds.AddClimatologyAxis(timeAxisName, climatologyAxisName, yearmin, yearmax, daymin, daymax, hourmin, hourmax, IntervalToSplit.Hours, hourStep);
        }

        /// <summary>
        /// Adds a climatology bounds information according to "Climate and Forecast Conventions". Climatology bounds will depict timeseries enumerating days within each year from <paramref name="daymin"/> to <paramref name="daymax"/> inclusivly while keeping fixed years interval and hours subinterval within each day
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="yearmin">An inclusive earlier bound of years to cover with timeseries</param>
        /// <param name="yearmax">An inclusive latter bound of years to cover with timeseries</param>
        /// <param name="daymin">An inclusive earlier bound of days in year to cover with timeseries</param>
        /// <param name="daymax">An inclusive latter bound of days in year to cover with timeseries</param>
        /// <param name="hourmin">An inclusive earlier bound of hours in day to cover with timeseries</param>
        /// <param name="hourmax">An inclusive latter bound of hours in day to cover with timeseries</param>
        /// <param name="dayStep">A step of days to enumarate through <paramref name="daymin"/> - <paramref name="daymax"/>interval. The last interval might be trimmed not to exceed the <paramref name="daymax"/> limit</param>
        /// <param name="timeAxisName">A name of the variable that would be used as time variable. Passing NULL will cause an automatic detection of a sutable varaible</param>
        /// <param name="climatologyAxisName">A name of the new variable that would be used as climatology axis</param>
        public static void AddClimatologyAxisSeasonly(this DataSet ds, int yearmin = 1961, int yearmax = 1990, int daymin = 1, int daymax = 365, int hourmin = 0, int hourmax = 24, int dayStep = 1, string timeAxisName = null, string climatologyAxisName = "climatology_bounds")
        {
            ds.AddClimatologyAxis(timeAxisName, climatologyAxisName, yearmin, yearmax, daymin, daymax, hourmin, hourmax, IntervalToSplit.Days, dayStep);
        }

        /// <summary>
        /// Adds a climatology bounds information according to "Climate and Forecast Conventions". Climatology bounds will depict timeseries enumerating years from <paramref name="yearmin"/> to <paramref name="yearmax"/> inclusivly while keeping fixed days subinterval within each year and hours subinterval within each day
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="yearmin">An inclusive earlier bound of years to cover with timeseries</param>
        /// <param name="yearmax">An inclusive latter bound of years to cover with timeseries</param>
        /// <param name="daymin">An inclusive earlier bound of days in year to cover with timeseries</param>
        /// <param name="daymax">An inclusive latter bound of days in year to cover with timeseries</param>
        /// <param name="hourmin">An inclusive earlier bound of hours in day to cover with timeseries</param>
        /// <param name="hourmax">An inclusive latter bound of hours in day to cover with timeseries</param>
        /// <param name="yearStep">A step of years to enumarate through <paramref name="yearmin"/> - <paramref name="yearmax"/>interval. The last interval might be trimmed not to exceed the <paramref name="yearmax"/> limit</param>
        /// <param name="timeAxisName">A name of the variable that would be used as time variable. Passing NULL will cause an automatic detection of a sutable varaible</param>
        /// <param name="climatologyAxisName">A name of the new variable that would be used as climatology axis</param>
        public static void AddClimatologyAxisYearly(this DataSet ds, int yearmin = 1961, int yearmax = 1990, int daymin = 1, int daymax = 365, int hourmin = 0, int hourmax = 24, int yearStep = 1, string timeAxisName = null, string climatologyAxisName = "climatology_bounds")
        {
            ds.AddClimatologyAxis(timeAxisName, climatologyAxisName, yearmin, yearmax, daymin, daymax, hourmin, hourmax, IntervalToSplit.Years, yearStep);
        }

        /// <summary>
        /// Adds a climatology bounds information according to "Climate and Forecast Conventions". Climatology bounds will depict timeseries enumerating all months in year while keeping fixed years interval and hours subinterval within each day
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="yearmin">An inclusive earlier bound of years to cover with timeseries</param>
        /// <param name="yearmax">An inclusive latter bound of years to cover with timeseries</param>
        /// <param name="hourmin">An inclusive earlier bound of hours in day to cover with timeseries. valid range 1..12</param>
        /// <param name="hourmax">An inclusive latter bound of hours in day to cover with timeseries</param>        
        /// <param name="timeAxisName">A name of the variable that would be used as time variable. Passing NULL will cause an automatic detection of a sutable varaible</param>
        /// <param name="climatologyAxisName">A name of the new variable that would be used as climatology axis</param>
        public static void AddClimatologyAxisMonthly(this DataSet ds, int yearmin = 1961, int yearmax = 1990, int hourmin = 0, int hourmax = 24, string timeAxisName = null, string climatologyAxisName = "climatology_bounds")
        {
            //System.Globalization.DateTimeFormatInfo.GetInstance().
            //DateTime.DaysInMonth(            
            ds.AddClimatologyAxis(timeAxisName, climatologyAxisName, yearmin, yearmax, 1, 1, hourmin, hourmax, calculateMonthlyIntervals: true);
        }

        internal enum IntervalToSplit { Hours, Days, Years };

        private static void AddClimatologyAxis(this DataSet ds, string timeAxis, string climatologyAxis, int yearmin, int yearmax, int daymin, int daymax, int hourmin, int hourmax, IntervalToSplit its = IntervalToSplit.Days, int step = 0, bool calculateMonthlyIntervals = false)
        {
            #region arguments check
            if (daymin < 0 || daymax > 366)
            {
                throw new ArgumentException(String.Format("The minimum day in the year doesn't fall into permited interval. You've entered {0}. However it must be set to 1..366 or to GlobalConsts.DefaultValue.", daymax));
            }
            if (daymin < 0 || daymin > 366)
            {
                throw new ArgumentException(String.Format("The maximum day in the year doesn't fall into permited interval. You've entered {0}. However it must be set to 1..366 or to GlobalConsts.DefaultValue.", daymin));
            }
            if (hourmin < 0 || hourmin > 24)
            {
                throw new ArgumentException(String.Format("starting hour in the day doesn't fall into permited interval. You've entered {0}. However it must be set to 0..24 or to GlobalConsts.DefaultValue.", hourmin));
            }
            if (hourmax < 0 || hourmax > 24)
            {
                throw new ArgumentException(String.Format("The last hour in the day doesn't fall into permited interval. You've entered {0}. However it must be set to 0..24 or to GlobalConsts.DefaultValue.", hourmax));
            }

            if (yearmin > yearmax)
            {
                throw new ArgumentException(String.Format("The minimum requested year(you've entered {0}) must be less or equel to maximum requested year(you've entered {1}).", yearmin, yearmax));
            }

            if (hourmin > hourmax)
            {
                throw new ArgumentException("Hour min is more then hour max");
            }
            if (daymin > daymax)
            {
                throw new ArgumentException("Daymin is more then day max");
            }
            if (calculateMonthlyIntervals && its != IntervalToSplit.Days && step == 0)
                throw new InvalidOperationException("monthlyDaysSteps should be specified only for ");
            int[] monthDays = null;
            if (calculateMonthlyIntervals)
                monthDays = Enumerable.Range(1, 12).Select(i => DateTime.DaysInMonth(2001, i)).ToArray();
            if (ds.Variables.Contains(climatologyAxis))
                throw new InvalidOperationException("Dataset already contains specified climatology axis");
            if (timeAxis == null)
                timeAxis = ds.Variables.Where(v => v.TypeOfData == typeof(DateTime) && v.Rank == 1).Select(v => v.Name).FirstOrDefault();
            if (timeAxis == null)
            {
                if (ds.Variables.Contains("time"))
                {
                    int num = 1;
                    while (ds.Variables.Contains(string.Format("time{0}", num)))
                        num++;
                    timeAxis = string.Format("time{0}", num);
                }
                else
                    timeAxis = "time";
            }
            #endregion

            int n = 0;
            if (calculateMonthlyIntervals)//monthly timeseries
                n = 12;
            else
                switch (its)
                {
                    case IntervalToSplit.Years: n = (int)Math.Ceiling((yearmax - yearmin) / (double)step); break;
                    case IntervalToSplit.Days: n = (int)Math.Ceiling((daymax - daymin) / (double)step); break;
                    case IntervalToSplit.Hours: n = (int)Math.Ceiling((hourmax - hourmin) / (double)step); break;
                    default: throw new NotImplementedException();
                }
            if (ds.Variables.Contains(timeAxis))
            {
                if (ds.Variables[timeAxis].Rank > 1)
                    throw new ArgumentException("Specified time axis has rank more than 1");
                int len = ds.Variables[timeAxis].Dimensions[0].Length;
                if (len != 0 && len != n)
                    throw new ArgumentException("Specified time axis has length more than zero and not equal to supposed climatology axis length");
            }

            int[] hourmins = new int[n];
            int[] hourmaxs = new int[n];
            int[] daymins = new int[n];
            int[] daymaxs = new int[n];
            int[] yearmins = new int[n];
            int[] yearmaxs = new int[n];
            DateTime[,] timeBounds = new DateTime[n, 2];
            DateTime[] times = new DateTime[n];


            int yi = (its == IntervalToSplit.Years) ? 1 : 0;
            int di = (its == IntervalToSplit.Days) ? 1 : 0;
            int hi = (its == IntervalToSplit.Hours) ? 1 : 0;

            int day = 1;
            for (int i = 0; i < n; i++)
            {
                hourmins[i] = Math.Min((hi == 0) ? hourmin : (hourmin + hi * i * step), 24);
                hourmaxs[i] = Math.Min((hi == 0) ? hourmax : (hourmin + hi * ((i + 1) * step - 1)), 24);
                yearmins[i] = (yi == 0) ? yearmin : (yearmin + yi * i * step);
                yearmaxs[i] = (yi == 0) ? yearmax : (yearmin + yi * ((i + 1) * step - 1));
                if (!calculateMonthlyIntervals) //seasonly timeseries
                {
                    daymins[i] = Math.Min((di == 0) ? daymin : (daymin + di * (i * step)), (yearmins[i] == yearmaxs[i] && DateTime.IsLeapYear(yearmins[i])) ? 366 : 365);
                    daymaxs[i] = Math.Min((di == 0) ? daymax : (daymin + di * ((i + 1) * step - 1)), (yearmins[i] == yearmaxs[i] && DateTime.IsLeapYear(yearmins[i])) ? 366 : 365);
                }
                else //monthly timeseries
                {
                    daymins[i] = day;
                    daymaxs[i] = day + monthDays[i] - 1;
                    day += monthDays[i];
                }
                timeBounds[i, 0] = new DateTime(yearmins[i], 1, 1).AddDays(daymins[i] - 1).AddHours(hourmins[i]);
                timeBounds[i, 1] = new DateTime(yearmaxs[i], 1, 1).AddDays(daymaxs[i] - 1).AddHours(hourmaxs[i]);
                times[i] = new DateTime((yearmaxs[i] + yearmins[i]) / 2, 1, 1).AddDays((daymaxs[i] + daymins[i]) / 2).AddHours((hourmaxs[i] + hourmins[i]) / 2);
            }
            string sndDim = "nv";
            if (ds.Dimensions.Contains(sndDim))
            {
                int num = 1;
                while (ds.Dimensions.Contains(string.Format("{0}{1}", sndDim, num)))
                    num++;
                sndDim = string.Format("{0}{1}", sndDim, num);
            }
            if (!ds.Variables.Contains(timeAxis))
            {
                ds.Add(timeAxis, times, timeAxis);
            }
            else if (ds.Variables[timeAxis].Dimensions[0].Length == 0)
            {
                ds.Variables[timeAxis].Append(times);
            }
            ds.Add(climatologyAxis, timeBounds, ds.Variables[timeAxis].Dimensions[0].Name, sndDim);
            ds.Variables[timeAxis].Metadata["climatology"] = climatologyAxis;
        }

        private static TimeBounds[] GetClimatologyBounds(DataSet ds, int timeAxisID = -1)
        {
            List<TimeBounds> tbList = new List<TimeBounds>();
            IEnumerable<Variable> q = timeAxisID < 0 ? ds.Variables.Where(va => va.Rank == 1 && va.Metadata.ContainsKey("climatology")) :
                new Variable[] { ds.Variables.GetByID(timeAxisID) };
            foreach (Variable timeVar in q)
            {
                Dimension timeDim = timeVar.Dimensions[0];
                var climAxes = ds.Variables.Where(cv => cv.Rank == 2 && cv.TypeOfData == typeof(DateTime) && cv.Dimensions[0].Name == timeDim.Name && cv.Dimensions[1].Length == 2);
                if (climAxes.Count() > 1)
                    throw new ArgumentException(string.Format("There are {0} possible climatological axes. Ambiguous specification", climAxes.Count()));
                Variable climatologyAxis = climAxes.FirstOrDefault();
                if (climatologyAxis == null)
                    continue;
                DateTime[,] bounds = (DateTime[,])climatologyAxis.GetData();
                for (int i = 0; i < bounds.GetLength(0); i++)
                {
                    TimeBounds tb = new TimeBounds()
                    {
                        MinYear = bounds[i, 0].Year,
                        MinDay = bounds[i, 0].DayOfYear,
                        MinHour = bounds[i, 0].Hour,
                        MaxYear = bounds[i, 1].Year,
                        MaxDay = bounds[i, 1].DayOfYear,
                        MaxHour = bounds[i, 1].Hour
                    };
                    if (tb.MaxDay == 1 && tb.MaxHour == 0)
                    {
                        tb.MaxDay = 365;
                        tb.MaxHour = 24;
                        tb.MaxYear--;
                    }
                    if (tb.MaxHour == tb.MinHour)
                    {
                        tb.MinHour = 0;
                        tb.MaxHour = 24;
                    }
                    tbList.Add(tb);
                }
                break;
            }
            return tbList.ToArray();
        }

        private static bool IsTimes(Variable var)
        {
            string name = var.Name.ToLower();
            if (name.StartsWith("time")) return true;
            return false;
        }

        private static Variable[] GetDefaultCoordinateSystem(this DataSet ds)
        {
            return ds.Where(p => p.Rank == 1 && p.Name == p.Dimensions[0].Name).ToArray();
        }
    }
}

namespace Microsoft.Research.Science.Data.Climate
{
    /// <summary>
    /// A variation that is primary for user to reflect
    /// </summary>
    public enum ResearchVariationType { Auto, Spatial, Yearly, Seasonly, Hourly };


    public class TimeBounds : IEquatable<TimeBounds>
    {
        int minday = 1, maxday = 365, minhour = 0, maxhour = 24;

        public int MinYear { get; set; }
        public int MaxYear { get; set; }
        /// <summary>
        /// 1..366
        /// </summary>
        public int MinDay
        {
            get
            {
                return minday;
            }
            set
            {
                if ((value < 1 || value > 366) && value != GlobalConsts.DefaultValue)
                    throw new ArgumentException("Day cant be less then 1 or more then 366");
                minday = value;
            }
        }

        /// <summary>
        /// 1..366
        /// </summary>
        public int MaxDay
        {
            get
            {
                return maxday;
            }
            set
            {
                if ((value < 1 || value > 366) && value != GlobalConsts.DefaultValue)
                    throw new ArgumentException("Day number cant be less then 1 or more then 366");
                maxday = value;
            }
        }

        /// <summary>
        /// 0..24
        /// </summary>
        public int MaxHour
        {
            get
            { return maxhour; }
            set
            {
                if ((value < 0 || value > 24) && value != GlobalConsts.DefaultValue)
                    throw new ArgumentException("Hour cant be less then 0 or more then 24");
                maxhour = value;
            }
        }

        /// <summary>
        /// 0..24
        /// </summary>
        public int MinHour
        {
            get
            {
                return minhour;
            }
            set
            {
                if ((value < 0 || value > 24) && value != GlobalConsts.DefaultValue)
                    throw new ArgumentException("Hour cant be less then 0 or more then 24");
                minhour = value;
            }
        }

        public TimeBounds Clone()
        {
            return (TimeBounds)MemberwiseClone();
        }

        public override bool Equals(object obj)
        {
            if (obj is TimeBounds)
            {
                return ((TimeBounds)obj).Equals(this);
            }
            else return false;
        }

        public override int GetHashCode()
        {
            return MaxYear.GetHashCode() >> 1 ^
                MinYear.GetHashCode() >> 2 ^
                MaxDay.GetHashCode() << 1 ^
                MinDay.GetHashCode() << 4 ^
                MaxHour.GetHashCode() >> 2 ^
                MinHour.GetHashCode() >> 1;
        }
        #region IEquatable<TimeBounds> Members

        public bool Equals(TimeBounds first)
        {
            return (MaxYear == first.MaxYear &&
                        MinYear == first.MinYear &&
                        MaxDay == first.MaxDay &&
                        MinDay == first.MinDay &&
                        MaxHour == first.MaxHour &&
                        MinHour == first.MinHour);
        }

        #endregion
    }

}

