using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Microsoft.Research.Science.FetchClimate2;

using mathIntegrators = Microsoft.Research.Science.FetchClimate2.Integrators;

namespace Microsoft.Research.Science.FetchClimate2.Tests
{
    [TestClass]
    public class AllStationsTests
    {
        [TestMethod]
        [TestCategory("Local")]
        [DeploymentItem("GHCNv2_part.nc")]
        public async Task TestGetObservations()
        {
            var storage = TestDataStorageFactory.GetStorage("msds:nc?file=GHCNv2_part.nc&openMode=readOnly");
            var axis = storage.GetDataAsync("time").Result;
            var integrator = new mathIntegrators.Temporal.StepFunctionDateTimeAxisIntegrator(axis);
            var provider = new ObservationProviders.AllStationsOP(storage, integrator, "lat", "lon");
            var res = await provider.GetObservationsAsync(storage, "temp", -9999, 0.0, 0.0, 0.0, 0.0, new TimeSegment(1870, 1870, 1, 365, 0, 24));
            Assert.AreEqual(313, res.Observations.Length); //exactly 313 station have non-mv timeseries (manually checked)
        }
    }
}
