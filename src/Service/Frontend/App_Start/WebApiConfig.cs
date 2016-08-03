using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace Frontend
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // the order in which the routes are added to the route table is very important
            // the first route in the route table that matches the URI is selected

            config.Routes.MapHttpRoute(
                name: "Configuration",
                routeTemplate: "api/configuration/{timestamp}",
                defaults: new { controller = "Configuration", timestamp = RouteParameter.Optional }
            );

            config.Routes.MapHttpRoute(
                name: "UIConfiguration",
                routeTemplate: "api/uiconfiguration",
                defaults: new { controller = "UIConfiguration" }
            );

            config.Routes.MapHttpRoute(
                name: "Compute",
                routeTemplate: "api/compute",
                defaults: new { controller = "Compute" }
            );

            config.Routes.MapHttpRoute(
                name: "Status",
                routeTemplate: "api/status/{hash}",
                defaults: new { controller = "Status", hash = RouteParameter.Optional }
            );

            config.Routes.MapHttpRoute(
                name: "Schema",
                routeTemplate: "jsproxy/schema/{uri}",
                defaults: new { controller = "Proxy", uri = string.Empty }
            );

            config.Routes.MapHttpRoute(
                name: "Data",
                routeTemplate: "jsproxy/data/{uri}/{variables}",
                defaults: new { controller = "Proxy", uri = string.Empty, variables = string.Empty }
            );

            config.Routes.MapHttpRoute(
                name: "Logs",
                routeTemplate: "logs/{hash}/{days}/{format}",
                defaults: new { controller = "Logs", hash = string.Empty, days = 1,format="html" }
            );

            config.Routes.MapHttpRoute(
                name: "Merge",
                routeTemplate: "merge/{r}/{file}",
                defaults: new { controller = "Merge", r = string.Empty, file = "output.csv" }
            );

            config.Routes.MapHttpRoute(
                name: "Export",
                routeTemplate: "export/{g}/{p}/{file}",
                defaults: new { controller = "Export", g = string.Empty, p = string.Empty, file = "output.csv" }
            );

        }
    }
}
