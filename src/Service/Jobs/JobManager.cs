using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.Jobs
{
    internal class Job : IJob
    {
        private readonly IJobRecord record;
        private readonly IJobDatabase database;

        public Job(IJobRecord record, IJobDatabase database) : this(record, database, true) { }

        private Job(IJobRecord record, IJobDatabase database, bool needDispose)
        {
            this.record = record;
            this.database = database;
        }

        public IJobRecord Record
        {
            get { return record; }
        }

        public int ID
        {
            get { return Record.ID; }
        }

        public IJob Parent
        {
            get { return record.ParentID == -1 ? null : new Job(database.Jobs.First(j => j.ID == record.ParentID), database, false); }
        }

        public int Priority
        {
            get { return Record.Priority; }
        }

        public string Data
        {
            get { return Record.DataUri; }
        }

        public string Request
        {
            get { return Record.RequestUri; }
        }

        public virtual void Discard()
        {
            database.DiscardJob(record.ID);
        }
    }

    public class NodeJobManager
    {
        private IJobDatabase database;
        private IScheduler scheduler;
        private int nodeID;

        interface IBlockingRequest
        {
            bool IsComplete { get; }
            void Unblock();
        }

        interface IIORequest : IBlockingRequest
        {
            void Start(IObserver<IIORequest> requestCompleted);
            long DataSize { get; }
        }

        abstract class BlockingRequest<T> : IBlockingRequest
        {

            protected TaskCompletionSource<T> completion = new TaskCompletionSource<T>();

            public abstract bool IsComplete { get; }

            public abstract void Unblock();

            public Task<T> WaitTask
            {
                get { return completion.Task; }
            }
        }

        class WaitRequest : BlockingRequest<bool>
        {
            private List<Tuple<int, int>> criteria;
            private bool result = true;

            public WaitRequest(IEnumerable<Tuple<int, int>> c)
            {
                criteria = c.ToList();
            }

            public bool OnJobStatusChanged(IJobRecord record)
            {
                bool hasChanges = false;
                for (int i = 0; i < criteria.Count; )
                    if (criteria[i].Item1 == record.ID)
                    {
                        if (criteria[i].Item2 <= record.Status)
                        {
                            criteria.RemoveAt(i);
                            hasChanges = true;
                        }
                        else if (criteria[i].Item2 < 0) // Job failed
                        {
                            result = false;
                            criteria.RemoveAt(i);
                            hasChanges = true;
                        }
                        else
                            i++;
                    }
                    else
                        i++;
                return hasChanges;
            }

            public override bool IsComplete
            {
                get { return criteria.Count == 0; }
            }

            public override void Unblock()
            {
                completion.SetResult(result);
            }

            public void CheckStatus(IJobDatabase database)
            {
                for (int i = 0; i < criteria.Count; )
                    if (database.Jobs.Where(j => j.ID == criteria[i].Item1).Select(j => j.Status).First() >= criteria[i].Item2)
                        criteria.RemoveAt(i);
                    else
                        i++;
            }
        }

        class IORequest<T> : BlockingRequest<T>, IIORequest
        {
            private IDataRequest<T> request;
            private bool isCompleted;
            private T result;
            private Exception exception;

            public IORequest(IDataRequest<T> request)
            {
                this.request = request;
            }

            public override bool IsComplete
            {
                get { return isCompleted; }
            }

            public override void Unblock()
            {
                if (exception != null)
                    completion.SetException(exception);
                else
                    completion.SetResult(result);
            }

            public void Start(IObserver<IIORequest> requestCompleted)
            {
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        result = request.Perform();
                    }
                    catch (Exception exc)
                    {
                        exception = exc;
                    }
                    isCompleted = true;
                    requestCompleted.OnNext(this);
                }, TaskCreationOptions.LongRunning);
            }

            public long DataSize
            {
                get { return request.AmountInBytes; }
            }
        }

        enum NodeJobStatus
        {
            Running, Blocked, Ready
        }

        class NodeJob : Job, IJobContext
        {
            private NodeJobManager manager;
            private int status; // overrides Record.Status

            public List<WaitRequest> Waitings = new List<WaitRequest>();
            public List<IIORequest> IOs = new List<IIORequest>();
            public DateTime ReadyTime;
            public NodeJobStatus JobStatus;

            public NodeJob(IJobRecord record, NodeJobManager manager)
                : base(record, manager.database)
            {
                this.manager = manager;
            }

            public void OnJobStatusChanged(IJobRecord record)
            {
                if (JobStatus == NodeJobStatus.Blocked)
                {
                    bool hasChanges = false;
                    foreach (var w in Waitings)
                        hasChanges = hasChanges || w.OnJobStatusChanged(record);
                    if (Waitings.All(w => w.IsComplete) && IOs.All(i => i.IsComplete))
                    {
                        JobStatus = NodeJobStatus.Ready;
                        ReadyTime = DateTime.Now;
                    }
                }
            }

            public void OnIOCompleted(IIORequest request)
            {
                if (JobStatus == NodeJobStatus.Blocked)
                {
                    if (IOs.Contains(request))
                        if (Waitings.All(w => w.IsComplete) && IOs.All(i => i.IsComplete))
                        {
                            JobStatus = NodeJobStatus.Ready;
                            ReadyTime = DateTime.Now;
                        }
                }
            }

            public void Continue()
            {
                if (JobStatus == NodeJobStatus.Blocked)
                    throw new InvalidOperationException("Cannot continue blocked task");
                JobStatus = NodeJobStatus.Running;
                foreach (var w in Waitings.ToArray())
                    w.Unblock();
                Waitings.Clear();
                foreach (var i in IOs.ToArray())
                    i.Unblock();
                IOs.Clear();
            }

            public IJob Submit(Func<IJobContext, Task> action, string dataUri, string requestUri)
            {
                int p2 = (Record.Priority == JobPriority.Regular || Record.Priority == JobPriority.SystemRegular) ?
                    (JobPriority.SystemRegular) : (JobPriority.SystemPrivileged);
                var nj = new Job(manager.database.AddJob(Record.RootID, Record.ID, p2, action, dataUri, requestUri), manager.database);
                manager.scheduler.Schedule(manager.database);
                return nj;
            }

            public Task<bool> WaitAsync(params Tuple<IJob, int>[] states)
            {
                WaitRequest wr = new WaitRequest(states.Select(s => new Tuple<int, int>(s.Item1.ID, s.Item2)));
                manager.waitRequested.OnNext(new Tuple<NodeJob, WaitRequest>(this, wr));
                return wr.WaitTask;
            }

            public Task<T> GetDataAsync<T>(IDataRequest<T> request)
            {
                IORequest<T> ir = new IORequest<T>(request);
                manager.ioRequested.OnNext(new Tuple<NodeJob, IIORequest>(this, ir));
                return ir.WaitTask;
            }

            public int Status
            {
                get
                {
                    return status;
                }
                set
                {
                    if (status > 0 && value < status)
                        throw new InvalidOperationException("Cannot decrease node status");
                    manager.database.SetJobStatus(Record.ID, value);
                    status = value;
                }
            }

            public override void Discard()
            {
                throw new InvalidOperationException("Cannot discard currently executed job");
            }
        }

        private List<NodeJob> jobs = new List<NodeJob>();
        private Queue<IIORequest> ioq = new Queue<IIORequest>();
        private bool isIOActive = false;
        private List<WaitRequest> extWaitings = new List<WaitRequest>();

        private Subject<IJobRecord> statusChanged = new Subject<IJobRecord>();
        private Subject<IJobRecord> jobStarted = new Subject<IJobRecord>();
        private Subject<IIORequest> ioCompleted = new Subject<IIORequest>();
        private Subject<Tuple<NodeJob, WaitRequest>> waitRequested = new Subject<Tuple<NodeJob, WaitRequest>>();
        private Subject<Tuple<NodeJob, IIORequest>> ioRequested = new Subject<Tuple<NodeJob, IIORequest>>();
        private Subject<WaitRequest> externalWaitRequested = new Subject<WaitRequest>();
        private Subject<bool> scheduleRequested = new Subject<bool>();

        /// <summary>This scheduler is used to interleave all events occuring in the owner class</summary>
        protected readonly EventLoopScheduler interleaver = new EventLoopScheduler();

        public NodeJobManager(int nodeID, IJobDatabase database, IScheduler scheduler)
        {
            this.nodeID = nodeID;
            this.database = database;
            this.scheduler = scheduler;

            if(nodeID >= 0)
                database.JobStarted.ObserveOn(interleaver).Subscribe(jr => OnJobStarted(jr));
            database.JobStatusChanged.ObserveOn(interleaver).Subscribe(jr => OnJobStatusChanged(jr));

            jobStarted.ObserveOn(interleaver).Subscribe(r => OnJobStarted(r));
            statusChanged.ObserveOn(interleaver).Subscribe(r => OnJobStatusChanged(r));
            waitRequested.ObserveOn(interleaver).Subscribe(r => OnWaitRequested(r));
            ioRequested.ObserveOn(interleaver).Subscribe(r => OnIORequested(r));
            ioCompleted.ObserveOn(interleaver).Subscribe(r => OnIOCompleted(r));
            externalWaitRequested.ObserveOn(interleaver).Subscribe(r => OnExternalWaitRequested(r));
            if(nodeID >= 0)
                scheduleRequested.ObserveOn(interleaver).Subscribe(dummy => scheduler.Schedule(database));
        }

        public IJobDatabase Database
        {
            get { return database; }
        }

        public IJob Submit(int priority, Func<IJobContext, Task> action, string dataUri, string requestUri)
        {
            var job = new Job(database.AddJob(-1, -1, priority, action, dataUri, requestUri), database);
            scheduleRequested.OnNext(true);
            return job;
        }

        public Task<bool> WaitAsync(IJob job, int status)
        {
            var wr = new WaitRequest(new Tuple<int, int>[1] { new Tuple<int, int>(job.ID, status) });
            externalWaitRequested.OnNext(wr);
            return wr.WaitTask;
        }

        private void OnExternalWaitRequested(WaitRequest wr)
        {
            wr.CheckStatus(database);
            if (wr.IsComplete)
                wr.Unblock();
            else
                extWaitings.Add(wr);
        }

        private void OnJobStatusChanged(IJobRecord record)
        {
            // Unblock potential waitings
            foreach (var j in jobs)
                j.OnJobStatusChanged(record);

            // Remove records for finished or failed tasks
            var nj = jobs.FirstOrDefault(j => j.ID == record.ID);
            if (nj != null && (record.Status == JobStatus.RanToCompletion || record.Status <= JobStatus.Faulted))
                jobs.Remove(nj);

            // Unblock potential external waiters
            for (int i = 0; i < extWaitings.Count; )
            {
                extWaitings[i].OnJobStatusChanged(record);
                if (extWaitings[i].IsComplete)
                {
                    extWaitings[i].Unblock();
                    extWaitings.RemoveAt(i);
                }
                else
                    i++;
            }

            // Take more jobs if it is possible
            TrySchedule();
        }

        private void OnJobStarted(IJobRecord record)
        {
            var nj = new NodeJob(record, this);
            jobs.Add(nj);
            Task.Factory.StartNew(() =>
            {
                try
                {
                    nj.Record.Action(nj).ContinueWith(t =>
                    {
                        switch (t.Status)
                        {
                            case TaskStatus.Canceled:
                            case TaskStatus.Faulted:
                                nj.Status = -1;
                                break;
                            case TaskStatus.RanToCompletion:
                                nj.Status = Int32.MaxValue;
                                break;
                        }
                    });
                }
                catch
                {
                    nj.Status = -1;
                }
            }, TaskCreationOptions.LongRunning);
            UpdateNodeStatus();
        }

        private void OnIOCompleted(IIORequest request)
        {
            foreach (var j in jobs)
                j.OnIOCompleted(request);
            TrySchedule();
            isIOActive = false;
            TryIO();
        }

        private void OnWaitRequested(Tuple<NodeJob, WaitRequest> r)
        {
            r.Item2.CheckStatus(database);
            if (r.Item2.IsComplete)
            {
                r.Item2.Unblock();
                return;
            }
            var nj = r.Item1;
            nj.JobStatus = NodeJobStatus.Blocked;
            nj.Waitings.Add(r.Item2);
            TrySchedule();
        }

        private void OnIORequested(Tuple<NodeJob, IIORequest> r)
        {
            var nj = r.Item1;
            nj.JobStatus = NodeJobStatus.Blocked;
            nj.IOs.Add(r.Item2);
            ioq.Enqueue(r.Item2);
            TrySchedule();
            TryIO();
        }

        private void UpdateNodeStatus()
        {
            database.SetNodeStatus(
                nodeID,
                SystemInfo.GetProcessorUsage(),
                SystemInfo.GetMemoryUsage(),
                jobs.Where(j => j.Record.Priority == JobPriority.Regular).Count(),
                jobs.Where(j => j.Record.Priority == JobPriority.Privileged).Count(),
                jobs.Where(j => j.Record.Priority == JobPriority.SystemPrivileged).Count(),
                jobs.Where(j => j.Record.Priority == JobPriority.SystemRegular).Count());
        }

        private void TrySchedule()
        {
            while (jobs.Count(j => j.JobStatus == NodeJobStatus.Running) <= 2)
            {
                var nextJob = jobs.Where(j => j.JobStatus == NodeJobStatus.Ready).OrderBy(j => j.ReadyTime).FirstOrDefault();
                if (nextJob != null)
                    nextJob.Continue();
                else
                {
                    scheduler.Schedule(database);
                    break;
                }
            }
        }

        private void TryIO()
        {
            if (!isIOActive && ioq.Count > 0)
            {
                isIOActive = true;
                ioq.Dequeue().Start(ioCompleted);
            }
        }
    }
}