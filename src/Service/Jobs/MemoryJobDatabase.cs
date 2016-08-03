using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.Jobs
{
    class JobRecord : IJobRecord
    {
        public int ID { get; set; }
        public int ParentID { get; set; }
        public int RootID { get; set; }
        public int Priority { get; set; }
        public Func<IJobContext, Task> Action { get; set; }
        public int Status { get; set; }
        public int NodeID { get; set; }
        public string DataUri { get; set; }
        public string RequestUri { get; set; }
        public DateTime SubmitTime { get; set; }
        public DateTime AccessTime { get; set; }
        public int PartitionNo { get; set; }
        public int PartitionCount { get; set; }
    }

    class NodeRecord : INodeRecord
    {
        public int ID { get; set; }
        public int MemoryUsage { get; set; }
        public int ProcessorUsage { get; set; }
        public int RegularJobsCount { get; set; }
        public int PrivilegedJobsCount { get; set; }
        public int SystemRegularJobsCount { get; set; }
        public int SystemPrivilegedJobsCount { get; set; }
    }

    public class MemoryJobDatabase : IJobDatabase
    {
        private readonly List<JobRecord> jobs = new List<JobRecord>();
        private readonly List<NodeRecord> nodes = new List<NodeRecord>();

        private readonly Subject<IJobRecord> jobStatusChanged = new Subject<IJobRecord>();
        private readonly Subject<IJobRecord> jobStarted = new Subject<IJobRecord>();
        private readonly Subject<IJobRecord> jobDiscarded = new Subject<IJobRecord>();

        private int nextID = 0;

        public MemoryJobDatabase()
        {
            nodes.Add(new NodeRecord { ID = 0 });
        }

        public IQueryable<IJobRecord> Jobs
        {
            get { return jobs.AsQueryable(); }
        }

        public IQueryable<INodeRecord> Nodes
        {
            get { return nodes.AsQueryable(); }
        }

        public void SetNodeStatus(int id, int processorUsage, int memoryUsage, int regCount, int privCount, int sysRegCount, int sysPrivCount)
        {
            lock (this)
            {
                var node = nodes.FirstOrDefault(n => n.ID == id);
                if (node != null)
                {
                    node.ProcessorUsage = processorUsage;
                    node.MemoryUsage = memoryUsage;
                    node.RegularJobsCount = regCount;
                    node.PrivilegedJobsCount = privCount;
                    node.SystemRegularJobsCount = sysRegCount;
                    node.SystemPrivilegedJobsCount = sysPrivCount;
                }
            }
        }

        public void SetJobStatus(int id, int status)
        {
            IJobRecord changedJob = null;
            lock (this)
            {
                var job = jobs.FirstOrDefault(j => j.ID == id);
                if (job != null)
                {
                    if (job.Status != status)
                    {
                        job.Status = status;
                        changedJob = job;
                    }
                    job.AccessTime = DateTime.Now;
                }
            }
            if (changedJob != null)
                jobStatusChanged.OnNext(changedJob);
        }

        public IJobRecord AddJob(int rootID, int parentID, int priority, Func<IJobContext, Task> action, string dataUri, string requestUri, int partitionNo = 0, int partitionCount = 1)
        {
            lock (this)
            {
                var job = new JobRecord
                {
                    ID = nextID++,
                    ParentID = parentID,
                    RootID = rootID,
                    Priority = priority,
                    Action = action,
                    DataUri = dataUri,
                    SubmitTime = DateTime.Now,
                    AccessTime = DateTime.Now,
                    NodeID = -1,
                    RequestUri = requestUri,
                    Status = JobStatus.WaitingToRun,
                    PartitionNo = partitionNo,
                    PartitionCount = partitionCount
                };
                jobs.Add(job);
                return job;
            }
        }

        public bool StartJob(int jobID, int nodeID)
        {
            JobRecord job;
            lock (this)
            {
                job = jobs.FirstOrDefault(j => j.ID == jobID);
                if (job != null)
                {
                    if (job.Status != JobStatus.WaitingToRun)
                        return false;
                    if (job.NodeID != -1)
                        return false;
                    job.NodeID = 0;
                    job.Status = JobStatus.Running;
                    job.AccessTime = DateTime.Now;
                }
            }
            if (job != null)
            {
                jobStarted.OnNext(job);
                return true;
            }
            else
                return false;

        }

        public void DiscardJob(int jobID)
        {
            JobRecord job;
            lock (this)
            {
                job = jobs.FirstOrDefault(j => j.ID == jobID);
                if (job != null)
                {

                    jobs.Remove(job);
                }
            }
            if (job != null)
                jobDiscarded.OnNext(job);
        }

        public IObservable<IJobRecord> JobStatusChanged
        {
            get { return jobStatusChanged; }
        }

        public IObservable<IJobRecord> JobStarted
        {
            get { return jobStarted; }
        }

        public IObservable<IJobRecord> JobDiscarded
        {
            get { return jobDiscarded; }
        }
    }     
}
