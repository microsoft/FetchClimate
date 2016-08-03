using System;
using System.Data.Linq;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Microsoft.Research.Science.FetchClimate2
{
    public struct JobSettings
    {
        public int TouchPeriodInSeconds { get; set; }
        public int LightJobExecutionPermitedTimeSec { get; set; }
        public int PermitedHeavyPartWorkers { get; set; }
    }

    public abstract class RunningJob : IDisposable
    {
        protected JobSettings settings;

        protected string Hash { get; set; }
        protected int partNo { get; set; }
        protected int partsCount { get; set; }

        //DataSet jobDataset;
        protected JobsDBDataContext jobsDataContext;
        protected Job job;
        //string blobConnectionString;

        ManualResetEvent stopTouch = new ManualResetEvent(false);
        Thread touchThread;
        protected Thread workingThread;

        public override string ToString()
        {
            return string.Format("Request {0} ({1} of {2})", Hash, partNo + 1, partsCount);
        }

        public string ToShortString()
        {
            return string.Format("{0}:{1}:{2}", Hash, partNo + 1, partsCount);
        }

        protected RunningJob(Job job, JobsDBDataContext context, JobSettings settings)
        {
            this.settings = settings;
            this.job = job;
            this.jobsDataContext = context;

            Hash = job.Hash;
            partNo = job.PartNo;
            partsCount = job.PartsCount;

            
        }

        public virtual void Perform() {
            touchThread = new Thread(TouchThreadFunc);
            touchThread.IsBackground = true;
            JobManager.JobManagerTrace.TraceVerbose(string.Format("{0}:{1}:{2}:starting touch thread", Hash, partNo + 1, partsCount));
            touchThread.Start(new Tuple<Job, JobsDBDataContext, ManualResetEvent>(job, jobsDataContext, stopTouch));
        }

        public void Complete(bool successful)
        {
            if (!successful)
                DeletePendingParts();
            byte effectiveStatus = (byte)(successful ? JobOrPartState.Completed : JobOrPartState.Failed);
            CompleteWithStatus(effectiveStatus);
        }

        public void Abandon()
        {
            JobManager.JobManagerTrace.TraceInfo(this.ToShortString() + ": Abandoning( setting job status as pending again due to stop)");
            CompleteWithStatus(0);
        }

        /// <summary>
        /// Deletes other parts of this job as failed, preventing the worker from taking them for calculation
        /// </summary>
        private void DeletePendingParts()
        {
            lock (jobsDataContext)
            {
                jobsDataContext.ExecuteCommand("DELETE FROM Job WHERE Hash='{0}' AND Status={1}",
                    Hash,
                    (int)JobOrPartState.Pending);
            }
        }

        private void CompleteWithStatus(byte status)
        {
            lock (jobsDataContext)
            {
                try
                {
                    //Trace.WriteLine(string.Format("setting job {0} status {1}",this,status));
                    jobsDataContext.Refresh(RefreshMode.KeepChanges, job);
                    job.Status = status;
                    jobsDataContext.SubmitChanges();
                }
                catch (ChangeConflictException)
                {
                    foreach (var item in jobsDataContext.ChangeConflicts)
                    {
                        if (item.IsDeleted)
                            JobManager.JobManagerTrace.TraceError(this.ToShortString() + ":The job was unexpectedly deleted");
                        foreach (var conflict in item.MemberConflicts)
                        {
                            JobManager.JobManagerTrace.TraceError(this.ToShortString() + ":{4} Conflict {3} original:{0} current:{1} database:{2}", conflict.OriginalValue, conflict.CurrentValue, conflict.DatabaseValue, conflict.Member.Name, this);
                        }
                    }
                    throw;
                }
                finally
                {
                    stopTouch.Set();
                }
            }            
        }

        public virtual void Dispose()
        {
            JobManager.JobManagerTrace.TraceVerbose(this.ToShortString() + ": Signaling touch thread to stop");
            stopTouch.Set();
            //if (!IsCleanUpJob) jobDataset.Dispose();
        }

        private void TouchThreadFunc(object state)
        {
            Job job = ((Tuple<Job, JobsDBDataContext, ManualResetEvent>)state).Item1;
            JobsDBDataContext jobsDataContext = ((Tuple<Job, JobsDBDataContext, ManualResetEvent>)state).Item2;
            ManualResetEvent stopEvent = ((Tuple<Job, JobsDBDataContext, ManualResetEvent>)state).Item3;
            var ramCounter = new System.Diagnostics.PerformanceCounter("Memory", "Available MBytes");
            long curWorkingSet, curGCTotal, curPrivate;
            long maxWorkingSet = 0;
            long maxGCTotal = 0;
            long maxPrivate = 0;
            double curRamAvailMb, minRamAvailMb = double.MaxValue;
            int fails = 0;
            JobManager.JobManagerTrace.TraceVerbose(this.ToShortString() + ":Touch thread running");
            bool jobDeleted = false;
            while (true)
            {
                Process currentProcess = Process.GetCurrentProcess();
                currentProcess.Refresh();
                curWorkingSet = Environment.WorkingSet;
                curGCTotal = GC.GetTotalMemory(false);
                curPrivate = currentProcess.PrivateMemorySize64;
                curRamAvailMb = ramCounter.NextValue();
                if (maxWorkingSet < curWorkingSet) maxWorkingSet = curWorkingSet;
                if (maxGCTotal < curGCTotal) maxGCTotal = curGCTotal;
                if (maxPrivate < curPrivate) maxPrivate = curPrivate;
                if (minRamAvailMb > curRamAvailMb) minRamAvailMb = curRamAvailMb;
                try
                {
                    bool cancelationCheckNeeded = false;
                    lock (jobsDataContext)
                    {
                        JobManager.JobManagerTrace.TraceVerbose(this.ToShortString() + ":{0} Touch thread tick", job);
                        bool touched = false;
                        for (int touchTries = 0; touchTries < 5; touchTries++)
                        {
                            try
                            {
                                jobsDataContext.Refresh(RefreshMode.KeepChanges, job);
                                job.Touchtime = DateTime.UtcNow;                                
                                jobsDataContext.SubmitChanges();
                                touched = true;
                                break;
                            }
                            catch (ChangeConflictException)
                            {
                                //TraceConflicts(jobsDataContext, "updating touch time");
                                JobManager.JobManagerTrace.TraceVerbose(this.ToShortString() + ":{0} Failed to update touchtime. attempt {1}", job, touchTries + 1);
                                jobsDataContext.ChangeConflicts.ResolveAll(RefreshMode.OverwriteCurrentValues);
                            }
                        }
                        if (touched)
                            JobManager.JobManagerTrace.TraceVerbose(this.ToShortString() + ":{0} Updated TouchTime", job);
                        else
                        {
                            JobManager.JobManagerTrace.TraceWarning(this.ToShortString() + ":{0} Failed to update TouchTime", job);
                        }
                        if (job.IsHeavyJob == 0)
                        {
                            var execTimespanSec = (DateTime.UtcNow - job.StartTime.Value).TotalSeconds;
                            if (execTimespanSec > settings.LightJobExecutionPermitedTimeSec)
                            {
                                JobManager.JobManagerTrace.TraceVerbose(this.ToShortString() + ":{0} Job considered to be HEAVY", job);
                                
                                for (int retries = 0; retries < 10; retries++)
                                {
                                    try
                                    {
                                        var allParts = jobsDataContext.Jobs.Where(j1 => j1.Hash == job.Hash).ToArray();
                                        foreach (var j in allParts)
                                            j.IsHeavyJob = 1;

                                        JobManager.JobManagerTrace.TraceVerbose(this.ToShortString() + ":{0} Committing HEAVY flag for all jobs part", job);
                                        jobsDataContext.Refresh(RefreshMode.KeepChanges, allParts);
                                        jobsDataContext.SubmitChanges(ConflictMode.ContinueOnConflict);
                                        break;
                                    }
                                    catch (ChangeConflictException)
                                    {
                                        JobManager.JobManagerTrace.TraceVerbose(this.ToShortString() + ":{0} Failed to set heavy flag for all parts. attempt {1}", job, retries + 1);
                                        //TraceConflicts(jobsDataContext, "heavy flag committing");
                                        jobsDataContext.ChangeConflicts.ResolveAll(RefreshMode.OverwriteCurrentValues);
                                    }
                                }
                                JobManager.JobManagerTrace.TraceVerbose(this.ToShortString() + ":{0} Successfully committed HEAVY flag for all jobs part", job);

                                cancelationCheckNeeded = true;
                            }
                            else
                            {
                                JobManager.JobManagerTrace.TraceVerbose(this.ToShortString() + ":{0} touched job part record. Job still light ({1} execution seconds of {2} permitted)", job, execTimespanSec, settings.LightJobExecutionPermitedTimeSec);
                            }
                        }
                        else
                        {
                            JobManager.JobManagerTrace.TraceVerbose(this.ToShortString() + ":{0} touched job part record. Job is already marked as heavy", job);
                            cancelationCheckNeeded = true;
                        }

                        if (cancelationCheckNeeded)
                        {
                            JobManager.JobManagerTrace.TraceVerbose(this.ToShortString() + ":{0} Checking for heavy job cancelation", job);
                            int otherRunningHeavyPartsCount = jobsDataContext.Jobs.Count(j2 => j2.StartTime < job.StartTime && j2.Status == 1 && j2.IsHeavyJob == 1);
                            if (otherRunningHeavyPartsCount >= settings.PermitedHeavyPartWorkers)
                            {
                                JobManager.JobManagerTrace.TraceInfo(this.ToShortString() + ":{0} Aborting working thread as there are {1} other active heavy parts ({2} total permitted)", job, otherRunningHeavyPartsCount, settings.PermitedHeavyPartWorkers);
                                if (workingThread != null)
                                    workingThread.Abort();
                            }
                            else
                                JobManager.JobManagerTrace.TraceVerbose(this.ToShortString() + ":{0} Continuing heavy part as there are {1} other active heavy parts ({2} total permitted)", job, otherRunningHeavyPartsCount, settings.PermitedHeavyPartWorkers);
                        }
                    }

                    if (stopEvent.WaitOne(TimeSpan.FromSeconds(settings.TouchPeriodInSeconds)))
                    {
                        JobManager.JobManagerTrace.TraceVerbose(this.ToShortString() + ":{0} detected touch thread stop request. stopping touch thread loop...", job);
                        break;
                    }
                }
                catch (InvalidOperationException exc)
                {
                    jobDeleted = true;
                    JobManager.JobManagerTrace.TraceWarning(this.ToShortString() + ":{0} InvalidOperationException. Is job part deleted? ({1})", job, exc.ToString());
                    break;
                }
                catch (ChangeConflictException)
                {
                    lock (jobsDataContext)
                    {
                        if (TraceConflicts(jobsDataContext, "last chance exc") || jobDeleted)
                            break;
                    }
                }
                catch (Exception exc)
                {
                    JobManager.JobManagerTrace.TraceWarning(this.ToShortString() + ":{0} Error updating touch time for job {1}", job, exc.ToString());
                    if (fails++ > 3)
                        break;
                }

                if (jobDeleted)
                    JobManager.JobManagerTrace.TraceWarning(this.ToShortString() + ":{0} record is deleted. stoping touch thread", job);

                //curPhys = currentProcess.WorkingSet64;
                //curPaged = currentProcess.PagedMemorySize64;
                //curVirtual = currentProcess.VirtualMemorySize64;
                //if (currentLogRecord.PeakPhysicalMemoryUsage < curPhys) currentLogRecord.PeakPhysicalMemoryUsage = curPhys;
                //if (currentLogRecord.PeakPagedMemoryUsage < curPaged) currentLogRecord.PeakPagedMemoryUsage = curPaged;
                //if (currentLogRecord.PeakVirtualMemoryUsage < curVirtual) currentLogRecord.PeakVirtualMemoryUsage = curVirtual;

            }
            JobManager.JobManagerTrace.TraceVerbose(this.ToShortString() + ":Touch thread ending. Peak mem stats: working set {0}Mb, GC.allocated {1}Mb, PrivateMem {2}Mb, min available Ram {3}Mb",
                ((double)maxWorkingSet) / (1024 * 1024), ((double)maxGCTotal) / (1024 * 1024), ((double)maxPrivate) / (1024 * 1024), minRamAvailMb);

            //currentLogRecord.WorkEnd = DateTime.UtcNow;
            //logManager.Insert(currentLogRecord);
        }

        private bool TraceConflicts(JobsDBDataContext context, string identification)
        {
            bool jobDeleted = false;
            foreach (var conflict in context.ChangeConflicts)
                if (conflict.IsDeleted)
                {
                    JobManager.JobManagerTrace.TraceWarning(this.ToShortString() + " {0} : job is deleted unexpectedly", identification);
                    jobDeleted = true;
                    break;
                }
                else
                {
                    foreach (var item in conflict.MemberConflicts)
                    {
                        JobManager.JobManagerTrace.TraceInfo(this.ToShortString() + " {7} : conflict member {3} origin:{4} current:{5} database:{6}", job.Hash, job.PartNo, job.PartsCount,
                            item.Member.Name, item.OriginalValue, item.CurrentValue, item.DatabaseValue, identification);
                    }
                    conflict.Resolve(RefreshMode.KeepChanges);
                    jobsDataContext.SubmitChanges(ConflictMode.FailOnFirstConflict);
                }
            return jobDeleted;

        }
    }
}