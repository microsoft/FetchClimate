using Microsoft.Research.Science.FetchClimate2;
using System;
using System.Data.Linq;
using System.Linq;

namespace FetchWorker
{
    class CleanUpJob : RunningJob
    {
        //TODO: run cleaning in the workingThread

        TimeSpan timeBeforeDeletion;
        JobManager localJobManager;

        public CleanUpJob(Job job, JobsDBDataContext jobsDataContext, JobManager jobManager, TimeSpan timeBeforeDeletion, JobSettings settings)
            : base(job, jobsDataContext, settings)
        {
            this.timeBeforeDeletion = timeBeforeDeletion;
            this.localJobManager = jobManager;
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        public override void Perform()
        {
            base.Perform();
            DateTime cleanUpTime = DateTime.UtcNow;
            DateTime oldEnough = cleanUpTime - timeBeforeDeletion;
            var toDel = jobsDataContext.Jobs.Where(j => j.PartNo == 0 && j.Touchtime <= oldEnough && j.Hash != JobManager.CleanUpJobHash).ToArray();
            int totalToDel = toDel.Length;
            try
            {
                for (int i = 0; i < totalToDel; ++i)
                {
                    try
                    {
                        localJobManager.DeleteJobBlob(toDel[i].Hash.Trim());
                    }
                    catch (Exception ex)
                    {
                        JobManager.JobManagerTrace.TraceError(this.ToShortString() + "Error occurred while deleting blob: {0}", ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                JobManager.JobManagerTrace.TraceError(this.ToShortString() + "Error occurred while accessing requests container: {0}", ex.ToString());
            }

            lock (jobsDataContext)
            {
                try
                {
                    jobsDataContext.Jobs.DeleteAllOnSubmit(jobsDataContext.Jobs.Where(j => j.Touchtime <= oldEnough));
                    jobsDataContext.SubmitChanges(ConflictMode.ContinueOnConflict);
                }
                catch (InvalidOperationException)
                { }
                catch (ChangeConflictException)
                {
                    foreach (var conflict in jobsDataContext.ChangeConflicts)
                        conflict.Resolve(RefreshMode.OverwriteCurrentValues);
                }
            }

            var logs = Microsoft.Research.Science.FetchClimate2.Diagnostics.WADTableServiceContext.CreateFromRoleSettings();
            try
            {
                logs.DeleteOldRecords(oldEnough);
            }
            catch (Exception ex)
            {
                JobManager.JobManagerTrace.TraceError(this.ToShortString() + "Error occurred while cleaning logs table: {0}", ex.ToString());
            }            
        }
    }
}
