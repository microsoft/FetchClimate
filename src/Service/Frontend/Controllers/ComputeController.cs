using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.Research.Science.FetchClimate2;

using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.Research.Science.FetchClimate2.Frontend;
using System.Threading;

namespace Frontend.Controllers
{
    /// <summary>Handler POST request to Compute endpoint</summary>
    public class ComputeController : ApiController
    {
        int minPtsPerPartition = FrontendSettings.Current.MinPtsPerPartition;
        int maxPtsPerPartition = FrontendSettings.Current.MaxPtsPerPartition;
        int waitingFastResultPeriodSec = FrontendSettings.Current.WaitingFastResultPeriodSec;
        int jobStatusCheckIntervalMilisec = FrontendSettings.Current.JobStatusCheckIntervalMilisec;
        int permitedTouchTimeTreshold = FrontendSettings.Current.JobTouchTimeTreshold;
        double jobRegistrationPermitedTime = FrontendSettings.Current.AllowedJobRegistrationSpan;

        public static readonly AutoRegistratingTraceSource FrontendTrace = new AutoRegistratingTraceSource("FrontendComputeCtrl");

        // GET api/Compute
        public string Get()
        {
            throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                ReasonPhrase = "Compute endpoint doesn't support GET requests"
            });
        }

        // POST api/Compute
        public string Post(Microsoft.Research.Science.FetchClimate2.Serializable.FetchRequest request)
        {
            try
            {                
                if (request.ReproducibilityTimestamp == new DateTime())
                    request.ReproducibilityTimestamp = DateTime.MaxValue;
                var exactTS = WebApiApplication.GetExactConfigurationTimestamp(request.ReproducibilityTimestamp);
                if (exactTS == DateTime.MinValue)
                {
                    FrontendTrace.TraceError("Cannot process request. Timestamp {0} is too early for current service configuration", request.ReproducibilityTimestamp);
                    return String.Format(Constants.FaultReply, "Timestamp is too early for this service");
                }
                else
                {
                    FrontendTrace.TraceVerbose("Request received: timestamp = {0}, exact timestamp = {1}",
                        request.ReproducibilityTimestamp, exactTS);
                    request.ReproducibilityTimestamp = exactTS;
                }

                var jobManager = WebApiApplication.GetSharedJobManager(Request);
                jobManager.MarkOutdatedAsFailed(permitedTouchTimeTreshold);

                var fetchRequest = request.ConvertFromSerializable();

                string errorMsg;
                if (!fetchRequest.Domain.IsContentValid(out errorMsg)) //checking request content
                    return string.Format(Constants.FaultReply, errorMsg);

                string hash = fetchRequest.GetSHAHash();
                FrontendTrace.TraceInfo("{0}: Hash is computed for request", hash);

                var jobStatus = jobManager.Submit(fetchRequest, hash, jobRegistrationPermitedTime, minPtsPerPartition, maxPtsPerPartition, RoleEnvironment.Roles["FetchWorker"].Instances.Count);
                if (jobStatus.State == JobOrPartState.Pending || jobStatus.State == JobOrPartState.InProgress)
                {
                    // Waiting for some time before response in case job manage to complete                                
                    FrontendTrace.TraceVerbose("{0}:Waiting for request completion", hash);
                    for (int i = 0; i * jobStatusCheckIntervalMilisec * 0.001 < waitingFastResultPeriodSec; ++i)
                    {
                        Thread.Sleep(jobStatusCheckIntervalMilisec);
                        jobStatus = jobManager.GetStatus(hash);
                        if ((jobStatus.State == JobOrPartState.Completed) || (jobStatus.State == JobOrPartState.Failed))
                        {
                            FrontendTrace.TraceVerbose("{0}:Request result is ready in less than {1} seconds, reporting {2} status", hash, waitingFastResultPeriodSec, jobStatus.ToString());
                            return jobStatus.ToString();
                        }
                    }
                    FrontendTrace.TraceVerbose("{0}:Request result is not ready in {1} seconds, reporting {2} status", hash, waitingFastResultPeriodSec, jobStatus.ToString());
                    return jobStatus.ToString();
                }
                else
                    return jobStatus.ToString();
            }
            catch (Exception exc)
            {
                FrontendTrace.TraceError("Request is processing error: {0}", exc.ToString());
                return JobStatus.GetFailedStatus(exc.ToString()).ToString();
            }
        }
    }
}
