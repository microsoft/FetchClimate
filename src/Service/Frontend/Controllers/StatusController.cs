using Microsoft.Research.Science.FetchClimate2;
using System.Linq;
using System.Web.Http;

namespace Frontend.Controllers
{
    public class StatusController : ApiController
    {

        public static readonly AutoRegistratingTraceSource FrontendTrace = new AutoRegistratingTraceSource("FrontendStatusCtrl");

        // GET api/Status?hash=blobHash
        public string Get(string hash)
        {
            var jobManager = WebApiApplication.GetSharedJobManager(Request);
            var status = jobManager.GetStatus(hash).ToString();
            FrontendTrace.TraceVerbose("{0}: Reporting status {1}",hash, status);
            return status;
        }

        public SystemStatus Get()
        {
            JobsDBDataContext dataContext = new JobsDBDataContext(FrontendSettings.Current.JobsDatabaseConnectionString);

            var failedHashes = dataContext.Jobs.Where(j => j.Status == (byte)JobOrPartState.Failed).Select(j => j.Hash).Distinct().ToArray();
            var activeParts = dataContext.Jobs.Where(j => j.Status < (byte)JobOrPartState.Completed).ToArray().
                Where(j => !failedHashes.Contains(j.Hash)).ToArray();
            var activeHashes = activeParts.Select(p => p.Hash).Distinct().Count();
            int runningHashes = activeParts.Where(p => p.Status == (byte)JobOrPartState.InProgress).Select(p => p.Hash).Distinct().Count();

            return new SystemStatus
            {
                pendingRequests = activeHashes - runningHashes,
                activeRequests = runningHashes,
                pendingParts = activeParts.Count(j => j.Status == (byte)JobOrPartState.Pending),
                activeParts = activeParts.Count(j => j.Status == (byte)JobOrPartState.InProgress)
            };
        }
    }

    public class SystemStatus
    {
        public int pendingRequests { get; set; }
        public int activeRequests { get; set; }
        public int pendingParts { get; set;  }
        public int activeParts { get; set; }
    }
}
