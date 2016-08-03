using System;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.Jobs
{
    /// <summary>Defines generic request for external data</summary>
    /// <typeparam name="T">Return value</typeparam>
    public interface IDataRequest<T>
    {
        /// <summary>Performs the request</summary>
        /// <returns>Request result</returns>
        T Perform();
        /// <summary>Gets size of requested data in bytes. This is also a rough estimation of the returned object size</summary>
        long AmountInBytes { get; }
    }

    /// <summary>Provides permanent information about a job. Disposing this object removes job information from a table</summary>
    public interface IJob
    {
        /// <summary>Unique ID of the job</summary>
        int ID { get; }

        /// <summary>Parent job</summary>
        IJob Parent { get; }

        /// <summary>Gets data uri</summary>
        string Data { get; }

        /// <summary>Gets request uri</summary>
        string Request { get; }

        /// <summary>Gets priority of the job</summary>
        int Priority { get; }

        /// <summary>Removes job from jobs table. Does not delete job data.
        /// Can be done only for fully completed jobs (with all children fully completed)</summary>
        void Discard();
    }

    /// <summary>Provides information about current job and abilities to perform requests,
    /// launch new jobs and control job status</summary>
    public interface IJobContext : IJob 
    {
        /// <summary>Submits new job with data and request to process</summary>
        /// <returns>Descriptor of child job</returns>
        IJob Submit(Func<IJobContext, Task> action,string data, string request);        

        /// <summary>Waits until all jobs have status greater or equal to corresponding value</summary>
        /// <param name="states">Array of states to reach</param>
        /// <returns>Task that returns true if all jobs have reach required status. Task never returns False and fails if
        /// some of the child tasks failed</returns>
        Task<bool> WaitAsync(params Tuple<IJob, int>[] states);

        /// <summary>Queue async request for external data. All requests on single node are performent sequentially</summary>
        Task<T> GetDataAsync<T>(IDataRequest<T> request);

        /// <summary>Gets or sets job status. Value values are from 2 to Int32.MaxValue - 1. 
        /// Only increased sequence of value can be assigned.</summary>
        int Status { get; set; }
    }

    /// <summary>Base values for job status</summary>
    public static class JobStatus 
    {
        public const int WaitingToRun = 0;
        public const int Running = 1;
        public const int RanToCompletion = Int32.MaxValue;
        public const int Faulted = -1; // And less than Faulted
    }

    public static class JobPriority
    {
        public const int Regular = 3; // Lowest priority
        public const int Privileged = 2;
        public const int System = 1; // Jobs with priority <= System are mandatory to start
        public const int SystemRegular = 1;
        public const int SystemPrivileged = 0; // Highest priority
    }
}