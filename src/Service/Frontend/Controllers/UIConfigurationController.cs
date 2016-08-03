using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.Research.Science.FetchClimate2;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Frontend.Controllers
{
    public class UIConfigurationController : ApiController
    {
        // GET api/Configuration
        public UIConfiguration Get()
        {
            return new UIConfiguration
            {
                Boundaries = new Boundaries()
                {
                    Temporal = new TemporalBoundaries()
                    {
                        YearMax = FrontendSettings.Current.MaxYearBoundary,
                        YearMin = FrontendSettings.Current.MinYearBoundary
                    }
                }
            };
        }
    }

    public class UIConfiguration
    {
        public Boundaries Boundaries { get; set; }
    }

    public class Boundaries
    {
        public TemporalBoundaries Temporal { get; set; }
    }

    public class TemporalBoundaries
    {
        public int YearMin { get; set; }
        public int YearMax { get; set; }
    }
}
