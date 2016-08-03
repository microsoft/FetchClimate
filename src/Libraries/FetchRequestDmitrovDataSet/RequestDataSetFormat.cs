using Microsoft.Research.Science.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Research.Science.FetchClimate2
{
    /// <summary>Static class that provides methods for converting FetchRequest to Dmitrov data set and back</summary>
    public static class RequestDataSetFormat
    {
        public const string EnvironmentVariableNameKey = "environmentVariable";
        public const string TimeStampKey = "timeStamp";
        public const string DataSourceNameKey = "dataSource";

        public const string UncertaintyVariableName = "sd";
        public const string ProvenanceVariableName = "provenance";
        public const string ValuesVariableName = "values";
        public const string MaskVariableName = "mask";

        public const string HourAxisVariableName = "hours";
        public const string DayAxisVariableName = "days";
        public const string YearAxisVariableName = "years";

        public static string GetEnvironmentVariableName(this DataSet dataSet)
        {
            return dataSet.Metadata[EnvironmentVariableNameKey].ToString();
        }

        public static DateTime GetTimeStamp(this DataSet dataSet)
        {
            return (DateTime)dataSet.Metadata[TimeStampKey];
        }

        public static string GetDataSourceName(this DataSet dataSet)
        {
            return dataSet.Metadata.ContainsKey(DataSourceNameKey) ?
                dataSet.Metadata[DataSourceNameKey].ToString() : null;
        }

        /// <summary>Creates and fill dataset with completed request</summary>
        /// <param name="dsUri">Dmitrov URI to create dataset at</param>
        /// <param name="request">FetchClimate2 request</param>
        /// <param name="values">Array of computed values</param>
        /// <param name="provenance">Array of provenance IDs (may be null)</param>
        /// <param name="uncertainty">Array of uncertainties</param>
        /// <returns></returns>
        public static DataSet CreateCompletedRequestDataSet(string dsUri, IFetchRequest request, Array values, Array provenance, Array uncertainty)
        {
            var sd = GetRequestSchemaAndData(request);
            var ds = DataSet.Open(dsUri);
            if (sd.Item1.Metadata != null)
                foreach (var m in sd.Item1.Metadata)
                    ds.Metadata[m.Key] = m.Value.Value;
            foreach (var v in sd.Item1.Variables) {
                Array data;
                if (v.Name == RequestDataSetFormat.ValuesVariableName)
                    data = values;
                else if (v.Name == RequestDataSetFormat.ProvenanceVariableName)
                    data = provenance;
                else if (v.Name == RequestDataSetFormat.UncertaintyVariableName)
                    data = uncertainty;
                else
                    data = sd.Item2[v.Name];
                if(data != null)
                    ds.AddVariable(v.Type, v.Name, data, v.Dimensions);
            }	
            ds.Commit();
            return ds;
        }

        public static DataSet CreateRequestBlobDataSet(string dsUri, IFetchRequest request)
        {
            var sd = GetRequestSchemaAndData(request);
            var ds = AzureBlobDataSet.CreateSetWithSmallData(dsUri, sd.Item1, sd.Item2);
            return ds;
        }

        public static Tuple<SerializableDataSetSchema, IDictionary<string, Array>> GetRequestSchemaAndData(IFetchRequest request)
        {
            List<SerializableDimension> dims = new List<SerializableDimension>();
            dims.Add(new SerializableDimension("i", request.Domain.Lons.Length));
            if (request.Domain.SpatialRegionType == SpatialRegionSpecification.CellGrid ||
               request.Domain.SpatialRegionType == SpatialRegionSpecification.PointGrid)
                dims.Add(new SerializableDimension("j", request.Domain.Lats.Length));
            dims.Add(new SerializableDimension("t_y", request.Domain.TimeRegion.Years.Length));
            dims.Add(new SerializableDimension("t_d", request.Domain.TimeRegion.Days.Length));
            dims.Add(new SerializableDimension("t_h", request.Domain.TimeRegion.Hours.Length));
            if (request.Domain.SpatialRegionType == SpatialRegionSpecification.CellGrid) //data spatial dimensions
            {
                dims.Add(new SerializableDimension("ci", request.Domain.Lons.Length - 1));
                dims.Add(new SerializableDimension("cj", request.Domain.Lats.Length - 1));
            }

            bool isYearsDimReduced = request.Domain.TimeRegion.YearsAxisLength == 1;
            bool isDaysDimReduced = request.Domain.TimeRegion.DaysAxisLength == 1;
            bool isHoursDimReduced = request.Domain.TimeRegion.HoursAxisLength == 1;

            if (request.Domain.TimeRegion.IsIntervalsGridYears && !isYearsDimReduced) //data temporal dimensions
                dims.Add(new SerializableDimension("c_y", request.Domain.TimeRegion.YearsAxisLength));
            if (request.Domain.TimeRegion.IsIntervalsGridDays && !isDaysDimReduced)
                dims.Add(new SerializableDimension("c_d", request.Domain.TimeRegion.DaysAxisLength));
            if (request.Domain.TimeRegion.IsIntervalsGridDays && !isHoursDimReduced)
                dims.Add(new SerializableDimension("c_h", request.Domain.TimeRegion.HoursAxisLength));

            List<SerializableVariableSchema> vars = new List<SerializableVariableSchema>();
            Dictionary<string, Array> data = new Dictionary<string, Array>();

            var hoursMeta = new Dictionary<string, object>(); hoursMeta["IsIntervals"] = request.Domain.TimeRegion.IsIntervalsGridHours;
            vars.Add(new SerializableVariableSchema(HourAxisVariableName, typeof(int), new string[] { "t_h" }, hoursMeta));

            var daysMeta = new Dictionary<string, object>(); daysMeta["IsIntervals"] = request.Domain.TimeRegion.IsIntervalsGridDays;
            vars.Add(new SerializableVariableSchema(DayAxisVariableName, typeof(int), new string[] { "t_d" }, daysMeta));

            var yearsMeta = new Dictionary<string, object>(); yearsMeta["IsIntervals"] = request.Domain.TimeRegion.IsIntervalsGridYears;
            vars.Add(new SerializableVariableSchema(YearAxisVariableName, typeof(int), new string[] { "t_y" }, yearsMeta));

            data.Add("hours", request.Domain.TimeRegion.Hours);
            data.Add("days", request.Domain.TimeRegion.Days);
            data.Add("years", request.Domain.TimeRegion.Years);

            switch (request.Domain.SpatialRegionType)
            {
                case SpatialRegionSpecification.CellGrid:
                    vars.Add(new SerializableVariableSchema("lat", typeof(double), new string[] { "j" }, null));
                    vars.Add(new SerializableVariableSchema("lon", typeof(double), new string[] { "i" }, null));
                    break;
                case SpatialRegionSpecification.Cells:
                    vars.Add(new SerializableVariableSchema("latmin", typeof(double), new string[] { "i" }, null));
                    vars.Add(new SerializableVariableSchema("latmax", typeof(double), new string[] { "i" }, null));
                    vars.Add(new SerializableVariableSchema("lonmin", typeof(double), new string[] { "i" }, null));
                    vars.Add(new SerializableVariableSchema("lonmax", typeof(double), new string[] { "i" }, null));
                    break;
                case SpatialRegionSpecification.PointGrid:
                    vars.Add(new SerializableVariableSchema("lat", typeof(double), new string[] { "j" }, null));
                    vars.Add(new SerializableVariableSchema("lon", typeof(double), new string[] { "i" }, null));
                    break;
                case SpatialRegionSpecification.Points:
                    vars.Add(new SerializableVariableSchema("lat", typeof(double), new string[] { "i" }, null));
                    vars.Add(new SerializableVariableSchema("lon", typeof(double), new string[] { "i" }, null));
                    break;
            }

            if (request.Domain.SpatialRegionType == SpatialRegionSpecification.Cells)
            {
                data.Add("latmin", request.Domain.Lats);
                data.Add("latmax", request.Domain.Lats2);
                data.Add("lonmin", request.Domain.Lons);
                data.Add("lonmax", request.Domain.Lons2);
            }
            else
            {
                data.Add("lat", request.Domain.Lats);
                data.Add("lon", request.Domain.Lons);
            }

            string[] dimNames;
            int[] dimSizes;

            List<string> temporalDataDimsNames = new List<string>();
            if (!isYearsDimReduced)
                temporalDataDimsNames.Add(request.Domain.TimeRegion.IsIntervalsGridYears ? "c_y" : "t_y");
            if (!isDaysDimReduced)
                temporalDataDimsNames.Add(request.Domain.TimeRegion.IsIntervalsGridDays ? "c_d" : "t_d");
            if (!isHoursDimReduced)
                temporalDataDimsNames.Add(request.Domain.TimeRegion.IsIntervalsGridHours ? "c_h" : "t_h");

            List<int> temporalDataDimsLength = new List<int>();
            if (!isYearsDimReduced)
                temporalDataDimsLength.Add(request.Domain.TimeRegion.YearsAxisLength);
            if (!isDaysDimReduced)
                temporalDataDimsLength.Add(request.Domain.TimeRegion.DaysAxisLength);
            if (!isHoursDimReduced)
                temporalDataDimsLength.Add(request.Domain.TimeRegion.HoursAxisLength);

            if (request.Domain.SpatialRegionType == SpatialRegionSpecification.PointGrid)
            {
                dimNames = new string[] { "i", "j" };
                dimSizes = new int[] { dims[0].Length, dims[1].Length };
            }
            else if (request.Domain.SpatialRegionType == SpatialRegionSpecification.CellGrid)
            {
                dimNames = new string[] { "ci", "cj" };
                dimSizes = new int[] { dims[0].Length - 1, dims[1].Length - 1 };
            }
            else
            {
                dimNames = new string[] { "i" };
                dimSizes = new int[] { dims[0].Length };
            }
            //adding temporal dims
            dimNames = dimNames.Concat(temporalDataDimsNames).ToArray();
            dimSizes = dimSizes.Concat(temporalDataDimsLength).ToArray();

            vars.Add(new SerializableVariableSchema(ValuesVariableName, typeof(double), dimNames, null));
            //data.Add(ValuesVariableName, Array.CreateInstance(typeof(double), dimSizes));
            vars.Add(new SerializableVariableSchema(UncertaintyVariableName, typeof(double), dimNames, null));
            //data.Add(UncertaintyVariableName, Array.CreateInstance(typeof(double), dimSizes));

            if (request.ParticularDataSource == null || request.ParticularDataSource.Length > 1)
            {
                vars.Add(new SerializableVariableSchema(ProvenanceVariableName, typeof(ushort), dimNames, null));
                //data.Add(ProvenanceVariableName, Array.CreateInstance(typeof(ushort), dimSizes));
            }
            if (request.Domain.Mask != null)
            {
                vars.Add(new SerializableVariableSchema(MaskVariableName, typeof(bool), dimNames, null));
                //data.Add(MaskVariableName, Array.CreateInstance(typeof(bool), dimSizes));
            }

            Dictionary<string, object> metadata = new Dictionary<string, object>();
            metadata.Add(EnvironmentVariableNameKey, request.EnvironmentVariableName);
            if (request.ReproducibilityTimestamp != new DateTime())
            {
                metadata.Add(TimeStampKey, request.ReproducibilityTimestamp);
                if (request.ParticularDataSource != null)
                    metadata.Add(DataSourceNameKey, request.ParticularDataSource);
            }

            return new Tuple<SerializableDataSetSchema, IDictionary<string, Array>>(
                new SerializableDataSetSchema(dims.ToArray(), vars.ToArray(), metadata), data);
        }

        public static IFetchRequest ToFetchRequest(this DataSet dataSet)
        {
            Serializable.TimeRegion serializableRegion = new Serializable.TimeRegion();
            serializableRegion.Years = (int[])dataSet[YearAxisVariableName].GetData();
            serializableRegion.Days = (int[])dataSet[DayAxisVariableName].GetData();
            serializableRegion.Hours = (int[])dataSet[HourAxisVariableName].GetData();
            serializableRegion.IsIntervalsGridYears = (bool)dataSet[YearAxisVariableName].Metadata["IsIntervals"];
            serializableRegion.IsIntervalsGridDays = (bool)dataSet[DayAxisVariableName].Metadata["IsIntervals"];
            serializableRegion.IsIntervalsGridHours = (bool)dataSet[HourAxisVariableName].Metadata["IsIntervals"];
            TimeRegion region = serializableRegion.ConvertFromSerializable();

            IFetchDomain domain = null;
            double[] lat, lon, latmax = null, lonmax = null;
            if (dataSet.Variables.Contains("latmin"))
            {
                lat = (double[])dataSet["latmin"].GetData();
                lon = (double[])dataSet["lonmin"].GetData();
                latmax = (double[])dataSet["latmax"].GetData();
                lonmax = (double[])dataSet["lonmax"].GetData();
            }
            else
            {
                lat = (double[])dataSet["lat"].GetData();
                lon = (double[])dataSet["lon"].GetData();
            }

            if (dataSet.Variables.Contains(MaskVariableName))
            {
                Array mask = dataSet[MaskVariableName].GetData();
                if (latmax == null)
                {
                    if (dataSet.Dimensions.Contains("ci"))
                        domain = FetchDomain.CreateCellGrid(lat, lon, region, mask);
                    else if (dataSet.Dimensions.Contains("j"))
                        domain = FetchDomain.CreatePointGrid(lat, lon, region, mask);
                    else
                        domain = FetchDomain.CreatePoints(lat, lon, region, mask);
                }
                else
                    domain = FetchDomain.CreateCells(lat, lon, latmax, lonmax, region, (bool[,])mask);
            }
            else
            {
                if (latmax == null)
                {
                    if (dataSet.Dimensions.Contains("ci"))
                        domain = FetchDomain.CreateCellGrid(lat, lon, region);
                    else if (dataSet.Dimensions.Contains("j"))
                        domain = FetchDomain.CreatePointGrid(lat, lon, region);
                    else
                        domain = FetchDomain.CreatePoints(lat, lon, region);
                }
                else
                    domain = FetchDomain.CreateCells(lat, lon, latmax, lonmax, region);
            }

            string envVar = (string)dataSet.Metadata[EnvironmentVariableNameKey];
            string[] dataSourceNames = dataSet.Metadata.ContainsKey(DataSourceNameKey) ? (string[])dataSet.Metadata[DataSourceNameKey] : null;

            if (dataSet.Metadata.ContainsKey(TimeStampKey))
                return new FetchRequest(
                    envVar,
                    domain,
                    (DateTime)dataSet.Metadata[TimeStampKey],
                    dataSourceNames);
            else
                return new FetchRequest(envVar, domain, dataSourceNames);
        }
    }
}
