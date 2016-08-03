using Microsoft.Research.Science.Data;
using Microsoft.Research.Science.Data.CSV;
using Microsoft.Research.Science.FetchClimate2;
using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace Frontend.Controllers
{
    public class MergeController : ApiController
    {

        public HttpResponseMessage Get(string r, string file)
        {
            var response = Request.CreateResponse();
            response.Content = new PushStreamContent(
                async (outputStream, httpContent, transportContext) =>
                {
                    try
                    {
                        await MergeCSV(outputStream, HttpUtility.UrlDecode(r).Split(','));
                    }
                    catch (HttpException ex)
                    {
                        if (ex.ErrorCode == -2147023667) // The remote host closed the connection. 
                        {
                            return;
                        }
                    }
                    finally
                    {
                        // Close output stream as we are done
                        outputStream.Close();
                    }
                }, "text/plain");
            response.Content.Headers.Add("Content-disposition", string.Format("attachment; filename={0}", Uri.EscapeDataString(file)));
            return response;
        }

        private async Task MergeCSV(Stream output, string[] hashes)
        {
            string tempFile = Path.GetTempFileName();
            try
            {
                using (CsvDataSet result = new CsvDataSet(new CsvUri()
                {
                    FileName = tempFile,
                    Encoding = Encoding.UTF8,
                    OpenMode = ResourceOpenMode.Create,
                    Separator = Delimiter.Comma
                }.ToString()))
                {
                    result.IsAutocommitEnabled = false;
                    MergeCSV(result, hashes);
                }
                const int BufferSize = 4096;
                byte[] buffer = new byte[BufferSize];
                using (var input = File.OpenRead(tempFile))
                {
                    int count = input.Read(buffer, 0, BufferSize);
                    while (count > 0)
                    {
                        await output.WriteAsync(buffer, 0, count);
                        count = input.Read(buffer, 0, BufferSize);
                    }
                }
            }
            finally
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch (Exception exc)
                {
                    WebRole.TraceInfo("Error deleting temporary file: " + exc.Message);
                }
            }
        }

        private void MergeCSV(DataSet dst, string[] hashes)
        {
            for (var i = 0; i < hashes.Length; i++)
            {
                using (var src = DataSet.Open(WebApiApplication.GetSharedJobManager(Request).ResultDataSetUri(hashes[i], false)))
                {
                    var name = src.Metadata[RequestDataSetFormat.EnvironmentVariableNameKey].ToString();
                    if (src.Metadata.ContainsKey(RequestDataSetFormat.DataSourceNameKey))
                    {
                        string[] dataSources = (string[])src.Metadata[RequestDataSetFormat.DataSourceNameKey];
                        var config = WebApiApplication.GetFetchConfiguration(DateTime.MaxValue);
                        dst.Metadata[name + "_dataSourceNames"] = dataSources;
                        dst.Metadata[name + "_dataSourceIDs"] = dataSources.Select(ds =>
                            config.DataSources.Where(dsd => dsd.Name == ds).First().ID).ToArray();
                    }

                    bool isPointSet = src.Variables[RequestDataSetFormat.ValuesVariableName].Dimensions.Count() == 1;
                    string visMethod = isPointSet ? "Points" : "Colormap";

                    if (i == 0)
                    {
                        if (src.Variables.Contains("lat"))
                            dst.AddVariableByValue(src.Variables["lat"]);
                        if (src.Variables.Contains("latmax"))
                            dst.AddVariableByValue(src.Variables["latmax"]);
                        if (src.Variables.Contains("latmin"))
                            dst.AddVariableByValue(src.Variables["latmin"]);

                        if (src.Variables.Contains("lon"))
                            dst.AddVariableByValue(src.Variables["lon"]);
                        if (src.Variables.Contains("lonmax"))
                            dst.AddVariableByValue(src.Variables["lonmax"]);
                        if (src.Variables.Contains("lonmin"))
                            dst.AddVariableByValue(src.Variables["lonmin"]);

                        dst.AddVariableByValue(src.Variables["hours"]);
                        dst.AddVariableByValue(src.Variables["days"]);
                        dst.AddVariableByValue(src.Variables["years"]);

                        dst.Metadata["VisualHints"] = name + "_" + RequestDataSetFormat.ValuesVariableName + "Style: " + visMethod;
                    }


                    var valuesVar = src[RequestDataSetFormat.ValuesVariableName];
                    dst.AddVariable<double>(name + "_" + RequestDataSetFormat.ValuesVariableName,
                        valuesVar.GetData(),
                        valuesVar.Dimensions.Select(d => d.Name).ToArray()).Metadata["VisualHints"] = "Style: " + visMethod;

                    var sdVar = src[RequestDataSetFormat.UncertaintyVariableName];
                    dst.AddVariable<double>(name + "_" + RequestDataSetFormat.UncertaintyVariableName,
                        sdVar.GetData(),
                        sdVar.Dimensions.Select(d => d.Name).ToArray());

                    if (src.Variables.Contains(RequestDataSetFormat.ProvenanceVariableName))
                    {
                        var provVar = src[RequestDataSetFormat.ProvenanceVariableName];
                        dst.AddVariable<ushort>(name + "_" + RequestDataSetFormat.ProvenanceVariableName,
                            provVar.GetData(),
                            provVar.Dimensions.Select(d => d.Name).ToArray());
                    }
                }
            }
            dst.Commit();
        }
    }
}
