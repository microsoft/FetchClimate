using Microsoft.Research.Science.FetchClimate2;
using Microsoft.Research.Science.FetchClimate2.Integrators.Temporal;
using Microsoft.Research.Science.FetchClimate2.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.DataHandlersTests.ValueAggregators
{
    [TestClass]
    public class MonthlyMeans
    {
        [TestCategory("Local")]
        [TestCategory("BVT")]
        [TestMethod]
        public void WholeYearRequestTest()
        {
            var intergrator = new TimeAxisAvgProcessing.MonthlyMeansOverYearsStepIntegratorFacade();
            ITimeSegment ts = new TimeSegment(1961,1990,1,365,0,24);
            var ips = intergrator.GetTempIPs(ts);
            Assert.IsTrue(ips.Weights.All(w => w==ips.Weights[0]));
        }
    }
}
