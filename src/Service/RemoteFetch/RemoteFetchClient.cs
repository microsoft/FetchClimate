using Microsoft.Research.Science.Data;
using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Globalization;
using System.Web;

namespace Microsoft.Research.Science.FetchClimate2
{
    public class FetchClimateException : Exception
    {
        public FetchClimateException(string message)
            : base(message)
        {

        }
    }

    /// <summary>Fetches environmental data from remote service</summary>
    public class RemoteFetchClient : IFetchClient
    {
        private Uri serviceUri;

        public RemoteFetchClient(Uri serviceUri)
        {
            this.serviceUri = serviceUri;
        }

        public async Task<DataSet> FetchAsync(IFetchRequest request, Action<FetchStatus> progressReport=null)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = serviceUri;
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("plain/text"));
            client.Timeout = TimeSpan.FromMinutes(10);


            var response = await client.PostAsJsonAsync<Serializable.FetchRequest>("api/compute", new Serializable.FetchRequest(request));
            if(!response.IsSuccessStatusCode)
                throw new Exception(String.Format("Error posting request to server: {0}", response.ReasonPhrase));
            while (true)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                responseString = responseString.Trim('"');
                int delimiterIndex = responseString.IndexOf('=');
                if (delimiterIndex == -1)
                    throw new FetchClimateException("Unexpected service response. Waiting for response in format key=value");
                string status = responseString.Substring(0, delimiterIndex);
                string content = responseString.Substring(delimiterIndex + 1);

                if (status == "pending" || status == "progress")
                {
                    int hashIdx = content.IndexOf("hash=");
                    if (hashIdx == -1)
                        throw new FetchClimateException("Hash part is not found in progress or pending status");
                    var result = content.Substring(0, hashIdx).TrimEnd(' ', ';','%'); 
                    content = content.Substring(hashIdx + 5);
                    if (progressReport != null)
                    {

                        int p = -1;
                        if (status == "pending")
                            progressReport(Int32.TryParse(result, out p) ? FetchStatus.Pending(p, content) : FetchStatus.Failed("Cannot parse position in queue"));
                        else
                            progressReport(Int32.TryParse(result, out p) ? FetchStatus.InProgress(p, content) : FetchStatus.Failed("Cannot parse completion percent"));
                    }
                    //waiting for the result
                    System.Threading.Thread.Sleep(TimeSpan.FromSeconds(10));
                }
                else if (status == "fault")
                {
                    if (progressReport != null)
                        progressReport(FetchStatus.Failed(content));
                    throw new FetchClimateException(content);
                }
                else if (status == "completed")
                {
                    if (progressReport != null)
                        progressReport(FetchStatus.Completed(content));
                    return new AzureBlobDataSet(content);
                }
                else
                    throw new FetchClimateException("Unexpected service response: " + responseString);

                response = await client.GetAsync("api/status?hash=" + content);
                if (!response.IsSuccessStatusCode)
                    throw new Exception(String.Format("Error getting status from the server: {0}", response.ReasonPhrase));
            }
        }        

        public IFetchConfiguration GetConfiguration(DateTime utcTime)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = serviceUri;
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json")); // Add an accept header for JSON format

            HttpResponseMessage response = null;
            FetchConfiguration config = null;

            // Act
            if (utcTime == DateTime.MaxValue)
                response = client.GetAsync("api/configuration").Result;  // Blocking call
            else
                //?timestamp=03-Nov-2012
                response = client.GetAsync(string.Format("api/configuration?timestamp={0}", HttpUtility.UrlEncode(utcTime.ToString("dd-MMM-yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture)))).Result;
            if (response.IsSuccessStatusCode)
            {
                // Parse the response body. Blocking.
                config = response.Content.ReadAsAsync<Microsoft.Research.Science.FetchClimate2.Serializable.FetchConfiguration>().Result.ConvertFromSerializable();
                config = new FetchConfiguration(config.TimeStamp,
                    config.DataSources.Select(ds => new DataSourceDefinition(ds.ID, ds.Name, ds.Description, ds.Copyright,
                        string.IsNullOrEmpty(ds.Location) ? serviceUri.ToString() : ds.Location,
                        ds.ProvidedVariables)).ToArray(),
                    config.EnvironmentalVariables);
                return config;
            }
            throw new InvalidOperationException(response.ReasonPhrase);
        }
    } 
}