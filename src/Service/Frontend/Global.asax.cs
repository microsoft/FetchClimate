using Frontend.MessageHandlers;
using Microsoft.Research.Science.Data;
using Microsoft.Research.Science.Data.Factory;
using Microsoft.Research.Science.Data.Memory;
using Microsoft.Research.Science.FetchClimate2;
using Microsoft.WindowsAzure.ServiceRuntime;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;

namespace Frontend
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class WebApiApplication : System.Web.HttpApplication
    {

        // The Application_Start and Application_End methods are special methods that do not represent HttpApplication events.
        // ASP.NET calls them once for the lifetime of the application domain, not for each HttpApplication instance.
        // see "ASP.NET Application Life Cycle Overview for IIS 5.0 and 6.0" https://msdn.microsoft.com/en-us/library/ms178473.aspx
        // The Global.asax file is used in Integrated mode in IIS 7.0 much as it is used in ASP.NET in IIS 6.0. 
        protected void Application_Start()
        {
            Trace.TraceInformation("Global.asax: starting the http application domain (Application_Start).");

            // The System.Diagnostics.Trace object gets listeners from Web.config/configuration/system.diagnostics/trace/listeners section.
            // The below code copies Trace listeners to all AutoRegistratingTraceSource trace sources.
            foreach (TraceListener item in Trace.Listeners)
                if (!(item is DefaultTraceListener)) // The default trace listener is always in any TraceSource.Listeners collection.
                {
                    Microsoft.Research.Science.FetchClimate2.AutoRegistratingTraceSource.RegisterTraceListener(item);
                    Trace.TraceInformation("TraceListener \"{0}\" registered for accepting data from all AutoRegistratingTraceSources", item);
                }

            // Initializing Scientific DataSet registration
            if (!DataSetFactory.ContainsProvider("memory"))
                DataSetFactory.Register(typeof(MemoryDataSet));
            if (!DataSetFactory.ContainsProvider("ab"))
                DataSetFactory.Register(typeof(AzureBlobDataSet));
            Trace.TraceInformation("Scientific DataSet providers: " + string.Join(" ", DataSetFactory.GetRegisteredProviders()));

            // Initialize JobsManager if necessary
            bool firstInstance = RoleEnvironment.CurrentRoleInstance.Id == RoleEnvironment.CurrentRoleInstance.Role.Instances[0].Id;
            // The first Frontend role instance initializes the database if necessary.
            Microsoft.Research.Science.FetchClimate2.JobManager.InitializeJobTable(FrontendSettings.Current.JobsDatabaseConnectionString, firstInstance);

            // Initialize ASP.NET WebAPI
            // enabled by Microsoft.AspNet.WebApi.Tracing package
            if (FrontendSettings.Current.EnableAspnetDiagnosticTrace)
            {
                var tw = new System.Web.Http.Tracing.SystemDiagnosticsTraceWriter();
                tw.MinimumLevel = System.Web.Http.Tracing.TraceLevel.Debug;
                tw.IsVerbose = true;
                GlobalConfiguration.Configuration.Services.Replace(typeof(System.Web.Http.Tracing.ITraceWriter), tw);
            }
            AreaRegistration.RegisterAllAreas();
            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            //BundleConfig.RegisterBundles(BundleTable.Bundles);

            // enable CORS
            GlobalConfiguration.Configuration.MessageHandlers.Add(new CorsMessageHandler());

            // configure formatters
            var formatters = GlobalConfiguration.Configuration.Formatters;
            //formatters.Remove(formatters.XmlFormatter);

            formatters.Add(new PlainTextFormatter());

            var jsonFormatter = GlobalConfiguration.Configuration.Formatters.OfType<JsonMediaTypeFormatter>().FirstOrDefault();
            if (jsonFormatter == null)
            {
                string msg = "No json media formatter is available due to some internal error";
                Trace.TraceError(msg);
                throw new Exception(msg);
            }
            jsonFormatter.SerializerSettings.Converters.Add(new JsonCompliantNumberConverter());
        }
        // One instance of the HttpApplication class is used to process many requests in its lifetime. 
        // However, it can process only one request at a time. Thus, member variables can be used to store per-request data.
        //
        // JobManager is a relatively expensive resource that can be shared between several consecutive requests.

        JobManager _sharedJobManager = null;
        /// <summary>
        /// Get a shared <see cref="JobManager"/> from a current application instance.
        /// </summary>
        /// <param name="httpContext">An <see cref="System.Web.HttpContextBase"/> object that references an HttpApplication.</param>
        /// <remarks>The method assumes it is being called in a context of an HTTP request.</remarks>
        public static JobManager GetSharedJobManager(System.Web.HttpContextBase httpContext)
        {
            var current = (WebApiApplication)httpContext.ApplicationInstance;
            if (null == current._sharedJobManager)
                Trace.TraceInformation("Initializing a shared JobManager instance");
            current._sharedJobManager = new JobManager(FrontendSettings.Current.JobsDatabaseConnectionString, FrontendSettings.Current.ResultBlobConnectionString);
            return current._sharedJobManager;
        }
        /// <summary>
        /// Get a shared <see cref="JobManager"/> from a current application instance.
        /// </summary>
        /// <param name="request">An <see cref="HttpRequestMessage"/> object that references an HttpApplication.</param>
        /// <returns>A shared application-wide instance of <see cref="JobManager"/>.</returns>
        public static JobManager GetSharedJobManager(HttpRequestMessage request)
        {
            const string HttpContextBaseKey = "MS_HttpContext";
            return GetSharedJobManager((System.Web.HttpContextBase)request.Properties[HttpContextBaseKey]);

        }
        public static IFetchConfiguration GetFetchConfiguration(DateTime utcTimestamp)
        {
            return new FetchConfigurationProvider(FrontendSettings.Current.ConfigurationDatabaseConnectionString).GetConfiguration(utcTimestamp);
        }

        public static DateTime GetExactConfigurationTimestamp(DateTime utcTimestamp)
        {
            return new SqlExtendedConfigurationProvider(FrontendSettings.Current.ConfigurationDatabaseConnectionString).GetExactTimestamp(utcTimestamp);
        }

        internal static ExtendedConfiguration GetExtendedFetchConfiguration(DateTime utcTimestamp)
        {
            return new SqlExtendedConfigurationProvider(FrontendSettings.Current.ConfigurationDatabaseConnectionString).GetConfiguration(utcTimestamp);
        }
    }
    /// <summary>
    /// NaN and Infinity regardless of sign are represented as the String null.
    /// </summary>
    /// <remarks>
    /// See ECMA-262 "ECMAScript® 2015 Language Specification", http://www.ecma-international.org/publications/files/ECMA-ST/Ecma-262.pdf, page 473, NOTE 4.
    /// </remarks>
    class JsonCompliantNumberConverter : JsonConverter
    {
        public override bool CanRead
        {
            get
            {
                return false;
            }
        }
        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var val = value as double? ?? (double?)(value as float?);
            if (val == null || Double.IsNaN((double)val) || Double.IsInfinity((double)val))
            {
                writer.WriteNull();
                return;
            }
            writer.WriteValue((double)val);
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(double) || objectType == typeof(float);
        }
    }

    public class PlainTextFormatter : MediaTypeFormatter
    {
        public PlainTextFormatter()
        {
            this.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/plain"));
        }

        public override bool CanWriteType(Type type)
        {
            return type == typeof(string);
        }

        public override bool CanReadType(Type type)
        {
            return type == typeof(string);
        }

        public async override Task WriteToStreamAsync(Type type, object value, Stream stream, HttpContent content, TransportContext transportContext)
        {
            StreamWriter writer = new StreamWriter(stream);
            await writer.WriteAsync(value.ToString());
            writer.Flush();
        }

        public override async Task<object> ReadFromStreamAsync(Type type, Stream stream, HttpContent content, IFormatterLogger formatterLogger)
        {
            StreamReader reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
    }

}