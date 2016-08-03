using Microsoft.Research.Science.Data;
using Microsoft.Research.Science.FetchClimate2;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FetchClimate
{
    class Program
    {
        const string usage = "Usage: FetchClimate.exe [\"yyyy-mm-dd HH:mm\"]\nFetchClimate.exe /setServiceURI http://.../\nFetchClimate.exe /setLocalProcessing";

        static void Main(string[] args)
        {

            // Available command line options
            // /? /help or any incorrect command
            // /timestamp (/t) "yyyy-MM-dd hh:mm:ss:fff"
            // /show request-file
            // request-file output-dataset

            Console.WriteLine("FetchClient version {0}", Assembly.GetExecutingAssembly().GetName().Version);

            List<string> argList = new List<string>(args);
            if (argList.Count(a => a == "/?") > 0 || argList.Count(a => a == "/help") > 0)
            {
                PrintUsage();
                return;
            }

            // Lookup for "/t datetime" or for "/timestamp datetime" switch
            DateTime timestamp = DateTime.MaxValue;
            if (argList.Count(a => a == "/t" || a == "/timestamp") > 1)
            {
                PrintUsage();
                return;
            }
            int timeArgPos = argList.LastIndexOf("/t");
            if (timeArgPos < 0)
                timeArgPos = argList.LastIndexOf("/timestamp");
            if (timeArgPos >= 0)
            {
                if (timeArgPos + 1 >= argList.Count)
                {
                    PrintUsage();
                    return;                
                }
                if (!DateTime.TryParseExact(argList[timeArgPos + 1], "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out timestamp))
                    if (!DateTime.TryParseExact(argList[timeArgPos + 1], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out timestamp))
                        if (!DateTime.TryParseExact(argList[timeArgPos + 1], "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out timestamp))
                        {
                            PrintUsage();
                            return;
                        }
                argList.RemoveRange(timeArgPos, 2);
            }

            // Lookup for "/s" or "/show" switch
            int sCount = argList.Count(a => a == "/s" || a == "/show");
            if (sCount > 1)
            {
                PrintUsage();
                return;
            }
            bool isShowMode = false;
            int sIdx = argList.IndexOf("/s");
            if (sIdx < 0)
                sIdx = argList.IndexOf("/show");
            if (sIdx >= 0)
            {
                isShowMode = true;
                argList.RemoveAt(sIdx);
            }

            // Lookup for "/url service" or for "/u service" switch
            if (argList.Count(a => a == "/url" || a == "/u" || a == "/l" || a == "/local") > 1)
            {
                PrintUsage();
                return;
            }
            int urlArgPos = argList.LastIndexOf("/u");
            if (urlArgPos < 0)
                urlArgPos = argList.LastIndexOf("/url");
            if (urlArgPos >= 0)
            {
                if (urlArgPos + 1 >= argList.Count)
                {
                    PrintUsage();
                    return;
                }
                ClimateService.ServiceUrl = argList[urlArgPos + 1];
                argList.RemoveRange(urlArgPos, 2);
            }

            // Lookup for "/l" or "/local" switch
            int lIdx = argList.IndexOf("/l");
            if (lIdx < 0)
                lIdx = argList.IndexOf("/local");
            if (lIdx >= 0)
            {
                ClimateService.IsInProcessMode = true;
                argList.RemoveAt(lIdx);
            }

            // Executing commands
            try
            {
                if (argList.Count == 0)
                    PrintConfig(timestamp);
                else if (argList.Count == 1)
                {
                    if (!isShowMode)
                    {
                        PrintUsage();
                        return;
                    }
                    Fetch(argList[0], null, timestamp).View();
                }
                else if (argList.Count == 2)
                {
                    var result = Fetch(argList[0], argList[1], timestamp);
                    if (isShowMode)
                        result.View();
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine("Error: {0}", exc.Message);
            }
        }

        static private DataSet Fetch(string request, string result, DateTime timestamp)
        {
            var r = JsonConvert.DeserializeObject<Microsoft.Research.Science.FetchClimate2.Serializable.FetchRequest>(File.ReadAllText(request));
            if(timestamp != DateTime.MaxValue)
                r.ReproducibilityTimestamp = timestamp;
            var dataset = ClimateService.FetchAsync(r.ConvertFromSerializable(), s => Console.WriteLine(s)).Result;
            if (result != null)
                dataset = dataset.Clone(result);
            return dataset;
        }

        static private void PrintUsage()
        {
            Console.WriteLine("Usage: fetchclimate.exe [/t timestamp] [/u url | /l] [/s] [jsonfile] [dataset]");
            Console.WriteLine("Options:");
            Console.WriteLine("/t \"yyyy-MM-dd hh:mm[:ss.fff]\" - sets timestamp for FetchClimate request.");
            Console.WriteLine("/u url - sets service url.");
            Console.WriteLine("/l - sets in-process mode");
            Console.WriteLine("/s - shows result in DataSet Viewer");
            Console.WriteLine("jsonfile - specifies file with request in JSON format");
            Console.WriteLine("dataset - specifies Dmitrov dataset to write results");
        }

        static private void PrintConfig(DateTime utcTimestamp)
        {
            var fc = ClimateService.Instance;
            try 
            {
                string configHeading;
                if (ClimateService.IsInProcessMode)
                    configHeading="Configuration info for local in-process FetchClimate";
                else
                {
                    configHeading = string.Format("Configuration info for {0}",ClimateService.ServiceUrl);
                }
                var config = fc.GetConfiguration(utcTimestamp);
                PrettyPrint(config,configHeading);
            }
            catch(Exception exc)
            {
                Console.WriteLine("Error getting configuration: {0}", exc.Message);
            }
        }

        static private void PrettyPrint(IFetchConfiguration config, string heading)
        {
            ConsoleColor oldColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(heading);
            Console.WriteLine();
            Console.WriteLine("Config timestamp: {0}", config.TimeStamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            Console.WriteLine();

            if (config.EnvironmentalVariables.Length == 0)
            {
                Console.ForegroundColor = oldColor;
                Console.WriteLine("There are no environmental variables available");
            }
            else
            {
                int maxVarNameLength =
                    Math.Max(9, Math.Min(15, config.EnvironmentalVariables.Select(v => v.Name.Length).Max()));
                int maxUnitsLength =
                    Math.Max(6, Math.Min(15, config.EnvironmentalVariables.Select(v => v.Units.Length).Max()));
                int maxDescrLength =
                    Math.Max(12, Math.Min(47, config.EnvironmentalVariables.Select(v => v.Description.Length).Max()));

                Console.WriteLine("Environmental variables\n");
                Console.ForegroundColor = oldColor;

                Console.Write("Variable".PadRight(maxVarNameLength + 1));
                Console.Write("Units".PadRight(maxUnitsLength + 1));
                Console.WriteLine("Description".PadRight(maxDescrLength + 1));

                Console.Write("-".PadRight(maxVarNameLength, '-'));
                Console.Write(" -".PadRight(maxUnitsLength + 1, '-'));
                Console.WriteLine(" -".PadRight(maxDescrLength + 1, '-'));

                foreach (var v in config.EnvironmentalVariables)
                {
                    var len = v.Name.Length;
                    Console.Write(len > maxVarNameLength ?
                        String.Concat(v.Name.Substring(0, maxVarNameLength - 3), "... ") :
                        v.Name.PadRight(maxVarNameLength + 1));
                    len = v.Units.Length;
                    Console.Write(len > maxUnitsLength ?
                        String.Concat(v.Units.Substring(0, maxUnitsLength - 3), "... ") :
                        v.Units.PadRight(maxUnitsLength + 1));
                    len = v.Description.Length;
                    Console.WriteLine(len > maxDescrLength ?
                        String.Concat(v.Description.Substring(0, maxDescrLength - 3), "...") :
                        v.Description);
                }

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("\nData sources\n");
                Console.ForegroundColor = oldColor;

                foreach (var d in config.DataSources)
                {
                    Console.WriteLine("Name:\t\t{0}", d.Name);
                    Console.WriteLine("ID:\t\t{0}", d.ID);
                    Console.WriteLine("Description:\t{0}", d.Description);
                    Console.WriteLine("Copyright:\t{0}", d.Copyright);
                    Console.WriteLine("Variables:\t{0}", d.ProvidedVariables.Aggregate<string, StringBuilder>(new StringBuilder(), (sb, v) => sb.Append(string.Format("{0} ", v))).ToString());
                    Console.WriteLine("Location:\t{0}", d.Location);
                    Console.WriteLine();
                }
            }
        }
    }
}
