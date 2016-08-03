using Microsoft.Research.Science.Data;
using Microsoft.Research.Science.Data.Imperative;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.Research.Science.FetchClimate2
{    
    public static class JobDataSetDefinition
    {
        public static void AssertJobDataSet(DataSet job)
        {
            string[] timeMetadata = new string[] { "FirstYear", "LastYear", "FirstDay", "LastDay", "StartHour", "StopHour" };
            foreach (var metaEntry in timeMetadata)
                if (!job.Metadata.ContainsKey(metaEntry))
                    throw new ArgumentException(string.Format("The dataset temporal region definition is incomplete. {0} global metadata value cant be found", metaEntry));


            if ((!job.Variables.Contains("Lat") || !job.Variables.Contains("Lon")) &&
                (!job.Variables.Contains("LatMin") || !job.Variables.Contains("LatMax") || !job.Variables.Contains("LonMin") || !job.Variables.Contains("LonMax")))
                throw new ArgumentException("The dataset spatial region definition schema doesn't satisfy any permited variants");
            if (job.Variables.Contains("Lat"))
            {
                Variable latVar = job.Variables["Lat"];
                Variable lonVar = job.Variables["Lon"];

                if (latVar.Dimensions.Count > 1)
                    throw new ArgumentException("Invalid job dataset schema. Lat varaible is not an axis.");
                if (lonVar.Dimensions.Count > 1)
                    throw new ArgumentException("Invalid job dataset schema. Lon varaible is not an axis.");
            }
            else
            {
                Variable latMinVar = job.Variables["LatMin"];
                Variable lonMinVar = job.Variables["LonMin"];
                Variable latMaxVar = job.Variables["LatMax"];
                Variable lonMaxVar = job.Variables["LonMax"];
                if (latMinVar.Dimensions.Count > 1)
                    throw new ArgumentException("Invalid job dataset schema. LatMin varaible is not an axis.");
                if (lonMinVar.Dimensions.Count > 1)
                    throw new ArgumentException("Invalid job dataset schema. LonMin varaible is not an axis.");
                if (latMaxVar.Dimensions.Count > 1)
                    throw new ArgumentException("Invalid job dataset schema. LatMax varaible is not an axis.");
                if (lonMaxVar.Dimensions.Count > 1)
                    throw new ArgumentException("Invalid job dataset schema. LonMax varaible is not an axis.");
                if (latMinVar.Dimensions[0].Name != latMaxVar.Dimensions[0].Name && latMinVar.Dimensions[0].Name != lonMinVar.Dimensions[0].Name && latMinVar.Dimensions[0].Name != lonMaxVar.Dimensions[0].Name)
                    throw new ArgumentException("Invalid job dataset schema. LatMin, LatMax, LonMin, LonMax must use the same dimension");
            }

        }

        #region time region specification
       
        public static TimeRegion BuildDescribedTimeRegion(DataSet job)
        {
            TimeSegment basicSegment = new TimeSegment()
            {
                FirstYear = (int)job.Metadata["FirstYear"],
                LastYear = (int)job.Metadata["LastYear"],
                FirstDay = (int)job.Metadata["FirstDay"],
                LastDay = (int)job.Metadata["LastDay"],
                StartHour = (int)job.Metadata["StartHour"],
                StopHour = (int)job.Metadata["StopHour"]
            };
            if (job.Variables.Contains("FirstYear") && job.Variables.Contains("LastYear"))
                return new TimeRegion(basicSegment, (int[])job.Variables["FirstYear"].GetData(), (int[])job.Variables["LastYear"].GetData(), TimeSeries.Yearly);
            if (job.Variables.Contains("FirstDay") && job.Variables.Contains("LastDay"))
                return new TimeRegion(basicSegment, (int[])job.Variables["FirstDay"].GetData(), (int[])job.Variables["LastDay"].GetData(), TimeSeries.Seasonly);
            if (job.Variables.Contains("StartHour") && job.Variables.Contains("StopHour"))
                return new TimeRegion(basicSegment, (int[])job.Variables["StartHour"].GetData(), (int[])job.Variables["StopHour"].GetData(), TimeSeries.Daily);
            return new TimeRegion(basicSegment);
        }

        #endregion

        public static SpatialRegionSpecification GetSpatialRegionSpecification(DataSet job)
        {
            if (job.Variables.Contains("Lat"))
            {
                Variable latVar = job.Variables["Lat"];
                Variable lonVar = job.Variables["Lon"];
                Dimension latDim = latVar.Dimensions[0];
                Dimension lonDim = lonVar.Dimensions[0];
                if (latDim.Name == lonDim.Name)
                    return SpatialRegionSpecification.Points;
                else
                {
                    if (latVar.Metadata.ContainsKey("Cells") || lonVar.Metadata.ContainsKey("Cells"))
                        return SpatialRegionSpecification.CellGrid;
                    else
                        return SpatialRegionSpecification.PointGrid;
                }
            }
            return SpatialRegionSpecification.Cells;
        }
    }
}

