using Microsoft.Research.Science.Data;
using Microsoft.Research.Science.FetchClimate2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FetchWorker
{
    class ComputationJob : RunningJob
    {
        SqlExtendedConfigurationProvider configProvider;

        public int[] resultOrigin { get; internal set; }
        public IFetchRequest Request { get; internal set; }
        public bool IsCleanUpJob { get; internal set; }

        DataSet jobDataset;

        public ComputationJob(
                Job job, JobsDBDataContext jobsDataContext, // Reference to Jobs database contents including query
                SqlExtendedConfigurationProvider configProvider, // current FetchClimate configuration
                JobSettings settings, DataSetUri resultDataSetUri
                )
            : base(job, jobsDataContext, settings)
        {
            this.configProvider = configProvider;
            jobDataset = DataSet.Open(resultDataSetUri);
            var wholeRequest = jobDataset.ToFetchRequest();
            var subJob = JobManager.EvaluateSubrequestData(wholeRequest, job.PartsCount, job.PartNo);
            resultOrigin = subJob.Item2;
            Request = subJob.Item1;
        }

        public override void Dispose()
        {
            jobDataset.Dispose();
            base.Dispose();
        }
        /// <summary>
        /// Instantiates an Engine and uses the the instance to perform the request prepared in the constructor of the ComputationJob class.
        /// </summary>
        public override void Perform()
        {
            base.Perform();
            Process currentP = Process.GetCurrentProcess();

            currentP.Refresh();
            JobManager.JobManagerTrace.TraceVerbose(string.Format("{0}:Start of job mem stats: working set {1}Mb, GC.allocated {2}Mb, PrivateMem {3}Mb",
                this.ToShortString(),
                Environment.WorkingSet / 1024 / 1024,
                GC.GetTotalMemory(false) / 1024 / 1024,
                currentP.PrivateMemorySize64 / 1024 / 1024));

            var config = configProvider.GetConfiguration(this.Request.ReproducibilityTimestamp);
            JobManager.JobManagerTrace.TraceVerbose("{0}: FE type determined {1}. Loading FE assembly", this.ToShortString(), config.FetchEngineTypeName);
            var feType = Type.GetType(config.FetchEngineTypeName);
            if (feType == null)
                throw new InvalidOperationException("Cannot load fetch engine type " + feType);
            JobManager.JobManagerTrace.TraceVerbose("{0}: FE assembly loaded", this.ToShortString());
            var feConst = feType.GetConstructor(new Type[1] { typeof(IExtendedConfigurationProvider) });
            if (feConst == null)
                throw new InvalidOperationException("The FE constrictor with needed signature is not found. Are the currently running service assemblies and math assemblies from AzureGAC built with different Core assemblies?");
            JobManager.JobManagerTrace.TraceVerbose("{0}: FE assembly loaded", this.ToShortString());
            var fe = (IFetchEngine)feConst.Invoke(new object[1] { configProvider });

            JobManager.JobManagerTrace.TraceVerbose("{0}: FE instance constructed", this.ToShortString());

            IFetchResponseWithProvenance result = null;

            System.Threading.ManualResetEvent isDone = new System.Threading.ManualResetEvent(false);
            bool isWorkingThreadAborted = false;
            Exception executionException = null;

            //TODO: seems like spawning a thread is not necessary anymore -- check!
            workingThread = new System.Threading.Thread(new System.Threading.ThreadStart(() =>
                {
                    try
                    {
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        result = fe.PerformRequestAsync(this.Request).Result;
                        sw.Stop();

                        JobManager.JobManagerTrace.TraceVerbose("{0}: FE processed the request in {1}. Writing data to blob...", this.ToShortString(), sw.Elapsed);

                        if (result.Provenance != null)
                            this.PutProvenance(result.Provenance);
                        this.PutValues(result.Values);
                        this.PutUncertaties(result.Uncertainty);
                    }
                    catch (ThreadAbortException)
                    {
                        JobManager.JobManagerTrace.TraceInfo("{0}: Working thread is aborted (due to heavy part calculation cancelation?)", this.ToShortString());
                        isWorkingThreadAborted = true;
                    }
                    catch (Exception exc)
                    {
                        JobManager.JobManagerTrace.TraceError("{0}: Exception in working thread: {1}", this.ToShortString(), exc.ToString());
                        executionException = exc;
                    }
                    finally
                    {
                        isDone.Set();
                    }
                }
            ));
            workingThread.IsBackground = true;
            JobManager.JobManagerTrace.TraceInfo("{0}: Starting working thread", this.ToShortString());
            workingThread.Start();

            isDone.WaitOne();
            JobManager.JobManagerTrace.TraceInfo("{0}: Working thread signaled that it is finished. Joining it", this.ToShortString());
            workingThread.Join();
            JobManager.JobManagerTrace.TraceVerbose("{0}: Joined worker thread", this.ToShortString());

            if (isWorkingThreadAborted)
                this.Abandon();
            else
                this.Complete(executionException == null);

            JobManager.JobManagerTrace.TraceInfo("{0}: marked as {1}", this.ToShortString(), isWorkingThreadAborted ? "Pending" : "Complete");

            currentP.Refresh();
            JobManager.JobManagerTrace.TraceVerbose(string.Format("{0}:End of job mem stats: working set {1}Mb, GC.allocated {2}Mb, PrivateMem {3}Mb",
                this.ToShortString(),
                Environment.WorkingSet / 1024 / 1024,
                GC.GetTotalMemory(false) / 1024 / 1024,
                currentP.PrivateMemorySize64 / 1024 / 1024));
        }

        public void PutValues(Array data)
        {
            jobDataset[RequestDataSetFormat.ValuesVariableName].PutData(resultOrigin, data);
            jobDataset.Commit();
        }

        public void PutUncertaties(Array data)
        {
            jobDataset[RequestDataSetFormat.UncertaintyVariableName].PutData(resultOrigin, data);
            jobDataset.Commit();
        }

        public void PutProvenance(Array data)
        {
            if (jobDataset.Variables.Contains(RequestDataSetFormat.ProvenanceVariableName))
            {
                jobDataset[RequestDataSetFormat.ProvenanceVariableName].PutData(resultOrigin, data);
                jobDataset.Commit();
            }
        }
    }
}
