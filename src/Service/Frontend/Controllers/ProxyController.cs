using Frontend;
using Microsoft.Research.Science.Data;
using Microsoft.Research.Science.FetchClimate2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace JsonProxy.Controllers
{
    public class ProxyController : ApiController
    {
        public static readonly AutoRegistratingTraceSource traceSource = new AutoRegistratingTraceSource("FrontendProxy", SourceLevels.All);

        // GET http://.../JsonProxy/getSchema?uri=dmitrov-uri
        public SerializableDataSetSchema Get(string uri)
        {
            traceSource.TraceEvent(TraceEventType.Information, 1, string.Format("Request for JSON schema of dataset {0}", uri));

            SerializableDataSetSchema result = null;

            if (!String.IsNullOrEmpty(uri))
            {
                var ds = new AzureBlobDataSet(uri);
                result = ConvUtils.GetSerializableSchema(ds);
            }

            if (result == null)
                throw new HttpResponseException(HttpStatusCode.InternalServerError);

            return result;
        }

        // GET http://.../JsonProxy/data?uri=dmitrov-uri&variables=name1,name2,name3
        public Dictionary<string,Array> Get(string uri, string variables)
        {
            traceSource.TraceEvent(TraceEventType.Start, 2, string.Format("Request for JSON data for {0} of {1}", variables, uri));

            if (String.IsNullOrEmpty(uri))
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest) 
                { 
                    ReasonPhrase = "Dataset URI is empty or not specified"
                });
            if (String.IsNullOrEmpty(variables))
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    ReasonPhrase = "Variable name is empty or not specified"
                });
            string[] variableNames = variables.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            Dictionary<string, Array> result = new Dictionary<string, Array>();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            try
            {
                using (var ds = new AzureBlobDataSet(uri)) 
                    foreach(var name in variableNames)
                        if(!result.ContainsKey(name))
                            result.Add(name, ds[name].GetData());
                return result;

            }
            catch (Exception exc)
            {
                traceSource.TraceEvent(TraceEventType.Error,3,string.Format("Error getting variables {0} of dataset {1}: {2}", variables, uri, exc.Message));
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Dataset access error: " + exc.Message
                });
            }
            finally
            {
                sw.Stop();
                traceSource.TraceEvent(TraceEventType.Stop, 2, string.Format("Request for JSON data is completed in {0}", sw.Elapsed));
            }
        }
    }
}
