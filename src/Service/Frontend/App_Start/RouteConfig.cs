using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;

namespace Frontend
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "FetchClimateForm",
                url: "form",
                defaults: new { controller = "Form", action = "Form" });

            routes.MapRoute(
                name: "FetchClimateResults",
                url: "results",
                defaults: new { controller = "Form", action = "Results" });

            routes.MapRoute(
                name: "FetchClimateDataSources",
                url: "datasources",
                defaults: new { controller = "Form", action = "DataSources" });
        }
    }
}