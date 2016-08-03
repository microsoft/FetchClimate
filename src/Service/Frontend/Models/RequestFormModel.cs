using Microsoft.Research.Science.FetchClimate2;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Frontend.Models
{
    public class GeoPoint 
    {
        private readonly double lat,lon;

        public GeoPoint(double lat, double lon)
        {
            this.lat = lat;
            this.lon = lon;
        }

        public double Latitude 
        {
            get { return lat; }
        }

        public double Longitude 
        {
            get { return lon; }
        }
    }

    public class GeoGrid
    {
        private readonly double[] lat,lon;

        public GeoGrid(double[] lat, double[] lon)
        {
            this.lat = lat;
            this.lon = lon;
        }

        public GeoGrid(double latmin, double latmax, int latcount,
            double lonmin, double lonmax, int loncount)
        {
            lat = Enumerable.Range(0,latcount).Select(i => latmin + i * (latmax - latmin) / (latcount - 1)).ToArray();
            lon = Enumerable.Range(0,loncount).Select(i => lonmin + i * (lonmax - lonmin) / (loncount - 1)).ToArray();
        }

        public double[] Latitudes
        {
            get { return lat; }
        }

        public double[] Longitudes 
        {
            get { return lon; }
        }
    }

    public enum YearsMode
    {
        Cells, Points
    }

    public enum DaysMode
    {
        Cells, Points, MonthlyCells, EntireYear
    }

    public enum HoursMode
    {
        Cells, Points, EntireDay
    }

    public class RequestFormModel
    {
        private readonly ExtendedConfiguration config;
        private readonly Dictionary<string, List<string>> variables = new Dictionary<string, List<string>>(); // Variable name -> list of data source names
        private readonly List<GeoPoint> points = new List<GeoPoint>();
        private readonly List<GeoGrid> grids = new List<GeoGrid>();
        private readonly ITimeRegion tr = new TimeRegion();
        private readonly string variableErrors = "";
        private readonly string regionErrors = "";
        private readonly string intervalErrors = "";

        public RequestFormModel(ExtendedConfiguration config)
        {
            this.config = config;
            RegionsText = "51.53,-0.18\n55.77,37.64\n-23.10,-43.22";
            
            YearsMode = Models.YearsMode.Cells;
            YearCellStart = "1980";
            YearCellEnd = "2000";
            YearCellSize = "20";
            IndividualYearStart = "1980";
            IndividualYearEnd = "2000";
            IndividualYearStep = "5";

            DaysMode = Models.DaysMode.MonthlyCells;
            DayCellStart = "1";
            DayCellEnd = "365";
            DayCellSize = "7";
            IndividualDayStart = "1";
            IndividualDayStep = "7";
            IndividualDayEnd = "365";

            HoursMode = Models.HoursMode.EntireDay;
            HourCellStart = "0";
            HourCellEnd = "24";
            HourCellSize = "6";
            IndividualHourStart = "0";
            IndividualHourEnd = "24";
            IndividualHourStep = "6";
        }

        public RequestFormModel(ExtendedConfiguration config, NameValueCollection form, bool fromPost) : this(config)
        {
            if (fromPost)
            {
                // Get selected variables from form
                foreach (string key in form)
                    if (key.StartsWith("variable_"))
                    {
                        int idx = key.LastIndexOf("_");
                        var vname = key.Substring(9, idx - 9);
                        var dsID = Int32.Parse(key.Substring(idx + 1));
                        List<string> dsList;
                        if (!variables.TryGetValue(vname, out dsList))
                        {
                            dsList = new List<string>();
                            variables.Add(vname, dsList);
                        }
                        dsList.Add(config.DataSources.Where(ds => ds.ID == dsID).First().Name);
                    }
                if (variables.Count == 0)
                    variableErrors = "No variables selected";

                try
                {
                    ParseRegion(RegionsText = form["regionText"]);
                }
                catch (Exception exc)
                {
                    regionErrors = exc.Message;
                }

                YearCellStart = (string)form["yearCellStart"];
                YearCellEnd = (string)form["yearCellEnd"];
                YearCellSize = (string)form["yearCellSize"];
                IndividualYearStart = (string)form["indYearStart"];
                IndividualYearEnd = (string)form["indYearEnd"];
                IndividualYearStep = (string)form["indYearStep"];
                try
                {
                    switch ((string)form["yearsMode"])
                    {
                        case "cells":
                            tr = tr.GetYearlyTimeseries(Int32.Parse(YearCellStart), Int32.Parse(YearCellEnd), Int32.Parse(YearCellSize), true);
                            YearsMode = Models.YearsMode.Cells;
                            break;
                        case "years":
                            tr = tr.GetYearlyTimeseries(Int32.Parse(IndividualYearStart), Int32.Parse(IndividualYearEnd), Int32.Parse(IndividualYearStep), true);
                            YearsMode = Models.YearsMode.Points;
                            break;
                    }
                }
                catch (Exception exc)
                {
                    intervalErrors = "Year axis specification has errors: " + exc.Message;
                }

                DayCellStart = (string)form["dayCellStart"];
                DayCellEnd = (string)form["dayCellEnd"];
                DayCellSize = (string)form["dayCellSize"];
                IndividualDayStart = (string)form["inDayStart"];
                IndividualDayEnd = (string)form["inDayEnd"];
                IndividualDayStep = (string)form["inDayStep"];
                try
                {
                    switch ((string)form["daysMode"])
                    {
                        case "cells":
                            tr = tr.GetSeasonlyTimeseries(Int32.Parse(DayCellStart), Int32.Parse(DayCellEnd), Int32.Parse(DayCellSize), true);
                            break;
                        case "days":
                            tr = tr.GetSeasonlyTimeseries(Int32.Parse(IndividualDayStart), Int32.Parse(IndividualDayEnd), Int32.Parse(IndividualDayEnd), true);
                            break;
                        case "monthly":
                            tr = tr.GetMonthlyTimeseries();
                            break;
                    }
                }
                catch (Exception exc)
                {
                    if (!String.IsNullOrEmpty(intervalErrors))
                        intervalErrors += "\n";
                    intervalErrors += "Days axis specification has errors: " + exc.Message;
                }

                HourCellStart = (string)form["hourCellStart"];
                HourCellEnd = (string)form["hourCellEnd"];
                HourCellSize = (string)form["hourCellSize"];
                IndividualHourStart = (string)form["indHourStart"];
                IndividualHourEnd = (string)form["indHourEnd"];
                IndividualHourStep = (string)form["indHourStep"];
                try
                {
                    switch ((string)form["hoursMode"])
                    {
                        case "cells":
                            tr = tr.GetHourlyTimeseries(Int32.Parse(HourCellStart), Int32.Parse(HourCellEnd), Int32.Parse(HourCellSize), true);
                            break;
                        case "hours":
                            tr = tr.GetHourlyTimeseries(Int32.Parse(IndividualHourStart), Int32.Parse(IndividualHourEnd), Int32.Parse(IndividualHourStep), true);
                            break;
                    }
                }
                catch (Exception exc)
                {
                    if (!String.IsNullOrEmpty(intervalErrors))
                        intervalErrors += "\n";
                    intervalErrors += "Hours axis specification has errors: " + exc.Message;
                }
            }
            else // From get
            {
                string v = form["v"];
                if (!String.IsNullOrEmpty(v))
                {
                    var names = Uri.UnescapeDataString(v).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach(var n in names)
                        if (config.EnvironmentalVariables.Any(vd => vd.Name == n))
                            variables.Add(n, 
                                config.DataSources.Where(d => d.ProvidedVariables.Contains(n)).Select(d => d.Name).ToList());
                }

                string p = form["p"];
                if (!String.IsNullOrEmpty(p))
                {
                    var latLons = Uri.UnescapeDataString(p).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < latLons.Length - 1; i+=2)
                    {
                        double lat, lon;
                        if (Double.TryParse(latLons[i], out lat) && Double.TryParse(latLons[i + 1], out lon))
                            points.Add(new GeoPoint(lat, lon));
                    }
                }

                string g = form["g"];
                if (!String.IsNullOrEmpty(g))
                {
                    var gp = Uri.UnescapeDataString(g).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < gp.Length; i += 6)
                    {
                        double latmin, latmax, lonmin, lonmax;
                        int latcount, loncount;
                        if (Double.TryParse(gp[0], out latmin) && Double.TryParse(gp[1], out latmax) && Int32.TryParse(gp[2], out latcount) &&
                            Double.TryParse(gp[3], out lonmin) && Double.TryParse(gp[4], out lonmax) && Int32.TryParse(gp[5], out loncount) &&
                            latmin < latmax && lonmin < lonmax && latcount > 1 && loncount > 1)
                            grids.Add(new GeoGrid(latmin, latmax, latcount, lonmin, lonmax, loncount));
                    }
                }
            }
        }

        public RequestFormModel(ExtendedConfiguration config, Stream requestStream) : this(config)
        {
            try
            {
                StreamReader reader = new StreamReader(requestStream, Encoding.UTF8);
                var requests =
                    JsonConvert.DeserializeObject<Microsoft.Research.Science.FetchClimate2.Serializable.FetchRequest[]>(reader.ReadToEnd());
                if (requests.Length > 0)
                {
                    var firstVariable = requests[0].EnvironmentVariableName;
                    for (var i = 0; i < requests.Length && requests[i].EnvironmentVariableName == firstVariable; i++)
                    {
                        if (requests[i].Domain.SpatialRegionType == "Points")
                            points.AddRange(requests[i].Domain.Lats.Zip(requests[i].Domain.Lons, (lat, lon) => new GeoPoint(lat, lon)));
                        else if (requests[i].Domain.SpatialRegionType == "CellGrid")
                            grids.Add(new GeoGrid(requests[i].Domain.Lats, requests[i].Domain.Lons));
                        else
                            throw new Exception("Region type \'" + requests[i].Domain.SpatialRegionType + "\' is not supported");
                    }

                    foreach (var r in requests)
                    {
                        if (!variables.ContainsKey(r.EnvironmentVariableName))
                        {
                            if (!config.EnvironmentalVariables.Any(vd => vd.Name == r.EnvironmentVariableName))
                                throw new Exception("Variable " + r.EnvironmentVariableName + " is not availble in current service configuration");
                            variables.Add(r.EnvironmentVariableName, r.ParticularDataSources.ToList());
                        }
                    }
                }
                RegionsText = GetRegionsText();
            }
            catch(Exception exc) 
            {
                RequestUploadErrors = exc.Message;
            }
        }

        public string RegionErrors
        {
            get { return regionErrors; }
        }

        public string IntervalErrors
        {
            get { return intervalErrors; }
        }

        public string VariableErrors
        {
            get { return variableErrors; }
        }

        public string RequestUploadErrors { get; set; }


        public bool HasErrors
        {
            get
            {
                return 
                    !String.IsNullOrEmpty(regionErrors) || 
                    !String.IsNullOrEmpty(intervalErrors) || 
                    !String.IsNullOrEmpty(RequestUploadErrors) ||
                    !String.IsNullOrEmpty(variableErrors);
            }
        }

        #region Converters and parsers

        private void ParseRegion(string text)
        {
            List<int> errors = new List<int>();

            var lines = text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lines.Length && errors.Count < 11; i++)
            {
                lines[i] = lines[i].Trim();
                if (lines[i].Length == 0)
                    continue;

                var latLon = lines[i].Split(new char[] { ',' });
                if (latLon.Length != 2)
                {
                    errors.Add(i + 1); // Store line number
                    continue;
                }
                if (latLon[0].Contains(':') && latLon[1].Contains(':'))
                {
                    double[] lat = ParseMatlabSequence(latLon[0]);
                    double[] lon = ParseMatlabSequence(latLon[1]);
                    if (lat != null && lon != null)
                        grids.Add(new GeoGrid(lat, lon));
                    else
                        errors.Add(i + 1);
                }
                else
                {
                    double lat, lon;
                    if (Double.TryParse(latLon[0], NumberStyles.Number, CultureInfo.InvariantCulture, out lat) && lat >= -90 && lat <= 90 &&
                       Double.TryParse(latLon[1], NumberStyles.Number, CultureInfo.InvariantCulture, out lon) && lon >= -180 && lon <= 360)
                        points.Add(new GeoPoint(lat,lon));
                    else
                        errors.Add(i + 1);
                }
            }

            if(points.Count == 0 && grids.Count == 0)
                throw new ArgumentException("No points or grids specified");

            if (errors.Count > 0)
                throw new ArgumentException(String.Concat("Syntax error(s) at line(s) ",
                    String.Concat(errors.Skip(1).Aggregate(errors.First().ToString(), (s,e) => String.Concat(s,',',e.ToString())))));
        }

        private static double[] ParseMatlabSequence(string s)
        {
            string[] parts = s.Split(':');
            double start = Double.Parse(parts[0], CultureInfo.InvariantCulture);
            double step = Double.Parse(parts[1], CultureInfo.InvariantCulture);
            double end = Double.Parse(parts[2], CultureInfo.InvariantCulture);
            double eps = Math.Min(1e-6, step * 1e-6);
            List<double> result = new List<double>();
            for (double x = start; x <= end - eps; x += step)
                result.Add(x);
            return result.ToArray();
        }

        private static string GetMatlabSequenceText(double[] s)
        {
            if (s.Length == 0)
                return "";
            else if (s.Length == 1)
                return s[0].ToString(CultureInfo.InvariantCulture);
            else if (s.Length == 2)
                return String.Format(CultureInfo.InvariantCulture, "{0},{1}", s[0], s[1]);
            else
            {
                double start = s[0];
                double end = s[s.Length - 1];
                double step = s[1] - s[0];
                double eps = (end - start) * 1e-6;
                bool isUniform = true;
                for (int i = 2; i < s.Length && isUniform; i++)
                    isUniform = Math.Abs(step - s[i] + s[i - 1]) < eps;
                return isUniform ?
                    String.Format(CultureInfo.InvariantCulture, "{0}:{1}:{2}", start, step, end) :
                    String.Concat(s.Skip(1).Aggregate(
                        s[0].ToString(CultureInfo.InvariantCulture),
                        (str,val) => String.Concat(str, ",", val.ToString(CultureInfo.InvariantCulture))));
            }
        }

        /// <summary>Gets string representation</summary>
        /// <returns></returns>
        private string GetRegionsText()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var gp in points)
                sb.Append(gp.Latitude.ToString(CultureInfo.InvariantCulture)).Append(", ").AppendLine(gp.Longitude.ToString(CultureInfo.InvariantCulture));
            foreach (var g in grids)
            {
                sb.Append(GetMatlabSequenceText(g.Latitudes));
                sb.Append(",");
                sb.Append(GetMatlabSequenceText(g.Longitudes));
                sb.Append("\n");
            }
            return sb.ToString();
        }

        #endregion

        public ExtendedConfiguration Configuration
        {
            get { return config; }
        }

        public string RegionsText { get; private set; }

        public YearsMode YearsMode { get; private set; }

        public string YearCellSize { get; private set; }

        public string YearCellStart { get; private set; }

        public string YearCellEnd { get; private set; }

        public string IndividualYearStep { get; private set; }

        public string IndividualYearStart { get; private set; }

        public string IndividualYearEnd { get; private set; }

        public DaysMode DaysMode { get; private set; }

        public string DayCellSize { get; private set; }

        public string DayCellStart { get; private set; }

        public string DayCellEnd { get; private set; }

        public string IndividualDayStep { get; private set; }

        public string IndividualDayStart { get; private set; }

        public string IndividualDayEnd { get; private set; }

        public HoursMode HoursMode { get; private set; }

        public string HourCellSize { get; private set; }

        public string HourCellStart { get; private set; }

        public string HourCellEnd { get; private set; }

        public string IndividualHourStep { get; private set; }

        public string IndividualHourStart { get; private set; }

        public string IndividualHourEnd { get; private set; }        

        public Dictionary<string, List<string>> Variables
        {
            get { return variables; }
        }

        public bool IsDataSourceSelected(string vname, string dsname)
        {
            List<string> ds;
            return variables.TryGetValue(vname, out ds) && ds.Contains(dsname);
        }

        public string GetFormCheckBoxName(IVariableDefinition v, DataSourceDefinition ds)
        {
            return String.Concat("variable_", v.Name, "_", ds.ID.ToString());
        }

        public List<GeoPoint> Points
        {
            get { return points;  }
        }

        public IEnumerable<GeoGrid> Grids
        {
            get { return grids; }
        }

        public string GetRequestText()
        {
            // Fill requests list so that regions are enumerated first and variables
            // are enumerated in outer loop
            var requests = new List<FetchRequest>();
            var pointsEnum = points.Count > 0 ? GetRequestsForPoints().GetEnumerator() : null;
            var gridsEnum = grids.Select(g => GetRequestsForGrid(g).GetEnumerator()).ToArray();
            while (true)
            {
                if (pointsEnum != null)
                {
                    if (!pointsEnum.MoveNext())
                        break;
                    requests.Add(pointsEnum.Current);
                }
                bool stop = false;
                foreach (var e in gridsEnum)
                {
                    if (!e.MoveNext()) {
                        stop = true;
                        break;
                    }
                    requests.Add(e.Current);
                }
                if (stop)
                    break;
            }
            return JsonConvert.SerializeObject(requests.Select(r => new Microsoft.Research.Science.FetchClimate2.Serializable.FetchRequest(r)).ToArray(), Formatting.Indented);
        }

        public IEnumerable<FetchRequest> GetRequestsForPoints()
        {
            foreach (var pair in variables)
            {
                yield return new FetchRequest(
                    pair.Key,
                    FetchDomain.CreatePoints(
                        points.Select(p => p.Latitude).ToArray(), 
                        points.Select(p => p.Longitude).ToArray(), tr),
                    config.TimeStamp,
                    pair.Value.ToArray());
            }
            yield break;
        }

        public IEnumerable<FetchRequest> GetRequestsForGrid(GeoGrid g)
        {
            foreach (var pair in variables)
            {
                yield return new FetchRequest(
                    pair.Key,
                    FetchDomain.CreateCellGrid(g.Latitudes, g.Longitudes, tr),
                    config.TimeStamp,
                    pair.Value.ToArray());
            }
            yield break;
        }

        public string GetClientUrlParameters()
        {
            StringBuilder sb = new StringBuilder();
            
            sb.Append("v=");
            var names = variables.Keys.ToArray();
            for(var i =0;i<names.Length;i++) {
                if (i > 0)
                    sb.Append(",");
                sb.Append(names[i]);
            }

            if (points.Count > 0)
            {
                sb.Append("&p=");
                for (var i = 0; i < points.Count; i++)
                {
                    if (i > 0)
                        sb.Append(",");
                    sb.Append(points[i].Latitude);
                    sb.Append(",");
                    sb.Append(points[i].Longitude);
                }
            }

            if (grids.Count > 0)
            {
                sb.Append("&g=");
                for (var i = 0; i < grids.Count; i++)
                {
                    if (i > 0)
                        sb.Append(",");
                    double[] lat = grids[i].Latitudes;
                    double[] lon = grids[i].Longitudes;
                    sb.Append(lat[0]);
                    sb.Append(",");
                    sb.Append(lat[lat.Length - 1]);
                    sb.Append(",");
                    sb.Append(lat.Length);
                    sb.Append(",");
                    sb.Append(lon[0]);
                    sb.Append(",");
                    sb.Append(lon[lon.Length - 1]);
                    sb.Append(",");
                    sb.Append(lon.Length);
                }
            }

            return sb.ToString();
        }
    }
}