using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Frontend.Controllers
{
    public class ConfigurationController : ApiController
    {

        // GET api/Configuration
        public Microsoft.Research.Science.FetchClimate2.Serializable.FetchConfiguration Get()
        {
            var configuration = WebApiApplication.GetFetchConfiguration(DateTime.MaxValue);
            var toSerizlize = new Microsoft.Research.Science.FetchClimate2.Serializable.FetchConfiguration(configuration);
            return toSerizlize;
        }

        // GET api/Configuration?timestamp=03-Nov-2012%2012:00:00
        public Microsoft.Research.Science.FetchClimate2.Serializable.FetchConfiguration Get(DateTime timestamp)
        {
            try
            {
                var configuration = WebApiApplication.GetFetchConfiguration(timestamp);
                var toSerizlize = new Microsoft.Research.Science.FetchClimate2.Serializable.FetchConfiguration(configuration);
                return toSerizlize;
            }
            catch (ArgumentException exc)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest) 
                { 
                    ReasonPhrase = exc.Message
                });
            }
        }
    }
}
