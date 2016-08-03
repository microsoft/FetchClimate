using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Research.Science.Data;

namespace Microsoft.Research.Science.FetchClimate2
{
    // index * start * end
    using TIndex = Tuple<int, int, int>; 

    internal sealed class SIndex
    {
        public int I, J;
        public double Lon, Lat, LatMin, LonMin, LatMax, LonMax;
        public string Region;
    }

    internal struct Cell
    {
        public double Lon, Lat, LatMin, LonMin, LatMax, LonMax;
        public DateTime Start, End;

        public override int GetHashCode()
        {
            return LatMin.GetHashCode() ^ LonMin.GetHashCode() ^ LatMax.GetHashCode() ^ LonMax.GetHashCode() ^ Start.GetHashCode() ^ End.GetHashCode();
        }
    }

    internal class Variables
    {
        public Variable<double> Values, Uncertainty;
        public Variable<string> Provenance;
        public int RowCount;
    }


    public static class TableExportHelper
    {
        private static IEnumerable<TIndex> EnumerateHours(ITimeRegion tr)
        {
            bool isAxisReduced = tr.HoursAxisLength == 1;
            var hours = tr.Hours;
            if (tr.IsIntervalsGridHours)
            {
                for (var i = 0; i < hours.Length - 1; i++)
                    yield return new TIndex(isAxisReduced ? -1 : i, hours[i], hours[i + 1]);
            }
            else
            {
                for (var i = 0; i < hours.Length; i++)
                    yield return new TIndex(isAxisReduced ? -1 : i, hours[i], hours[i]);
            }
            yield break;
        }

        private static IEnumerable<TIndex> EnumerateDays(ITimeRegion tr, bool isLeapYear)
        {
            bool isAxisReduced = tr.DaysAxisLength == 1;
            var days = (int[])tr.Days;
            if (tr.IsIntervalsGridDays)
            {
                if (isLeapYear && days.Length == 13 && // Adjust monthly bounds for leap year
                    days[0] == 1 && days[1] == 32 && days[2] == 60 && days[3] == 91 &&
                    days[4] == 121 && days[5] == 152 && days[6] == 182 && days[7] == 213 &&
                    days[8] == 244 && days[9] == 274 && days[10] == 305 && days[11] == 335 && days[12] == 366)
                {
                    days = (int[])days.Clone();
                    for (var i = 2; i < 13; i++)
                        days[i]++;
                }
                else if (isLeapYear && days.Length == 2 && days[0] == 1 && days[1] == 366)
                { // Adjust end of year for entire year
                    days = (int[])days.Clone();
                    days[1] = 367;
                }
                for (var i = 0; i < days.Length - 1; i++)
                    yield return new TIndex(isAxisReduced ? -1 : i, days[i], days[i + 1] - 1);
            }
            else
            {
                
                for (var i = 0; i < days.Length; i++)
                    yield return new TIndex(isAxisReduced ? -1 : i, days[i], days[i]);
            }
            yield break;
        }

        private static IEnumerable<TIndex> EnumerateYears(ITimeRegion tr)
        {
            var years = tr.Years;
            if (tr.IsIntervalsGridYears)
            {
                bool isAxisReduced = years.Length == 2;
                for (var i = 0; i < years.Length - 1; i++)
                    yield return new TIndex(isAxisReduced ? -1 : i, years[i], years[i + 1] - 1);
            }
            else
            {
                bool isAxisReduced = years.Length == 1;
                for (var i = 0; i < years.Length; i++)
                    yield return new TIndex(isAxisReduced ? -1 : i, years[i], years[i]);
            }
            yield break;
        }

        private static IEnumerable<SIndex> EnumerateGeo(IFetchDomain fd, string[] regions)
        {
            var lat = fd.Lats;
            var lon = fd.Lons;
            var latmax = fd.Lats2;
            var lonmax = fd.Lons2;
            switch (fd.SpatialRegionType)
            {
                case SpatialRegionSpecification.CellGrid:
                    for (var i = 0; i < lon.Length - 1; i++)
                        for (var j = 0; j < lat.Length - 1; j++)
                            yield return new SIndex
                            {
                                I = i,
                                J = j,
                                Lon = (lon[i] + lon[i + 1]) / 2,
                                Lat = (lat[j] + lat[j + 1]) / 2,
                                LonMin = lon[i],
                                LatMin = lat[j],
                                LonMax = lon[i + 1],
                                LatMax = lat[j + 1],
                                Region = regions == null || regions.Length == 0 ? "" : regions[0]
                            };
                    break;
                case SpatialRegionSpecification.Cells:
                    for (var i = 0; i < lon.Length; i++)
                        yield return new SIndex
                        {
                            I = i,
                            J = -1,
                            Lon = (lon[i] + lonmax[i]) / 2,
                            Lat = (lat[i] + latmax[i]) / 2,
                            LonMin = lon[i],
                            LatMin = lat[i],
                            LonMax = lonmax[i],
                            LatMax = latmax[i],
                            Region = regions == null || i >= regions.Length ? "" : regions[i]
                        };
                    break;
                case SpatialRegionSpecification.PointGrid:
                    for (var i = 0; i < lon.Length; i++)
                        for (var j = 0; j < lat.Length; j++)
                            yield return new SIndex
                            {
                                I = i,
                                J = j,
                                Lon = lon[i],
                                Lat = lat[j],
                                LonMin = lon[i],
                                LatMin = lat[j],
                                LonMax = lon[i],
                                LatMax = lat[j],
                                Region = regions == null || regions.Length == 0 ? "" : regions[0]
                            };
                    break;
                case SpatialRegionSpecification.Points:
                    for(var i =0;i<lon.Length;i++)
                            yield return new SIndex
                            {
                                I = i,
                                J = i,
                                Lon = lon[i],
                                Lat = lat[i],
                                LonMin = lon[i],
                                LatMin = lat[i],
                                LonMax = lon[i],
                                LatMax = lat[i],
                                Region = regions == null || i >= regions.Length ? "" : regions[i]
                            };                                
                    break;
            }
            yield break;
        }

        private static IEnumerable<Tuple<Cell, double, double, UInt16, string>> Linearize(DataSet src, string[] regions)
        {
            var fr = src.ToFetchRequest();
            var dimCount = 0;
            int yearIdx = -1, dayIdx = -1, hourIdx = -1, iIdx = -1, jIdx = -1;

            if (fr.Domain.SpatialRegionType == SpatialRegionSpecification.Cells || fr.Domain.SpatialRegionType == SpatialRegionSpecification.Points)
            {
                iIdx = dimCount++;
            }
            else
            {
                iIdx = dimCount++;
                jIdx = dimCount++;
            }
            if (fr.Domain.TimeRegion.YearsAxisLength > 1)
                yearIdx = dimCount++;
            if (fr.Domain.TimeRegion.DaysAxisLength > 1)
                dayIdx = dimCount++;
            if (fr.Domain.TimeRegion.HoursAxisLength > 1)
                hourIdx = dimCount++;
            var idx = new int[dimCount];

            var values = src[RequestDataSetFormat.ValuesVariableName].GetData();
            var uncertainty = src[RequestDataSetFormat.UncertaintyVariableName].GetData();
            Array provenance = null;
            if(src.Variables.Contains(RequestDataSetFormat.ProvenanceVariableName))
                provenance = src[RequestDataSetFormat.ProvenanceVariableName].GetData();

            foreach (var si in EnumerateGeo(fr.Domain, regions))
            {
                if (iIdx != -1)
                    idx[iIdx] = si.I;
                if (jIdx != -1)
                    idx[jIdx] = si.J;
                foreach (var yi in EnumerateYears(fr.Domain.TimeRegion))
                {
                    if (yearIdx != -1)
                        idx[yearIdx] = yi.Item1;
                    bool isLeap = yi.Item2 == yi.Item3 && DateTime.IsLeapYear(yi.Item2);
                    foreach (var di in EnumerateDays(fr.Domain.TimeRegion, isLeap))
                    {
                        if (dayIdx != -1)
                            idx[dayIdx] = di.Item1;
                        foreach(var hi in EnumerateHours(fr.Domain.TimeRegion)) {
                            if(hourIdx != -1)
                                idx[hourIdx] = hi.Item1;
                            yield return new Tuple<Cell, double, double, UInt16, string>(
                                new Cell 
                                {
                                    Lon = si.Lon, Lat = si.Lat,
                                    LonMin = si.LonMin, LatMin = si.LatMin, 
                                    LonMax = si.LonMax, LatMax = si.LatMax,                                     
                                    Start = new DateTime(yi.Item2,1,1) + TimeSpan.FromDays(di.Item2 - 1) + TimeSpan.FromHours(hi.Item2),
                                    End = new DateTime(yi.Item3,1,1) + TimeSpan.FromDays(di.Item3 - 1) + TimeSpan.FromHours(hi.Item3)
                                },
                                (double)values.GetValue(idx),
                                (double)uncertainty.GetValue(idx),
                                provenance != null ? (UInt16)provenance.GetValue(idx) : (UInt16)65535,
                                si.Region);
                        }
                    }
                }
            }
            yield break;
        }

        public static void MergeTable(IFetchConfiguration config, DataSet dst, Tuple<DataSet, string[]>[] requests)
        {
            // For faster lookup of data source name from id
            var id2name = new Dictionary<int, string>();
            foreach (var dsd in config.DataSources)
                id2name.Add(dsd.ID, dsd.Name);

            var var2var = new Dictionary<string, Variables>(); // Environment variable short name => data set variables
            var cell2row = new Dictionary<Cell, int>(); // Space-time cell => row number
            var rowCount = 0;
            var optionalRowCount = 0;
            var regionRowCount = 0;

            var regions = dst.AddVariable<string>("region", "i");
            var lat = dst.AddVariable<double>("lat", "i");
            var lon = dst.AddVariable<double>("lon", "i");
            Variable<double> latmin = null, latmax = null, lonmin = null, lonmax = null;
            var start = dst.AddVariable<DateTime>("start", "i");
            var end = dst.AddVariable<DateTime>("end", "i");
            
            for (var i = 0; i < requests.Length; i++)
            {
                using(var src = requests[i].Item1.Clone("msds:memory"))
                {
                    var name = src.Metadata[RequestDataSetFormat.EnvironmentVariableNameKey].ToString();

                    // Define data source name to use when not provenance is supplied
                    var noProvDataSource = ""; 
                    if (src.Metadata.ContainsKey(RequestDataSetFormat.DataSourceNameKey))
                    {
                        var requestedDataSources = (string[])src.Metadata[RequestDataSetFormat.DataSourceNameKey];
                        if (requestedDataSources.Length == 1)
                            noProvDataSource = requestedDataSources[0];
                    }

                    var envVar = config.EnvironmentalVariables.Where(ev => ev.Name == name).First();
                    Variables variables;
                    if (!var2var.TryGetValue(name, out variables))
                    {
                        variables = new Variables();
                        variables.Values = dst.AddVariable<double>(String.Concat(name, " (", envVar.Units, ")"), "i");
                        variables.Values.MissingValue = Double.NaN;
                        variables.Uncertainty = dst.AddVariable<double>(String.Concat(name, "_uncertainty"), "i");
                        variables.Uncertainty.MissingValue = Double.NaN;
                        variables.Provenance = dst.AddVariable<string>(String.Concat(name, "_provenance"), "i");
                        variables.Provenance.MissingValue = null;
                        var2var.Add(name, variables);
                    }

                    foreach (var t in Linearize(src, requests[i].Item2))
                    {

                        int row;
                        if (!cell2row.TryGetValue(t.Item1, out row))
                        {
                            row = rowCount++;
                            cell2row.Add(t.Item1, row);
                        }

                        if (t.Item5 != "")
                        {
                            regions[row] = t.Item5;
                            regionRowCount = Math.Max(regionRowCount, row);
                        }
                        lat[row] = t.Item1.Lat;
                        lon[row] = t.Item1.Lon;
                        start[row] = t.Item1.Start;
                        end[row] = t.Item1.End;
                        if (t.Item1.LatMin != t.Item1.Lat)
                        {
                            if (latmin == null)
                            {
                                latmin = dst.AddVariable<double>("latmin", "i");
                                latmax = dst.AddVariable<double>("latmax", "i");
                                lonmin = dst.AddVariable<double>("lonmin", "i");
                                lonmax = dst.AddVariable<double>("lonmax", "i");
                            }
                            latmin[row] = t.Item1.LatMin;
                            latmax[row] = t.Item1.LatMax;
                            lonmin[row] = t.Item1.LonMin;
                            lonmax[row] = t.Item1.LonMax;
                            optionalRowCount = Math.Max(optionalRowCount, row);
                        }
                        variables.Values[row] = t.Item2;
                        variables.Uncertainty[row] = t.Item3 < Double.MaxValue ? t.Item3 : Double.NaN;
                        variables.Provenance[row] = (t.Item4 == 65535) ? noProvDataSource : id2name[t.Item4];
                        variables.RowCount = row;
                    }
                }
            }

            foreach (var v in var2var)
            {
                for (var j = v.Value.RowCount + 1; j < rowCount; j++)
                {
                    v.Value.Values[j] = Double.NaN;
                    v.Value.Uncertainty[j] = Double.NaN;
                    v.Value.Provenance[j] = null;
                }
            }

            if (latmin != null)
            {
                for (var j = optionalRowCount + 1; j < rowCount; j++)
                {
                    latmin[j] = Double.NaN;
                    latmax[j] = Double.NaN;
                    lonmin[j] = Double.NaN;
                    lonmax[j] = Double.NaN;
                }
            }

            for (var j = regionRowCount + 1; j < rowCount; j++)
                regions[j] = "";

            dst.Commit();
        }
    }
}