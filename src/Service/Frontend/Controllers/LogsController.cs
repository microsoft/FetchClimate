using Microsoft.Research.Science.FetchClimate2.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;

namespace Frontend.Controllers
{
    public class LogsController : ApiController
    {
        private static string RemoveAccountKeys(string s)
        {
            const string accKey = "accountkey=";
            int start = s.IndexOf(accKey, StringComparison.InvariantCultureIgnoreCase);
            while (start >= 0)
            {
                start += accKey.Length;
                int end = s.IndexOf("==", start);
                if (end >= 0)
                    s = s.Remove(start, end - start + 2).Insert(start, "*****");
                else
                    s = s.Substring(0, start) + "*****";
                start = s.IndexOf(accKey, start, StringComparison.InvariantCultureIgnoreCase);
            }
            return s;
        }

        public HttpResponseMessage Get(string hash, int days,string format)
        {
            var response = Request.CreateResponse();
            bool isCsv = format.ToLower() == "csv";
            var contentType = isCsv? "text/plain":"text/html";
            response.Content = new PushStreamContent(
                async (outputStream, httpContent, transportContext) =>
                {
                    try
                    {
                        byte[] buffer;
                        DateTime now = DateTime.UtcNow;
                        FetchClimateLogs logs = new FetchClimateLogs(WADTableServiceContext.CreateFromRoleSettings());
                        if (!isCsv)
                        {
                            buffer = UTF8Encoding.UTF8.GetBytes(@"<html><head><title>Logs of the service</title></head>
                                <h2>You may set optional GET parameters as well:</h2>
                                <ul><li>days - For how many days the logs are extracted (e.g. days=7)</li>
                                <li>hash - Show only messages that have specified hash associated with (e.g. hash=d311e8ef0a357fad39ad167a24faa259ee30132a)</li>
                                <li>format - change the format. The only alternative now is CSV (e.g format=csv)</li></ul>
                                <h2>Logs of the service</h2>
                                <table border='0'>");
                            await outputStream.WriteAsync(buffer, 0, buffer.Length);
                        }
                        foreach(var li in logs.GetLogs(hash, days)) 
                        {
                            
                            if(isCsv)
                                buffer = UTF8Encoding.UTF8.GetBytes(RemoveAccountKeys(li.ToCSVLine()) + "\r\n");
                            else
                                buffer = UTF8Encoding.UTF8.GetBytes(RemoveAccountKeys(li.ToHtmlTableRow()));
                            await outputStream.WriteAsync(buffer, 0, buffer.Length);
                        }
                        if (!isCsv)
                        {
                            buffer = UTF8Encoding.UTF8.GetBytes("</table><hr/>End of logs</html>");
                            await outputStream.WriteAsync(buffer, 0, buffer.Length);
                        }
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
                }, contentType);
            return response;
        }
    }
}
