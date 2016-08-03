using Microsoft.Research.Science.Data;
using Microsoft.Research.Science.Data.CSV;
using Microsoft.Research.Science.FetchClimate2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Frontend.Controllers
{
    public class ExportController : ApiController
    {

        public async Task<HttpResponseMessage> Get(string g, string p, string file)
        {
            return await Task.Run(() => {
                IEnumerable<Tuple<string, string[]>> requests = null;

                // Parse grids
                if (!String.IsNullOrEmpty(g))
                    requests = g.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(grid =>
                    {
                        int commaIdx = grid.LastIndexOf(',');
                        if(commaIdx== -1)
                            throw new HttpResponseException(HttpStatusCode.BadRequest);
                        string name = Uri.UnescapeDataString(grid.Substring(0,commaIdx));
                        string hash = grid.Substring(commaIdx + 1);
                        return new Tuple<string, string[]>(hash, new string[] { name });
                    });

                // Parse points
                if (!String.IsNullOrEmpty(p))
                {
                    var pr = p.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(points =>
                    {
                        var parts = points.Split(',').ToArray();
                        if (parts.Length < 2)
                            throw new HttpResponseException(HttpStatusCode.BadRequest);
                        return new Tuple<string, string[]>(parts[0], parts.Skip(1).Select(n => Uri.UnescapeDataString(n)).ToArray());
                    });
                    requests = requests == null ? pr : requests.Concat(pr);
                }
                        
                // No points or grids specified
                if(requests == null)
                    throw new HttpResponseException(HttpStatusCode.BadRequest);

                var csv = MergeCSV(requests.ToArray());
                HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
                result.Content = new StringContent(csv);
                result.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
                result.Content.Headers.Add("Content-disposition", string.Format("attachment; filename={0}", String.IsNullOrEmpty(file) ? "result.csv" : Uri.EscapeDataString(file)));
                return result;
            });
        }

        private string MergeCSV(Tuple<string,string[]>[] requests)
        {
            string tempFile = Path.GetTempFileName();
            try
            {
                using (CsvDataSet result = new CsvDataSet(new CsvUri()
                {
                    FileName = tempFile,
                    Encoding = Encoding.UTF8,
                    OpenMode = ResourceOpenMode.Create,
                    Separator = Delimiter.Comma,
                    AppendMetadata = false,
                    NoHeader = false
                }.ToString()))
                {
                    result.IsAutocommitEnabled = false;
                    MergeCSV(result, requests);
                }
                return File.ReadAllText(tempFile);
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

        private void MergeCSV(DataSet dst, Tuple<string,string[]>[] requests)
        {
            var jobManager = WebApiApplication.GetSharedJobManager(Request);
            var dsr = requests.Select(r => 
                new Tuple<DataSet, string[]>(DataSet.Open(jobManager.ResultDataSetUri(r.Item1, false)), r.Item2)).ToArray();
            try
            {
                var config = WebApiApplication.GetFetchConfiguration(DateTime.MaxValue);
                TableExportHelper.MergeTable(config, dst, dsr);
                dst.Commit();
            }
            finally
            {
                foreach (var r in dsr)
                    r.Item1.Dispose();
            }
        }

    }
}
