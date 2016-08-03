using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Research.Science.Jobs;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Threading;

namespace Microsoft.Research.Science.Jobs
{
    /// <summary>Provides information about one job</summary>
    public interface IJobRecord
    {
        /// <summary>Unique job ID</summary>
        int ID { get; }

        /// <summary>Parent ID</summary>
        int ParentID { get; }

        /// <summary>ID of root job (job launched externally)</summary>
        int RootID { get; }


        /// <summary>Job priority. See <see cref="JobPriority"/> class for allowed values</summary>
        int Priority { get; }

        /// <summary>Delegate to static method</summary>
        Func<IJobContext, Task> Action { get; }

        /// <summary>Job status. See <see cref="JobStatus"/> class for explanations</summary>
        int Status { get; }

        /// <summary>ID of node this job is assigned to or -1 if job is not yet scheduled</summary>
        int NodeID { get; }

        /// <summary>URI to data to process</summary>
        string DataUri { get; }

        /// <summary>URI to processing instructions</summary>
        string RequestUri { get; }

        /// <summary>Time when this job was submitted</summary>
        DateTime SubmitTime { get; }

        /// <summary>Last time when this job was accessed</summary>
        DateTime AccessTime { get; }

        /// <summary>Index of partition to process</summary>
        int PartitionNo { get; }

        /// <summary>Total number of partitions</summary>
        int PartitionCount { get; }
    }

    /// <summary>Provides information about single computational node (Azure role)</summary>
    public interface INodeRecord
    {
        /// <summary>Unique node ID</summary>
        int ID { get; }

        /// <summary>Percent of memory usage. May be > 100% because of virtual memory</summary>
        int MemoryUsage { get; }

        /// <summary>Precent of processor usage</summary>
        int ProcessorUsage { get; }

        /// <summary>Number of regular jobs scheduled for this node</summary>
        int RegularJobsCount { get; }

        /// <summary>Number of privileged jobs scheduled for this node</summary>
        int PrivilegedJobsCount { get; }

        /// <summary>Number of jobs produced by regular jobs</summary>
        int SystemRegularJobsCount { get; }

        /// <summary>Number of jobs produced by system jobs</summary>
        int SystemPrivilegedJobsCount { get; }
    }
  
    public interface IJobDatabase
    {
        /// <summary>Gets all jobs in all nodes. May be wrapper to LINQ-to-SQL. Not thread safe.</summary>
        IQueryable<IJobRecord> Jobs { get; }
        /// <summary>Provides information about all nodes. Not thread safe.</summary>
        IQueryable<INodeRecord> Nodes { get; }

        /// <summary>Updates database with latest information about particular node. Thread safe.</summary>
        void SetNodeStatus(int id, int processorUsage, int memoryUsage, int regCount, int privCount, int sysRegCount, int sysPrivCount);

        /// <summary>Adds one record to a job database. This job is in WaitingToRun status and is not assigned to any node. Thread safe.</summary>
        IJobRecord AddJob(int rootID, int parentID, int priority, Func<IJobContext, Task> action, string dataUri, string requestUri, int partitionNo = 0, int partitionCount = 1);
        /// <summary>Transactionally marks given job as started on particular node. Notifies that node. Thread safe.</summary>       
        bool StartJob(int jobID, int nodeID);
        /// <summary>Updates job status and notifies all nodes about that. Thread safe.</summary>
        void SetJobStatus(int id, int status);
        /// <summary>Removes row from jobs table. Thread safe.</summary>
        void DiscardJob(int jobID);

        /// <summary>Provides events about status change of any job on any node (including local)</summary>
        IObservable<IJobRecord> JobStatusChanged { get; }

        /// <summary>Provides events about assigning job to a node</summary>
        IObservable<IJobRecord> JobStarted { get; }
    }   
}