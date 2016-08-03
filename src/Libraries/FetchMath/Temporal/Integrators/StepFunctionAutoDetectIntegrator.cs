using Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Research.Science.FetchClimate2.TimeAxisAvgProcessing;

namespace Microsoft.Research.Science.FetchClimate2
{    
    public static class StepFunctionAutoDetectHelper
    {
        public enum AxisKind { Years, Days, Hours }
        public abstract class AxisDetectionResult { }
        public class AxisFound : AxisDetectionResult
        {
            public string AxisName { get; private set; }
            public AxisKind AxisKind { get; private set; }
            public DateTime BaseOffset { get; private set; }
            public AxisFound(string axisName, AxisKind axisKind, DateTime baseOffset)
            {
                this.AxisName = axisName;
                this.AxisKind = axisKind;
                this.BaseOffset = baseOffset;
            }
        }
        public class AxisNotFound : AxisDetectionResult { };


        private static readonly string[] dateFormats = new string[] { "yyyy-MM-dd HH:mm:ss", "yyyy-M-d H:m:s", "yyy-M-d H:m:s" };

        private static AutoRegistratingTraceSource traceSource = new AutoRegistratingTraceSource("StepFunctionAutoDetectHelper");

        public static AxisDetectionResult SmartDetectAxis(IStorageContext storage)
        {
            IDataStorageDefinition storageDefinition = storage.StorageDefinition;
            string varName = TimeAxisAutodetection.GetTimeVariableName(storageDefinition);
            if (string.IsNullOrEmpty(varName))
                throw new InvalidOperationException("Can't autodetect time axis variable");
            string timeUnits = storageDefinition.VariablesMetadata[varName].Where(pair => pair.Key.ToLowerInvariant() == "units").Select(p => p.Value).FirstOrDefault() as string;

            if (timeUnits == null)
                throw new InvalidOperationException(string.Format("Can't find units metadata entry for the time axis \"{0}\"", varName));

            string trimmed = timeUnits.Trim();
            string[] splitted = trimmed.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (splitted.Length < 4 || splitted[1].ToLowerInvariant() != "since")
                throw new InvalidOperationException("Automatic time axis detection failed to determine time axis semantics. Time axis units must be in format \"days|hours|years since YYYY-MM-DD HH:MM:SS\"");

            DateTime baseTime = new DateTime();
            string dateToParse = string.Format("{0} {1}", splitted[2], splitted[3]);
            if (dateToParse.Length > 19)
                dateToParse = dateToParse.Substring(0, 19);

            bool baseTimeParsed = false;
            foreach (var dateFormat in dateFormats)
            {
                baseTimeParsed = DateTime.TryParseExact(dateToParse, dateFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out baseTime);
                if (baseTimeParsed)
                {
                    traceSource.TraceEvent(TraceEventType.Information, 4, string.Format("base datetime \"{0}\" for axis variable \"{1}\" was successfuly parsed as {2}", dateToParse, varName, baseTime.ToString("u")));
                    break;
                }
                else
                {
                    traceSource.TraceEvent(TraceEventType.Information, 4, string.Format("can not parse base datetime \"{0}\" for axis variable \"{1}\" with format {2}", dateToParse, varName, dateFormat));
                }
            }

            if (baseTimeParsed)
            {
                switch (splitted[0].ToLowerInvariant())
                {
                    case "years":
                        traceSource.TraceEvent(TraceEventType.Information, 1, "Detected axis suitable for StepFunctionYearsIntegrator");
                        return new AxisFound(varName, AxisKind.Years, baseTime);
                    case "days":
                        traceSource.TraceEvent(TraceEventType.Information, 2, "Detected axis suitable for  StepFunctionDaysIntegrator");
                        return new AxisFound(varName, AxisKind.Days, baseTime);
                    case "hours":
                        traceSource.TraceEvent(TraceEventType.Information, 3, "Detected axis suitable for  StepFunctionHoursIntegrator");
                        return new AxisFound(varName, AxisKind.Hours, baseTime);
                    default:
                        traceSource.TraceEvent(TraceEventType.Error, 4, string.Format("the offset units in units metadata entry of \"{0}\" can't be parsed. It must be one of the following: years, days or hours", varName));
                        return new AxisNotFound();
                }
            }
            else
                traceSource.TraceEvent(TraceEventType.Error, 5, string.Format("reference datetime in units metadata entry of \"{0}\" can't be parsed. It must be in format \"{1}\", but it is \"{2}\"", varName, dateFormats[0], dateToParse));
            return new AxisNotFound();

        }

       
        public static ITimeAxisAvgProcessing ConstructAverager(AxisKind axisKind, Array axis, DateTime baseOffset)
        {
            IWeightProvider stepFunctionWP = new WeightProviders.StepFunctionInterpolation();
            IDataCoverageEvaluator coverageEvaluator = new DataCoverageEvaluators.ContinousMeansCoverageEvaluator();

            switch (axisKind)
            {
                case AxisKind.Years:
                    traceSource.TraceEvent(TraceEventType.Information, 1, "Constructing StepFunction Years Integrator");
                    return new TimeAxisAvgFacade(axis, new TimeAxisProjections.ContinousYears(baseOffset.Year), stepFunctionWP, coverageEvaluator);
                case AxisKind.Days:
                    traceSource.TraceEvent(TraceEventType.Information, 2, "Constructing StepFunction Days Integrator");
                    return new TimeAxisAvgFacade(axis, new TimeAxisProjections.ContinuousDays(baseOffset), stepFunctionWP, coverageEvaluator);
                case AxisKind.Hours:
                    traceSource.TraceEvent(TraceEventType.Information, 3, "Constructing StepFunction Hours Integrator");
                    return new TimeAxisAvgFacade(axis, new TimeAxisProjections.ContinuousHours(baseOffset), stepFunctionWP, coverageEvaluator);
                default:
                    throw new NotImplementedException("unexpected enum value");
            }
        }

        public static ITimeAxisStatisticsProcessing ConstructStatisticsCalculator(AxisKind axisKind, Array axis, DateTime baseOffset)
        {
            IDataMaskProvider stepDMP = new DataMaskProviders.StepFunctionDataMaskProvider();            
            IDataCoverageEvaluator coverageEvaluator = new DataCoverageEvaluators.ContinousMeansCoverageEvaluator();

            switch (axisKind)
            {
                case AxisKind.Years:
                    traceSource.TraceEvent(TraceEventType.Information, 1, "Constructing StepFunction Years Value Groupping facade");
                    return new TimeAxisStatisticsProcessing.TimeAxisValueGrouppingFacade(axis, new TimeAxisProjections.ContinousYears(baseOffset.Year), stepDMP, coverageEvaluator);
                case AxisKind.Days:
                    traceSource.TraceEvent(TraceEventType.Information, 2, "Constructing StepFunction Days Value Groupping facade");
                    return new TimeAxisStatisticsProcessing.TimeAxisValueGrouppingFacade(axis, new TimeAxisProjections.ContinuousDays(baseOffset), stepDMP, coverageEvaluator);                    
                case AxisKind.Hours:
                    traceSource.TraceEvent(TraceEventType.Information, 3, "Constructing StepFunction Hours Value Groupping facade");
                    return new TimeAxisStatisticsProcessing.TimeAxisValueGrouppingFacade(axis, new TimeAxisProjections.ContinuousHours(baseOffset), stepDMP, coverageEvaluator);                             
                default:
                    throw new NotImplementedException("unexpected enum value");
            }
        }
    }
}
