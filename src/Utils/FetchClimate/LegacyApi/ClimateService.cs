using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Research.Science.FetchClimate2;

namespace Microsoft.Research.Science.Data
{
    public static partial class ClimateService
    {
        private static Dictionary<int, string> _dataSources = null;
        private static Dictionary<int, string> _DataSources
        {
            get
            {
                if (_dataSources == null)
                {
                    _dataSources = new Dictionary<int, string>();
                    _dataSources.Add(65535, "");
                    foreach (var i in Configuration.DataSources) _dataSources.Add(i.ID, i.Name);
                }
                return _dataSources;
            }
        }

        private static void SetDefaults(ref int hourmin, ref int hourmax, ref int daymin, ref int daymax)
        {
            if (hourmin == GlobalConsts.DefaultValue) hourmin = 0;
            if (hourmax == GlobalConsts.DefaultValue) hourmax = 24;
            if (daymin == GlobalConsts.DefaultValue) daymin = 1;
            if (daymax == GlobalConsts.DefaultValue) daymax = -1;
        }

        private static Task<DataSet>[] ScheduleBatchWork(ClimateParameter parameter, double[] latmin, double[] latmax, double[] lonmin, double[] lonmax, int[] starthour, int[] stophour, int[] startday, int[] stopday, int[] startyear, int[] stopyear, EnvironmentalDataSource dataSource)
        {
            if (latmin == null || latmax == null || lonmin == null || lonmax == null ||
                latmin.Length != latmax.Length || latmin.Length != lonmin.Length || latmin.Length != lonmax.Length || (starthour != null && latmin.Length != starthour.Length) ||
                (stophour != null && latmin.Length != stophour.Length) || (startday != null && latmin.Length != startday.Length) || (stopday != null && latmin.Length != stopday.Length) ||
                (startyear != null && latmin.Length != startyear.Length) || (stopyear != null && latmin.Length != stopyear.Length)) throw new ArgumentException("Array lengths are out of sync.");
            Task<DataSet>[] tasks = new Task<DataSet>[latmin.Length];
            for (int i = 0; i < latmin.Length; ++i)
            {
                int startY = startyear != null ? startyear[i] : 1961;
                int stopY = stopyear != null ? stopyear[i] : 1990;
                int startD = startday != null ? startday[i] : GlobalConsts.DefaultValue;
                int stopD = stopday != null ? stopday[i] : GlobalConsts.DefaultValue;
                int startH = starthour != null ? starthour[i] : GlobalConsts.DefaultValue;
                int stopH = stophour != null ? stophour[i] : GlobalConsts.DefaultValue;
                SetDefaults(ref startH, ref stopH, ref startD, ref stopD);
                ITimeRegion tr = new TimeRegion(startY, stopY, startD, stopD, startH, stopH);
                IFetchRequest request = CreateCellOrPointRequest(parameter, latmin[i], latmax[i], lonmin[i], lonmax[i], dataSource, tr);

                tasks[i] = FetchAsync(request);
            }
            return tasks;
        }

        private static IFetchRequest CreateCellOrPointRequest(ClimateParameter parameter, double latmin, double latmax, double lonmin, double lonmax, EnvironmentalDataSource dataSource, ITimeRegion tr)
        {
            IFetchRequest request;
            if (latmin != latmax && lonmin != lonmax)
            {
                request = new FetchRequest(
                    ClimateParameterToFC2VariableName(parameter),
                    FetchDomain.CreateCells(
                        new double[] { latmin },
                        new double[] { lonmin },
                        new double[] { latmax },
                        new double[] { lonmax },
                        tr),
                    EnvironmentalDataSourceToArrayOfFC2DataSources(dataSource));
            }
            else if (latmin == latmax && lonmin == lonmax)
            {
                request = new FetchRequest(
                    ClimateParameterToFC2VariableName(parameter),
                    FetchDomain.CreatePoints(
                        new double[] { latmin },
                        new double[] { lonmin },
                        tr),
                    EnvironmentalDataSourceToArrayOfFC2DataSources(dataSource));
            }
            else throw new ArgumentException("Geographical region is a line segment.");
            return request;
        }

        /// <summary>
        /// Returns FC2 variable name mapped to given ClimateParameter <paramref name="p"/> in FetchClimate.exe.config.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static string ClimateParameterToFC2VariableName(ClimateParameter p)
        {
            switch (p)
            {
                case ClimateParameter.FC_ELEVATION: return Microsoft.Research.Science.FetchClimate2.Properties.Settings.Default.FC_ELEVATION;
                case ClimateParameter.FC_LAND_AIR_RELATIVE_HUMIDITY: return Microsoft.Research.Science.FetchClimate2.Properties.Settings.Default.FC_LAND_AIR_RELATIVE_HUMIDITY;
                case ClimateParameter.FC_LAND_AIR_TEMPERATURE: return Microsoft.Research.Science.FetchClimate2.Properties.Settings.Default.FC_LAND_AIR_TEMPERATURE;
                case ClimateParameter.FC_LAND_DIURNAL_TEMPERATURE_RANGE: return Microsoft.Research.Science.FetchClimate2.Properties.Settings.Default.FC_LAND_DIURNAL_TEMPERATURE_RANGE;
                case ClimateParameter.FC_LAND_ELEVATION: return Microsoft.Research.Science.FetchClimate2.Properties.Settings.Default.FC_LAND_ELEVATION;
                case ClimateParameter.FC_LAND_FROST_DAY_FREQUENCY: return Microsoft.Research.Science.FetchClimate2.Properties.Settings.Default.FC_LAND_FROST_DAY_FREQUENCY;
                case ClimateParameter.FC_LAND_SUN_PERCENTAGE: return Microsoft.Research.Science.FetchClimate2.Properties.Settings.Default.FC_LAND_SUN_PERCENTAGE;
                case ClimateParameter.FC_LAND_WET_DAY_FREQUENCY: return Microsoft.Research.Science.FetchClimate2.Properties.Settings.Default.FC_LAND_WET_DAY_FREQUENCY;
                case ClimateParameter.FC_LAND_WIND_SPEED: return Microsoft.Research.Science.FetchClimate2.Properties.Settings.Default.FC_LAND_WIND_SPEED;
                case ClimateParameter.FC_OCEAN_AIR_TEMPERATURE: return Microsoft.Research.Science.FetchClimate2.Properties.Settings.Default.FC_OCEAN_AIR_TEMPERATURE;
                case ClimateParameter.FC_OCEAN_DEPTH: return Microsoft.Research.Science.FetchClimate2.Properties.Settings.Default.FC_OCEAN_DEPTH;
                case ClimateParameter.FC_PRECIPITATION: return Microsoft.Research.Science.FetchClimate2.Properties.Settings.Default.FC_PRECIPITATION;
                case ClimateParameter.FC_RELATIVE_HUMIDITY: return Microsoft.Research.Science.FetchClimate2.Properties.Settings.Default.FC_RELATIVE_HUMIDITY;
                case ClimateParameter.FC_SOIL_MOISTURE: return Microsoft.Research.Science.FetchClimate2.Properties.Settings.Default.FC_SOIL_MOISTURE;
                case ClimateParameter.FC_TEMPERATURE: return Microsoft.Research.Science.FetchClimate2.Properties.Settings.Default.FC_TEMPERATURE;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Returns an array of FC2 data source names mapped to given EnvironmentalDataSource <paramref name="ds"/> in FetchClimate.exe.config.
        /// Array elements in config file have to be separated by '|' symbol.
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        public static string[] EnvironmentalDataSourceToArrayOfFC2DataSources(EnvironmentalDataSource ds)
        {
            switch (ds)
            {
                case EnvironmentalDataSource.ANY: return null;
                case EnvironmentalDataSource.CPC_SOIL_MOSITURE: return Microsoft.Research.Science.FetchClimate2.Properties.Settings.Default.CPC_SOIL_MOSITURE.Split('|').Select(x => x.Trim()).ToArray();
                case EnvironmentalDataSource.CRU_CL_2_0: return Microsoft.Research.Science.FetchClimate2.Properties.Settings.Default.CRU_CL_2_0.Split('|').Select(x => x.Trim()).ToArray();
                case EnvironmentalDataSource.ETOPO1_ICE_SHEETS: return Microsoft.Research.Science.FetchClimate2.Properties.Settings.Default.ETOPO1_ICE_SHEETS.Split('|').Select(x => x.Trim()).ToArray();
                case EnvironmentalDataSource.GHCNv2: return Microsoft.Research.Science.FetchClimate2.Properties.Settings.Default.GHCNv2.Split('|').Select(x => x.Trim()).ToArray();
                case EnvironmentalDataSource.GTOPO30: return Microsoft.Research.Science.FetchClimate2.Properties.Settings.Default.GTOPO30.Split('|').Select(x => x.Trim()).ToArray();
                case EnvironmentalDataSource.NCEP_REANALYSIS_1: return Microsoft.Research.Science.FetchClimate2.Properties.Settings.Default.NCEP_REANALYSIS_1.Split('|').Select(x => x.Trim()).ToArray();
                case EnvironmentalDataSource.WORLD_CLIM_1_4: return Microsoft.Research.Science.FetchClimate2.Properties.Settings.Default.WORLD_CLIM_1_4.Split('|').Select(x => x.Trim()).ToArray();
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Returns the provenance of the value calculated by FetchClimate of specified climate parameter for the given geographical region at the given time of the day, season and years interval.
        /// </summary>
        /// <param name="parameter">A climate variable that will be evaluated for provenance</param>
        /// <param name="latmin">Minimum latitude of the requested region</param>
        /// <param name="lonmin">Mimimun longatude of the requested region</param>
        /// <param name="latmax">Maximum latitude of the requesed region </param>
        /// <param name="lonmax">Maximum longatude of the requesed region</param>
        /// <param name="startyear">The first year that will be used to form time intervals of the request. Default value is 1961</param>
        /// <param name="startday">The first day of the year (1..365) that will be used to form time intervals of the request. Default value is 1</param>
        /// <param name="starthour">The time in hours of the day (0..24) that will be used as a lower inclusive bound to form time intervals of the request. Default value is 0</param>
        /// <param name="stopyear">The last year(inclusivly) that will be used to form time intervals for request. Default value is 1990</param>
        /// <param name="stopday">The last day of the year(inclusivly) that will be used to form time intervals of the request. Use GlobalConsts.DefaultValue to specify the last day of the year. Default value is "the last day of the year"</param>
        /// <param name="stophour">The time in hours of the day (0..24) that will be used as an upper inclusive bound to form time intervals of the request. Use 24 to specify the end of the day. Default value is 24</param>
        /// <param name="dataSource">The data source to use for calculation. By default the data source with lowest uncertainty is chosen automaticly</param>
        public static string FetchClimateProvenance(ClimateParameter parameter, double latmin, double latmax, double lonmin, double lonmax, int starthour = 0, int stophour = 24, int startday = 1, int stopday = GlobalConsts.DefaultValue, int startyear = 1961, int stopyear = 1990, EnvironmentalDataSource dataSource = EnvironmentalDataSource.ANY)
        {
            SetDefaults(ref starthour, ref stophour, ref startday, ref stopday);
            ITimeRegion tr = new TimeRegion(startyear, stopyear, startday, stopday, starthour, stophour);
            string[] ds = EnvironmentalDataSourceToArrayOfFC2DataSources(dataSource);
            if (ds == null || ds.Length > 1)
            {
                IFetchRequest request = CreateCellOrPointRequest(parameter, latmin, latmax, lonmin, lonmax, dataSource, tr);

                var ids = (UInt16[])FetchAsync(request).Result["provenance"].GetData();
                return _DataSources[ids[0]];//Configuration.DataSources.Single(x => x.ID == ids[0]).Name;
            }
            else
                return ds[0];
        }

        /// <summary>
        /// Returns an uncertainty of the value calculated by FetchClimate of specified climate parameter for the given geographical region at the given time of the day, season and years interval.
        /// </summary>
        /// <param name="parameter">A climate variable that will be evaluated for mean value</param>
        /// <param name="latmin">Minimum latitude of the requested region</param>
        /// <param name="lonmin">Mimimun longatude of the requested region</param>
        /// <param name="latmax">Maximum latitude of the requesed region </param>
        /// <param name="lonmax">Maximum longatude of the requesed region</param>
        /// <param name="startyear">The first year that will be used to form time intervals of the request. Default value is 1961</param>
        /// <param name="startday">The first day of the year (1..365) that will be used to form time intervals of the request. Default value is 1</param>
        /// <param name="starthour">The time in hours of the day (0..24) that will be used as a lower inclusive bound to form time intervals of the request. Default value is 0</param>
        /// <param name="stopyear">The last year(inclusivly) that will be used to form time intervals for request. Default value is 1990</param>
        /// <param name="stopday">The last day of the year(inclusivly) that will be used to form time intervals of the request. Use GlobalConsts.DefaultValue to specify the last day of the year. Default value is "the last day of the year"</param>
        /// <param name="stophour">The time in hours of the day (0..24) that will be used as an upper inclusive bound to form time intervals of the request. Use 24 to specify the end of the day. Default value is 24</param>
        /// <param name="dataSource">The data source to use for calculation. By default the data source with lowest uncertainty is chosen automaticly</param>
        public static double FetchClimateUncertainty(ClimateParameter parameter, double latmin, double latmax, double lonmin, double lonmax, int starthour = 0, int stophour = 24, int startday = 1, int stopday = GlobalConsts.DefaultValue, int startyear = 1961, int stopyear = 1990, EnvironmentalDataSource dataSource = EnvironmentalDataSource.ANY)
        {
            SetDefaults(ref starthour, ref stophour, ref startday, ref stopday);
            ITimeRegion tr = new TimeRegion(startyear, stopyear, startday, stopday, starthour, stophour);
            IFetchRequest request = CreateCellOrPointRequest(parameter, latmin, latmax, lonmin, lonmax, dataSource, tr);

            var res = (Double[])FetchAsync(request).Result["sd"].GetData();
            return res[0];
        }

        /// <summary>
        /// Returns mean value of specified climate parameter for the given geographical region at the given time of the day, season and years interval.
        /// </summary>
        /// <param name="parameter">A climate variable that will be evaluated for mean value</param>
        /// <param name="latmin">Minimum latitude of the requested region</param>
        /// <param name="lonmin">Mimimun longatude of the requested region</param>
        /// <param name="latmax">Maximum latitude of the requesed region </param>
        /// <param name="lonmax">Maximum longatude of the requesed region</param>
        /// <param name="startyear">The first year that will be used to form time intervals of the request. Default value is 1961</param>
        /// <param name="startday">The first day of the year (1..365) that will be used to form time intervals of the request. Default value is 1</param>
        /// <param name="starthour">The time in hours of the day (0..24) that will be used as a lower inclusive bound to form time intervals of the request. Default value is 0</param>
        /// <param name="stopyear">The last year(inclusivly) that will be used to form time intervals for request. Default value is 1990</param>
        /// <param name="stopday">The last day of the year(inclusivly) that will be used to form time intervals of the request. Use GlobalConsts.DefaultValue to specify the last day of the year. Default value is "the last day of the year"</param>
        /// <param name="stophour">The time in hours of the day (0..24) that will be used as an upper inclusive bound to form time intervals of the request. Use 24 to specify the end of the day. Default value is 24</param>
        /// <param name="dataSource">The data source to use for calculation. By default the data source with lowest uncertainty is chosen automaticly</param>
        public static double FetchClimate(ClimateParameter parameter, double latmin, double latmax, double lonmin, double lonmax, int starthour = 0, int stophour = 24, int startday = 1, int stopday = GlobalConsts.DefaultValue, int startyear = 1961, int stopyear = 1990, EnvironmentalDataSource dataSource = EnvironmentalDataSource.ANY)
        {
            SetDefaults(ref starthour, ref stophour, ref startday, ref stopday);
            ITimeRegion tr = new TimeRegion(startyear, stopyear, startday, stopday, starthour, stophour);
            IFetchRequest request = CreateCellOrPointRequest(parameter, latmin, latmax, lonmin, lonmax, dataSource, tr);

            var res = (Double[])FetchAsync(request).Result["values"].GetData();
            return res[0];
        }

        /// <summary>
        /// Batch version of single fetch climate request that returns provenances of the results. The parameteras are passed as a seperate arrays.
        /// </summary>
        /// <param name="parameter">A climate parameter that will be evaluated for mean value</param>
        /// <param name="latmin">Minimum latitude of the requested region</param>
        /// <param name="lonmin">Mimimun longatude of the requested region</param>
        /// <param name="latmax">Maximum latitude of the requesed region </param>
        /// <param name="lonmax">Maximum longatude of the requesed region</param>
        /// <param name="startyear">The first year that will be used to form time intervals of the request. The null value means 1961 (default)</param>
        /// <param name="startday">The first day of the year (1..365) that will be used to form time intervals of the request. The null value means 1</param>
        /// <param name="starthour">The time in hours of the day (0..24) that will be used as a lower inclusive bound to form time intervals of the request. The null value means 0</param>
        /// <param name="stopyear">The last year(inclusivly) that will be used to form time intervals for request. The null value means 1990</param>
        /// <param name="stopday">The last day of the year(inclusivly) that will be used to form time intervals of the request. Use GlobalConsts.DefaultValue to specify the last day of the year. The null value means "the last day of the year"</param>
        /// <param name="stophour">The time in hours of the day (0..24) that will be used as an upper inclusive bound to form time intervals of the request. Use 24 to specify the end of the day. The null value means 24</param>
        /// <param name="dataSource">The data source to use for calculation. By default the data source with lowest uncertainty is chosen automaticly</param> 
        public static string[] FetchClimateProvenance(ClimateParameter parameter, double[] latmin, double[] latmax, double[] lonmin, double[] lonmax, int[] starthour = null, int[] stophour = null, int[] startday = null, int[] stopday = null, int[] startyear = null, int[] stopyear = null, EnvironmentalDataSource dataSource = EnvironmentalDataSource.ANY)
        {
            string[] ds = EnvironmentalDataSourceToArrayOfFC2DataSources(dataSource);
            if (ds == null || ds.Length > 1)
            {
                Task<DataSet>[] tasks = ScheduleBatchWork(parameter, latmin, latmax, lonmin, lonmax, starthour, stophour, startday, stopday, startyear, stopyear, dataSource);
                string[] result = new string[latmin.Length];
                for (int i = 0; i < latmin.Length; ++i) result[i] = _DataSources[((UInt16[])tasks[i].Result["provenance"].GetData())[0]];
                return result;
            }
            else
            {
                if (latmin == null || latmax == null || lonmin == null || lonmax == null ||
                    latmin.Length != latmax.Length || latmin.Length != lonmin.Length || latmin.Length != lonmax.Length || (starthour != null && latmin.Length != starthour.Length) ||
                    (stophour != null && latmin.Length != stophour.Length) || (startday != null && latmin.Length != startday.Length) || (stopday != null && latmin.Length != stopday.Length) ||
                    (startyear != null && latmin.Length != startyear.Length) || (stopyear != null && latmin.Length != stopyear.Length)) throw new ArgumentException("Array lengths are out of sync.");
                string[] result = new string[latmin.Length];
                for (int i = 0; i < latmin.Length; ++i) result[i] = ds[0];
                return result;
            }
        }

        /// <summary>
        /// Batch version of single fetch climate request that return uncertainties of the results. The parameteras are passed as a seperate arrays.
        /// </summary>
        /// <param name="parameter">A climate parameter that will be evaluated for mean value</param>
        /// <param name="latmin">Minimum latitude of the requested region</param>
        /// <param name="lonmin">Mimimun longatude of the requested region</param>
        /// <param name="latmax">Maximum latitude of the requesed region </param>
        /// <param name="lonmax">Maximum longatude of the requesed region</param>
        /// <param name="startyear">The first year that will be used to form time intervals of the request. The null value means 1961 (default)</param>
        /// <param name="startday">The first day of the year (1..365) that will be used to form time intervals of the request. The null value means 1</param>
        /// <param name="starthour">The time in hours of the day (0..24) that will be used as a lower inclusive bound to form time intervals of the request. The null value means 0</param>
        /// <param name="stopyear">The last year(inclusivly) that will be used to form time intervals for request. The null value means 1990</param>
        /// <param name="stopday">The last day of the year(inclusivly) that will be used to form time intervals of the request. Use GlobalConsts.DefaultValue to specify the last day of the year. The null value means "the last day of the year"</param>
        /// <param name="stophour">The time in hours of the day (0..24) that will be used as an upper inclusive bound to form time intervals of the request. Use 24 to specify the end of the day. The null value means 24</param>
        /// <param name="dataSource">The data source to use for calculation. By default the data source with lowest uncertainty is chosen automaticly</param> 
        public static double[] FetchClimateUncertainty(ClimateParameter parameter, double[] latmin, double[] latmax, double[] lonmin, double[] lonmax, int[] starthour = null, int[] stophour = null, int[] startday = null, int[] stopday = null, int[] startyear = null, int[] stopyear = null, EnvironmentalDataSource dataSource = EnvironmentalDataSource.ANY)
        {
            Task<DataSet>[] tasks = ScheduleBatchWork(parameter, latmin, latmax, lonmin, lonmax, starthour, stophour, startday, stopday, startyear, stopyear, dataSource);
            double[] result = new double[latmin.Length];
            for (int i = 0; i < latmin.Length; ++i) result[i] = ((Double[])tasks[i].Result["sd"].GetData())[0];
            return result;
        }

        /// <summary>
        /// Batch version of single fetch climate request. The parameteras are passed as a seperate arrays.
        /// </summary>
        /// <param name="parameter">A climate parameter that will be evaluated for mean value</param>
        /// <param name="latmin">Minimum latitude of the requested region</param>
        /// <param name="lonmin">Mimimun longatude of the requested region</param>
        /// <param name="latmax">Maximum latitude of the requesed region </param>
        /// <param name="lonmax">Maximum longatude of the requesed region</param>
        /// <param name="startyear">The first year that will be used to form time intervals of the request. The null value means 1961 (default)</param>
        /// <param name="startday">The first day of the year (1..365) that will be used to form time intervals of the request. The null value means 1</param>
        /// <param name="starthour">The time in hours of the day (0..24) that will be used as a lower inclusive bound to form time intervals of the request. The null value means 0</param>
        /// <param name="stopyear">The last year(inclusivly) that will be used to form time intervals for request. The null value means 1990</param>
        /// <param name="stopday">The last day of the year(inclusivly) that will be used to form time intervals of the request. Use GlobalConsts.DefaultValue to specify the last day of the year. The null value means "the last day of the year"</param>
        /// <param name="stophour">The time in hours of the day (0..24) that will be used as an upper inclusive bound to form time intervals of the request. Use 24 to specify the end of the day. The null value means 24</param>
        /// <param name="dataSource">The data source to use for calculation. By default the data source with lowest uncertainty is chosen automaticly</param> 
        public static double[] FetchClimate(ClimateParameter parameter, double[] latmin, double[] latmax, double[] lonmin, double[] lonmax, int[] starthour = null, int[] stophour = null, int[] startday = null, int[] stopday = null, int[] startyear = null, int[] stopyear = null, EnvironmentalDataSource dataSource = EnvironmentalDataSource.ANY)
        {
            Task<DataSet>[] tasks = ScheduleBatchWork(parameter, latmin, latmax, lonmin, lonmax, starthour, stophour, startday, stopday, startyear, stopyear, dataSource);
            double[] result = new double[latmin.Length];
            for (int i = 0; i < latmin.Length; ++i) result[i] = ((Double[])tasks[i].Result["values"].GetData())[0];
            return result;
        }

        /// <summary>
        /// Split requested area into spatial grid and returns provenance for calculated values of the requested parameter.
        /// </summary>
        /// <param name="parameter">A climate parameter that will be evaluated for mean value</param>
        /// <param name="latmin">Minimum latitude of the requested area</param>
        /// <param name="lonmin">Mimimun longatude of the requested area</param>
        /// <param name="latmax">Maximum latitude of the requesed area </param>
        /// <param name="lonmax">Maximum longatude of the requesed area</param>
        /// <param name="yearmin">The first year that will be used to form time intervals of the request</param>
        /// <param name="daymin">The first day of the year (1..365) that will be used to form time intervals of the request</param>
        /// <param name="hourmin">The time in hours of the day (0..24) that will be used as a lower inclusive bound to form time intervals of the request. Use -999 or 0 to specify the begining of the day</param>
        /// <param name="yearmax">The last year(inclusivly) that will be used to form time intervals for request</param>
        /// <param name="daymax">The last day of the year(inclusivly) that will be used to form time intervals of the request. Use -999 to specify the last day of the year.</param>
        /// <param name="hourmax">The time in hours of the day (0..24) that will be used as an upper inclusive bound to form time intervals of the request. Use -999 or 24 to specify the end of the day</param>
        /// <param name="dlat">A step along latitude that will be used to split requested area into grid</param>
        /// <param name="dlon">A step along longatude that will be used to split requested areia into grid</param>
        /// <param name="dataSource">The data source to use for calculation. By default the data source with lowest uncertainty is chosen automaticly</param> 
        /// <returns>2D array, first dimension of which corresponds to latitude and second to longitude</returns>
        public static string[,] FetchProvenanceGrid(ClimateParameter parameter, double latmin, double latmax, double lonmin, double lonmax, double dlat, double dlon, int hourmin = 0, int hourmax = 24, int daymin = 1, int daymax = GlobalConsts.DefaultValue, int yearmin = 1961, int yearmax = 1990, EnvironmentalDataSource dataSource = EnvironmentalDataSource.ANY)
        {
            SetDefaults(ref hourmin, ref hourmax, ref daymin, ref daymax);
            ITimeRegion tr = new TimeRegion(yearmin, yearmax, daymin, daymax, hourmin, hourmax);
            int nlat = (int)Math.Round((latmax - latmin) / dlat);
            int nlon = (int)Math.Round((lonmax - lonmin) / dlon);
            
            string[] ds = EnvironmentalDataSourceToArrayOfFC2DataSources(dataSource);
            if (ds == null || ds.Length > 1)
            {
                double[] latsGrid = new double[nlat + 1];
                double[] lonsGrid = new double[nlon + 1];
                for (int i = 0; i < latsGrid.Length; i++)
                    latsGrid[i] = latmin + i * dlat;
                for (int i = 0; i < lonsGrid.Length; i++)
                    lonsGrid[i] = lonmin + i * dlon;
                IFetchRequest request = new FetchRequest(
                    ClimateParameterToFC2VariableName(parameter),
                    FetchDomain.CreateCellGrid(
                        latsGrid,
                        lonsGrid,
                        tr),
                    EnvironmentalDataSourceToArrayOfFC2DataSources(dataSource));

                var ids = (UInt16[,])FetchAsync(request).Result["provenance"].GetData();
                int len0 = ids.GetLength(1), len1 = ids.GetLength(0);
                string[,] result = new string[len0, len1];
                for (int i = 0; i < len0; ++i)
                    for (int j = 0; j < len1; ++j) result[i, j] = _DataSources[ids[j, i]];
                return result;
            }
            else
            {
                string[,] result = new string[nlat, nlon];
                for (int i = 0; i < nlat; ++i)
                    for (int j = 0; j < nlon; ++j) result[i, j] = ds[0];
                return result;
            }
        }

        /// <summary>
        /// Split requested area into spatial grid and returns uncertainties for calculated values of the requested parameter.
        /// </summary>
        /// <param name="parameter">A climate parameter that will be evaluated for mean value</param>
        /// <param name="latmin">Minimum latitude of the requested area</param>
        /// <param name="lonmin">Mimimun longatude of the requested area</param>
        /// <param name="latmax">Maximum latitude of the requesed area </param>
        /// <param name="lonmax">Maximum longatude of the requesed area</param>
        /// <param name="yearmin">The first year that will be used to form time intervals of the request</param>
        /// <param name="daymin">The first day of the year (1..365) that will be used to form time intervals of the request</param>
        /// <param name="hourmin">The time in hours of the day (0..24) that will be used as a lower inclusive bound to form time intervals of the request. Use -999 or 0 to specify the begining of the day</param>
        /// <param name="yearmax">The last year(inclusivly) that will be used to form time intervals for request</param>
        /// <param name="daymax">The last day of the year(inclusivly) that will be used to form time intervals of the request. Use -999 to specify the last day of the year.</param>
        /// <param name="hourmax">The time in hours of the day (0..24) that will be used as an upper inclusive bound to form time intervals of the request. Use -999 or 24 to specify the end of the day</param>
        /// <param name="dlat">A step along latitude that will be used to split requested area into grid</param>
        /// <param name="dlon">A step along longatude that will be used to split requested areia into grid</param>
        /// <param name="dataSource">The data source to use for calculation. By default the data source with lowest uncertainty is chosen automaticly</param> 
        /// <returns>2D array, first dimension of which corresponds to latitude and second to longitude</returns>
        public static double[,] FetchUncertaintyGrid(ClimateParameter parameter, double latmin, double latmax, double lonmin, double lonmax, double dlat, double dlon, int hourmin = 0, int hourmax = 24, int daymin = 1, int daymax = GlobalConsts.DefaultValue, int yearmin = 1961, int yearmax = 1990, EnvironmentalDataSource dataSource = EnvironmentalDataSource.ANY)
        {
            SetDefaults(ref hourmin, ref hourmax, ref daymin, ref daymax);
            ITimeRegion tr = new TimeRegion(yearmin, yearmax, daymin, daymax, hourmin, hourmax);
            int nlat = (int)Math.Round((latmax - latmin) / dlat);
            int nlon = (int)Math.Round((lonmax - lonmin) / dlon);
            double[] latsGrid = new double[nlat + 1];
            double[] lonsGrid = new double[nlon + 1];
            for (int i = 0; i < latsGrid.Length; i++)
                latsGrid[i] = latmin + i * dlat;
            for (int i = 0; i < lonsGrid.Length; i++)
                lonsGrid[i] = lonmin + i * dlon;
            var request = new FetchRequest(
                ClimateParameterToFC2VariableName(parameter),
                FetchDomain.CreateCellGrid(
                    latsGrid,
                    lonsGrid,
                    tr),
                EnvironmentalDataSourceToArrayOfFC2DataSources(dataSource));

            var transposed = (double[,])FetchAsync(request).Result["sd"].GetData();
            int len0 = transposed.GetLength(1), len1 = transposed.GetLength(0);
            var result = new double[len0, len1];
            for (int i = 0; i < len0; ++i)
                for (int j = 0; j < len1; ++j) result[i, j] = transposed[j, i];
            return result;
        }

        /// <summary>
        /// Split requested area into spatial grid and returns mean values of the requested parameter for its cells.
        /// </summary>
        /// <param name="parameter">A climate parameter that will be evaluated for mean value</param>
        /// <param name="latmin">Minimum latitude of the requested area</param>
        /// <param name="lonmin">Mimimun longatude of the requested area</param>
        /// <param name="latmax">Maximum latitude of the requesed area </param>
        /// <param name="lonmax">Maximum longatude of the requesed area</param>
        /// <param name="yearmin">The first year that will be used to form time intervals of the request</param>
        /// <param name="daymin">The first day of the year (1..365) that will be used to form time intervals of the request</param>
        /// <param name="hourmin">The time in hours of the day (0..24) that will be used as a lower inclusive bound to form time intervals of the request. Use -999 or 0 to specify the begining of the day</param>
        /// <param name="yearmax">The last year(inclusivly) that will be used to form time intervals for request</param>
        /// <param name="daymax">The last day of the year(inclusivly) that will be used to form time intervals of the request. Use -999 to specify the last day of the year.</param>
        /// <param name="hourmax">The time in hours of the day (0..24) that will be used as an upper inclusive bound to form time intervals of the request. Use -999 or 24 to specify the end of the day</param>
        /// <param name="dlat">A step along latitude that will be used to split requested area into grid</param>
        /// <param name="dlon">A step along longatude that will be used to split requested areia into grid</param>
        /// <param name="dataSource">The data source to use for calculation. By default the data source with lowest uncertainty is chosen automaticly</param> 
        /// <returns>2D array, first dimension of which corresponds to latitude and second to longitude</returns>
        public static double[,] FetchClimateGrid(ClimateParameter parameter, double latmin, double latmax, double lonmin, double lonmax, double dlat, double dlon, int hourmin = 0, int hourmax = 24, int daymin = 1, int daymax = GlobalConsts.DefaultValue, int yearmin = 1961, int yearmax = 1990, EnvironmentalDataSource dataSource = EnvironmentalDataSource.ANY)
        {
            SetDefaults(ref hourmin, ref hourmax, ref daymin, ref daymax);
            ITimeRegion tr = new TimeRegion(yearmin, yearmax, daymin, daymax, hourmin, hourmax);
            int nlat = (int)Math.Round((latmax - latmin) / dlat);
            int nlon = (int)Math.Round((lonmax - lonmin) / dlon);
            double[] latsGrid = new double[nlat + 1];
            double[] lonsGrid = new double[nlon + 1];
            for (int i = 0; i < latsGrid.Length; i++)
                latsGrid[i] = latmin + i * dlat;
            for (int i = 0; i < lonsGrid.Length; i++)
                lonsGrid[i] = lonmin + i * dlon;
            var request = new FetchRequest(
                ClimateParameterToFC2VariableName(parameter),
                FetchDomain.CreateCellGrid(
                    latsGrid,
                    lonsGrid,
                    tr),
                EnvironmentalDataSourceToArrayOfFC2DataSources(dataSource));

            var transposed = (double[,])FetchAsync(request).Result["values"].GetData();
            int len0 = transposed.GetLength(1), len1 = transposed.GetLength(0);
            var result = new double[len0, len1];
            for (int i = 0; i < len0; ++i)
                for (int j = 0; j < len1; ++j) result[i, j] = transposed[j, i];
            return result;
        }

        /// <summary>
        /// Provenance of the time series request (splitting yearmin-yearmax interval by dyear value).
        /// </summary>
        /// <param name="p">A climate parameter that will be evaluated for mean value</param>
        /// <param name="latmin">Minimum latitude of the requested region</param>
        /// <param name="lonmin">Mimimun longatude of the requested region</param>
        /// <param name="latmax">Maximum latitude of the requesed region </param>
        /// <param name="lonmax">Maximum longatude of the requesed region</param>
        /// <param name="yearmin">The first year that will be used to form time intervals of the request. The null value means 1961 (default)</param>
        /// <param name="daymin">The first day of the year (1..365) that will be used to form time intervals of the request. The null value means 1</param>
        /// <param name="hourmin">The time in hours of the day (0..24) that will be used as a lower inclusive bound to form time intervals of the request. The null value means 0</param>
        /// <param name="stopyear">The last year(inclusivly) that will be used to form time intervals for request. The null value means 1990</param>
        /// <param name="daymax">The last day of the year(inclusivly) that will be used to form time intervals of the request. Use GlobalConsts.DefaultValue to specify the last day of the year. The null value means "the last day of the year"</param>
        /// <param name="yearmax">The time in hours of the day (0..24) that will be used as an upper inclusive bound to form time intervals of the request. Use 24 to specify the end of the day. The null value means 24</param>
        /// <param name="dyear">The step that will be used to spit [yearmin,yearmax] interval into a separate intervals. The default value is 1</param>
        /// <param name="dataSource">The data source to use for calculation. By default the data source with lowest uncertainty is chosen automaticly</param> 
        public static string[] FetchClimateYearlyProvenance(ClimateParameter p, double latmin, double latmax, double lonmin, double lonmax, int hourmin = 0, int hourmax = 24, int daymin = 1, int daymax = GlobalConsts.DefaultValue, int yearmin = 1961, int yearmax = 1990, int dyear = 1, EnvironmentalDataSource dataSource = EnvironmentalDataSource.ANY)
        {
            SetDefaults(ref hourmin, ref hourmax, ref daymin, ref daymax);
            ITimeRegion tr = new TimeRegion(yearmin, yearmax, daymin, daymax, hourmin, hourmax).GetYearlyTimeseries(yearmin, yearmax, dyear, true);
            string[] ds = EnvironmentalDataSourceToArrayOfFC2DataSources(dataSource);
            if (ds == null || ds.Length > 1)
            {
                IFetchRequest request = CreateCellOrPointRequest(p, latmin, latmax, lonmin, lonmax, dataSource, tr);

                var ids = (UInt16[,])FetchAsync(request).Result["provenance"].GetData();
                string[] result = new string[ids.GetLength(1)];
                for (int i = 0; i < result.Length; ++i) result[i] = _DataSources[ids[0, i]];
                return result;
            }
            else
            {
                string[] result = new string[tr.SegmentsCount];
                for (int i = 0; i < result.Length; ++i) result[i] = ds[0];
                return result;
            }
        }

        /// <summary>
        /// Uncertainty of the time series request (splitting yearmin-yearmax interval by dyear value).
        /// </summary>
        /// <param name="p">A climate parameter that will be evaluated for mean value</param>
        /// <param name="latmin">Minimum latitude of the requested region</param>
        /// <param name="lonmin">Mimimun longatude of the requested region</param>
        /// <param name="latmax">Maximum latitude of the requesed region </param>
        /// <param name="lonmax">Maximum longatude of the requesed region</param>
        /// <param name="yearmin">The first year that will be used to form time intervals of the request. The null value means 1961 (default)</param>
        /// <param name="daymin">The first day of the year (1..365) that will be used to form time intervals of the request. The null value means 1</param>
        /// <param name="hourmin">The time in hours of the day (0..24) that will be used as a lower inclusive bound to form time intervals of the request. The null value means 0</param>
        /// <param name="stopyear">The last year(inclusivly) that will be used to form time intervals for request. The null value means 1990</param>
        /// <param name="daymax">The last day of the year(inclusivly) that will be used to form time intervals of the request. Use GlobalConsts.DefaultValue to specify the last day of the year. The null value means "the last day of the year"</param>
        /// <param name="yearmax">The time in hours of the day (0..24) that will be used as an upper inclusive bound to form time intervals of the request. Use 24 to specify the end of the day. The null value means 24</param>
        /// <param name="dyear">The step that will be used to spit [yearmin,yearmax] interval into a separate intervals. The default value is 1</param>
        /// <param name="dataSource">The data source to use for calculation. By default the data source with lowest uncertainty is chosen automaticly</param> 
        public static double[] FetchClimateYearlyUncertainty(ClimateParameter p, double latmin, double latmax, double lonmin, double lonmax, int hourmin = 0, int hourmax = 24, int daymin = 1, int daymax = GlobalConsts.DefaultValue, int yearmin = 1961, int yearmax = 1990, int dyear = 1, EnvironmentalDataSource dataSource = EnvironmentalDataSource.ANY)
        {
            SetDefaults(ref hourmin, ref hourmax, ref daymin, ref daymax);
            ITimeRegion tr = new TimeRegion(yearmin, yearmax, daymin, daymax, hourmin, hourmax).GetYearlyTimeseries(yearmin, yearmax, dyear, true);
            IFetchRequest request = CreateCellOrPointRequest(p, latmin, latmax, lonmin, lonmax, dataSource, tr);

            var res = (double[,])FetchAsync(request).Result["sd"].GetData();
            double[] result = new double[res.GetLength(1)];
            for (int i = 0; i < result.Length; ++i) result[i] = res[0, i];
            return result;
        }

        /// <summary>
        /// Time series request (splitting yearmin-yearmax interval by dyear value).
        /// </summary>
        /// <param name="p">A climate parameter that will be evaluated for mean value</param>
        /// <param name="latmin">Minimum latitude of the requested region</param>
        /// <param name="lonmin">Mimimun longatude of the requested region</param>
        /// <param name="latmax">Maximum latitude of the requesed region </param>
        /// <param name="lonmax">Maximum longatude of the requesed region</param>
        /// <param name="yearmin">The first year that will be used to form time intervals of the request. The null value means 1961 (default)</param>
        /// <param name="daymin">The first day of the year (1..365) that will be used to form time intervals of the request. The null value means 1</param>
        /// <param name="hourmin">The time in hours of the day (0..24) that will be used as a lower inclusive bound to form time intervals of the request. The null value means 0</param>
        /// <param name="stopyear">The last year(inclusivly) that will be used to form time intervals for request. The null value means 1990</param>
        /// <param name="daymax">The last day of the year(inclusivly) that will be used to form time intervals of the request. Use GlobalConsts.DefaultValue to specify the last day of the year. The null value means "the last day of the year"</param>
        /// <param name="yearmax">The time in hours of the day (0..24) that will be used as an upper inclusive bound to form time intervals of the request. Use 24 to specify the end of the day. The null value means 24</param>
        /// <param name="dyear">The step that will be used to spit [yearmin,yearmax] interval into a separate intervals. The default value is 1</param>
        /// <param name="dataSource">The data source to use for calculation. By default the data source with lowest uncertainty is chosen automaticly</param> 
        public static double[] FetchClimateYearly(ClimateParameter p, double latmin, double latmax, double lonmin, double lonmax, int hourmin = 0, int hourmax = 24, int daymin = 1, int daymax = GlobalConsts.DefaultValue, int yearmin = 1961, int yearmax = 1990, int dyear = 1, EnvironmentalDataSource dataSource = EnvironmentalDataSource.ANY)
        {
            SetDefaults(ref hourmin, ref hourmax, ref daymin, ref daymax);
            ITimeRegion tr = new TimeRegion(yearmin, yearmax, daymin, daymax, hourmin, hourmax).GetYearlyTimeseries(yearmin, yearmax, dyear, true);
            IFetchRequest request = CreateCellOrPointRequest(p, latmin, latmax, lonmin, lonmax, dataSource, tr);

            var res = (double[,])FetchAsync(request).Result["values"].GetData();
            double[] result = new double[res.GetLength(1)];
            for (int i = 0; i < result.Length; ++i) result[i] = res[0, i];
            return result;
        }

        /// <summary>
        /// Provenance of the time series request (splitting daymin-daymax interval by dyear value).
        /// </summary>
        /// <param name="p">A climate parameter that will be evaluated for mean value</param>
        /// <param name="latmin">Minimum latitude of the requested area</param>
        /// <param name="lonmin">Mimimun longatude of the requested area</param>
        /// <param name="latmax">Maximum latitude of the requesed area </param>
        /// <param name="lonmax">Maximum longatude of the requesed area</param>
        /// <param name="yearmin">The first year that will be used to form time intervals of the request</param>
        /// <param name="daymin">The first day of the year (1..365) that will be used to form time intervals of the request</param>
        /// <param name="hourmin">The time in hours of the day (0..24) that will be used as a lower inclusive bound to form time intervals of the request. Use -999 or 0 to specify the begining of the day</param>
        /// <param name="yearmax">The last year(inclusivly) that will be used to form time intervals for request</param>
        /// <param name="daymax">The last day of the year(inclusivly) that will be used to form time intervals of the request. Use -999 to specify the last day of the year.</param>
        /// <param name="hourmax">The time in hours of the day (0..24) that will be used as an upper inclusive bound to form time intervals of the request. Use -999 or 24 to specify the end of the day</param>
        /// <param name="dyear">The step that will be used to spit [daymin,daymax] interval into a separate intervals. The default value is 1</param>
        /// <param name="dataSource">The data source to use for calculation. By default the data source with lowest uncertainty is chosen automaticly</param> 
        public static string[] FetchClimateSeasonlyProvenance(ClimateParameter p, double latmin, double latmax, double lonmin, double lonmax, int hourmin = 0, int hourmax = 24, int daymin = 1, int daymax = GlobalConsts.DefaultValue, int yearmin = 1961, int yearmax = 1990, int dday = 1, EnvironmentalDataSource dataSource = EnvironmentalDataSource.ANY)
        {
            SetDefaults(ref hourmin, ref hourmax, ref daymin, ref daymax);
            ITimeRegion tr = new TimeRegion(yearmin, yearmax, daymin, daymax, hourmin, hourmax).GetSeasonlyTimeseries(daymin, daymax, dday, true);
            string[] ds = EnvironmentalDataSourceToArrayOfFC2DataSources(dataSource);
            if (ds == null || ds.Length > 1)
            {
                IFetchRequest request = CreateCellOrPointRequest(p, latmin, latmax, lonmin, lonmax, dataSource, tr);

                var ids = (UInt16[,])FetchAsync(request).Result["provenance"].GetData();
                string[] result = new string[ids.GetLength(1)];
                for (int i = 0; i < result.Length; ++i) result[i] = _DataSources[ids[0, i]];
                return result;
            }
            else
            {
                string[] result = new string[tr.SegmentsCount];
                for (int i = 0; i < result.Length; ++i) result[i] = ds[0];
                return result;
            }
        }

        /// <summary>
        /// Uncertatinty of the time series request (splitting daymin-daymax interval by dyear value).
        /// </summary>
        /// <param name="p">A climate parameter that will be evaluated for mean value</param>
        /// <param name="latmin">Minimum latitude of the requested area</param>
        /// <param name="lonmin">Mimimun longatude of the requested area</param>
        /// <param name="latmax">Maximum latitude of the requesed area </param>
        /// <param name="lonmax">Maximum longatude of the requesed area</param>
        /// <param name="yearmin">The first year that will be used to form time intervals of the request</param>
        /// <param name="daymin">The first day of the year (1..365) that will be used to form time intervals of the request</param>
        /// <param name="hourmin">The time in hours of the day (0..24) that will be used as a lower inclusive bound to form time intervals of the request. Use -999 or 0 to specify the begining of the day</param>
        /// <param name="yearmax">The last year(inclusivly) that will be used to form time intervals for request</param>
        /// <param name="daymax">The last day of the year(inclusivly) that will be used to form time intervals of the request. Use -999 to specify the last day of the year.</param>
        /// <param name="hourmax">The time in hours of the day (0..24) that will be used as an upper inclusive bound to form time intervals of the request. Use -999 or 24 to specify the end of the day</param>
        /// <param name="dyear">The step that will be used to spit [daymin,daymax] interval into a separate intervals. The default value is 1</param>
        /// <param name="dataSource">The data source to use for calculation. By default the data source with lowest uncertainty is chosen automaticly</param> 
        public static double[] FetchClimateSeasonlyUncertainty(ClimateParameter p, double latmin, double latmax, double lonmin, double lonmax, int hourmin = 0, int hourmax = 24, int daymin = 1, int daymax = GlobalConsts.DefaultValue, int yearmin = 1961, int yearmax = 1990, int dday = 1, EnvironmentalDataSource dataSource = EnvironmentalDataSource.ANY)
        {
            SetDefaults(ref hourmin, ref hourmax, ref daymin, ref daymax);
            ITimeRegion tr = new TimeRegion(yearmin, yearmax, daymin, daymax, hourmin, hourmax).GetSeasonlyTimeseries(daymin, daymax, dday, true);
            IFetchRequest request = CreateCellOrPointRequest(p, latmin, latmax, lonmin, lonmax, dataSource, tr);

            var res = (double[,])FetchAsync(request).Result["sd"].GetData();
            double[] result = new double[res.GetLength(1)];
            for (int i = 0; i < result.Length; ++i) result[i] = res[0, i];
            return result;
        }

        /// <summary>
        /// Time series request (splitting daymin-daymax interval by dyear value).
        /// </summary>
        /// <param name="p">A climate parameter that will be evaluated for mean value</param>
        /// <param name="latmin">Minimum latitude of the requested area</param>
        /// <param name="lonmin">Mimimun longatude of the requested area</param>
        /// <param name="latmax">Maximum latitude of the requesed area </param>
        /// <param name="lonmax">Maximum longatude of the requesed area</param>
        /// <param name="yearmin">The first year that will be used to form time intervals of the request</param>
        /// <param name="daymin">The first day of the year (1..365) that will be used to form time intervals of the request</param>
        /// <param name="hourmin">The time in hours of the day (0..24) that will be used as a lower inclusive bound to form time intervals of the request. Use -999 or 0 to specify the begining of the day</param>
        /// <param name="yearmax">The last year(inclusivly) that will be used to form time intervals for request</param>
        /// <param name="daymax">The last day of the year(inclusivly) that will be used to form time intervals of the request. Use -999 to specify the last day of the year.</param>
        /// <param name="hourmax">The time in hours of the day (0..24) that will be used as an upper inclusive bound to form time intervals of the request. Use -999 or 24 to specify the end of the day</param>
        /// <param name="dyear">The step that will be used to spit [daymin,daymax] interval into a separate intervals. The default value is 1</param>
        /// <param name="dataSource">The data source to use for calculation. By default the data source with lowest uncertainty is chosen automaticly</param> 
        public static double[] FetchClimateSeasonly(ClimateParameter p, double latmin, double latmax, double lonmin, double lonmax, int hourmin = 0, int hourmax = 24, int daymin = 1, int daymax = GlobalConsts.DefaultValue, int yearmin = 1961, int yearmax = 1990, int dday = 1, EnvironmentalDataSource dataSource = EnvironmentalDataSource.ANY)
        {
            SetDefaults(ref hourmin, ref hourmax, ref daymin, ref daymax);
            ITimeRegion tr = new TimeRegion(yearmin, yearmax, daymin, daymax, hourmin, hourmax).GetSeasonlyTimeseries(daymin, daymax, dday, true);
            IFetchRequest request = CreateCellOrPointRequest(p, latmin, latmax, lonmin, lonmax, dataSource, tr);

            var res = (double[,])FetchAsync(request).Result["values"].GetData();
            double[] result = new double[res.GetLength(1)];
            for (int i = 0; i < result.Length; ++i) result[i] = res[0, i];
            return result;
        }

        /// <summary>
        /// Provenance of the time series request (splitting hourmin-hourmax interval by dyear value).
        /// </summary>
        /// <param name="p">A climate parameter that will be evaluated for mean value</param>
        /// <param name="latmin">Minimum latitude of the requested area</param>
        /// <param name="lonmin">Mimimun longatude of the requested area</param>
        /// <param name="latmax">Maximum latitude of the requesed area </param>
        /// <param name="lonmax">Maximum longatude of the requesed area</param>
        /// <param name="yearmin">The first year that will be used to form time intervals of the request</param>
        /// <param name="daymin">The first day of the year (1..365) that will be used to form time intervals of the request</param>
        /// <param name="hourmin">The time in hours of the day (0..24) that will be used as a lower inclusive bound to form time intervals of the request. Use -999 or 0 to specify the begining of the day</param>
        /// <param name="yearmax">The last year(inclusivly) that will be used to form time intervals for request</param>
        /// <param name="daymax">The last day of the year(inclusivly) that will be used to form time intervals of the request. Use -999 to specify the last day of the year.</param>
        /// <param name="hourmax">The time in hours of the day (0..24) that will be used as an upper inclusive bound to form time intervals of the request. Use -999 or 24 to specify the end of the day</param>
        /// <param name="dyear">The step that will be used to spit [hourmin,hourmax] interval into a separate intervals. The default value is 1</param>
        /// <param name="dataSource">The data source to use for calculation. By default the data source with lowest uncertainty is chosen automaticly</param> 
        public static string[] FetchClimateHourlyProvenance(ClimateParameter p, double latmin, double latmax, double lonmin, double lonmax, int hourmin = 0, int hourmax = 24, int daymin = 1, int daymax = GlobalConsts.DefaultValue, int yearmin = 1961, int yearmax = 1990, int dhour = 1, EnvironmentalDataSource dataSource = EnvironmentalDataSource.ANY)
        {
            SetDefaults(ref hourmin, ref hourmax, ref daymin, ref daymax);
            ITimeRegion tr = new TimeRegion(yearmin, yearmax, daymin, daymax, hourmin, hourmax).GetHourlyTimeseries(hourmin, hourmax, dhour, true);
            string[] ds = EnvironmentalDataSourceToArrayOfFC2DataSources(dataSource);
            if (ds == null || ds.Length > 1)
            {
                IFetchRequest request = CreateCellOrPointRequest(p, latmin, latmax, lonmin, lonmax, dataSource, tr);

                var ids = (UInt16[,])FetchAsync(request).Result["provenance"].GetData();
                string[] result = new string[ids.GetLength(1)];
                for (int i = 0; i < result.Length; ++i) result[i] = _DataSources[ids[0, i]];
                return result;
            }
            else
            {
                string[] result = new string[tr.SegmentsCount];
                for (int i = 0; i < result.Length; ++i) result[i] = ds[0];
                return result;
            }
        }

        /// <summary>
        /// Uncertainty of the time series request (splitting hourmin-hourmax interval by dyear value).
        /// </summary>
        /// <param name="p">A climate parameter that will be evaluated for mean value</param>
        /// <param name="latmin">Minimum latitude of the requested area</param>
        /// <param name="lonmin">Mimimun longatude of the requested area</param>
        /// <param name="latmax">Maximum latitude of the requesed area </param>
        /// <param name="lonmax">Maximum longatude of the requesed area</param>
        /// <param name="yearmin">The first year that will be used to form time intervals of the request</param>
        /// <param name="daymin">The first day of the year (1..365) that will be used to form time intervals of the request</param>
        /// <param name="hourmin">The time in hours of the day (0..24) that will be used as a lower inclusive bound to form time intervals of the request. Use -999 or 0 to specify the begining of the day</param>
        /// <param name="yearmax">The last year(inclusivly) that will be used to form time intervals for request</param>
        /// <param name="daymax">The last day of the year(inclusivly) that will be used to form time intervals of the request. Use -999 to specify the last day of the year.</param>
        /// <param name="hourmax">The time in hours of the day (0..24) that will be used as an upper inclusive bound to form time intervals of the request. Use -999 or 24 to specify the end of the day</param>
        /// <param name="dyear">The step that will be used to spit [hourmin,hourmax] interval into a separate intervals. The default value is 1</param>
        /// <param name="dataSource">The data source to use for calculation. By default the data source with lowest uncertainty is chosen automaticly</param> 
        public static double[] FetchClimateHourlyUncertainty(ClimateParameter p, double latmin, double latmax, double lonmin, double lonmax, int hourmin = 0, int hourmax = 24, int daymin = 1, int daymax = GlobalConsts.DefaultValue, int yearmin = 1961, int yearmax = 1990, int dhour = 1, EnvironmentalDataSource dataSource = EnvironmentalDataSource.ANY)
        {
            SetDefaults(ref hourmin, ref hourmax, ref daymin, ref daymax);
            ITimeRegion tr = new TimeRegion(yearmin, yearmax, daymin, daymax, hourmin, hourmax).GetHourlyTimeseries(hourmin, hourmax, dhour, true);
            IFetchRequest request = CreateCellOrPointRequest(p, latmin, latmax, lonmin, lonmax, dataSource, tr);

            var res = (double[,])FetchAsync(request).Result["sd"].GetData();
            double[] result = new double[res.GetLength(1)];
            for (int i = 0; i < result.Length; ++i) result[i] = res[0, i];
            return result;
        }

        /// <summary>
        /// Time series request (splitting hourmin-hourmax interval by dyear value).
        /// </summary>
        /// <param name="p">A climate parameter that will be evaluated for mean value</param>
        /// <param name="latmin">Minimum latitude of the requested area</param>
        /// <param name="lonmin">Mimimun longatude of the requested area</param>
        /// <param name="latmax">Maximum latitude of the requesed area </param>
        /// <param name="lonmax">Maximum longatude of the requesed area</param>
        /// <param name="yearmin">The first year that will be used to form time intervals of the request</param>
        /// <param name="daymin">The first day of the year (1..365) that will be used to form time intervals of the request</param>
        /// <param name="hourmin">The time in hours of the day (0..24) that will be used as a lower inclusive bound to form time intervals of the request. Use -999 or 0 to specify the begining of the day</param>
        /// <param name="yearmax">The last year(inclusivly) that will be used to form time intervals for request</param>
        /// <param name="daymax">The last day of the year(inclusivly) that will be used to form time intervals of the request. Use -999 to specify the last day of the year.</param>
        /// <param name="hourmax">The time in hours of the day (0..24) that will be used as an upper inclusive bound to form time intervals of the request. Use -999 or 24 to specify the end of the day</param>
        /// <param name="dyear">The step that will be used to spit [hourmin,hourmax] interval into a separate intervals. The default value is 1</param>
        /// <param name="dataSource">The data source to use for calculation. By default the data source with lowest uncertainty is chosen automaticly</param> 
        public static double[] FetchClimateHourly(ClimateParameter p, double latmin, double latmax, double lonmin, double lonmax, int hourmin = 0, int hourmax = 24, int daymin = 1, int daymax = GlobalConsts.DefaultValue, int yearmin = 1961, int yearmax = 1990, int dhour = 1, EnvironmentalDataSource dataSource = EnvironmentalDataSource.ANY)
        {
            SetDefaults(ref hourmin, ref hourmax, ref daymin, ref daymax);
            ITimeRegion tr = new TimeRegion(yearmin, yearmax, daymin, daymax, hourmin, hourmax).GetHourlyTimeseries(hourmin, hourmax, dhour, true);
            IFetchRequest request = CreateCellOrPointRequest(p, latmin, latmax, lonmin, lonmax, dataSource, tr);

            var res = (double[,])FetchAsync(request).Result["values"].GetData();
            double[] result = new double[res.GetLength(1)];
            for (int i = 0; i < result.Length; ++i) result[i] = res[0, i];
            return result;
        }
    }
}
