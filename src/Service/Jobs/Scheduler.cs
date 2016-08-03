using System;
using System.Linq;

namespace Microsoft.Research.Science.Jobs
{
    public interface IScheduler
    {
        void Schedule(IJobDatabase database);
    }

    public class SingleNodeScheduler : IScheduler
    {
        public void Schedule(IJobDatabase database)
        {
            var node = database.Nodes.First();
            // Schedule all new system jobs
            foreach (var jr in database.Jobs.Where(j => j.Priority >= JobPriority.System && j.Status == JobStatus.WaitingToRun).OrderBy(j => j.SubmitTime))
                database.StartJob(jr.ID, node.ID);

            int runningCount = database.Jobs.Count(j => j.Status >= JobStatus.Running || j.Status < Int32.MaxValue);
            if (runningCount < 2)
            {
                // Schedule some number of privileged jobs
                foreach (var jr in database.Jobs.Where(j => j.Priority == JobPriority.Privileged && j.Status == JobStatus.WaitingToRun).OrderBy(j => j.SubmitTime))
                {
                    database.StartJob(jr.ID, node.ID);
                    if (++runningCount > 2)
                        break;
                }
                if (runningCount < 2)
                {
                    int privilegedCount = database.Jobs.Count(j => j.Priority == JobPriority.Privileged || j.Priority == JobPriority.SystemPrivileged);
                    if (privilegedCount == 0)
                    {
                        // Schedule some number of privileged jobs
                        foreach (var jr in database.Jobs.Where(j => j.Priority == JobPriority.Regular && j.Status == JobStatus.WaitingToRun).OrderBy(j => j.SubmitTime))
                        {
                            database.StartJob(jr.ID, node.ID);
                            if (++runningCount > 2)
                                break;
                        }
                    }
                }
            }
        }
    }
}