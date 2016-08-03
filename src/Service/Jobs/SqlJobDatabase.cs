using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Linq.Mapping;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Data.Linq;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.Research.Science.Jobs
{
    [Table(Name = "Job")]
    public class SqlJobRecord : IJobRecord
    {
        [Column(DbType = "Int NOT NULL IDENTITY", IsDbGenerated = true, IsPrimaryKey = true)]
        public int ID { get; set; }
        [Column(DbType = "Int NOT NULL")]
        public int ParentID { get; set; }
        [Column(DbType = "Int NOT NULL")]
        public int RootID { get; set; }
        [Column(DbType = "Int NOT NULL")]
        public int Priority { get; set; }
        [Column(DbType = "Int NOT NULL")]
        public int Status { get; set; }
        [Column(DbType = "Int NOT NULL")]
        public int NodeID { get; set; }
        [Column(DbType = "NVarChar(MAX) NOT NULL")]
        public string MethodName { get; set; }
        [Column(DbType = "NVarChar(MAX) NOT NULL")]
        public string TypeName { get; set; }
        [Column(DbType = "NVarChar(MAX)")]
        public string DataUri { get; set; }
        [Column(DbType = "NVarChar(MAX)")]
        public string RequestUri { get; set; }
        [Column(DbType = "DateTime NOT NULL")]
        public DateTime SubmitTime { get; set; }
        [Column(DbType = "DateTime NOT NULL")]
        public DateTime AccessTime { get; set; }
        [Column(DbType = "Int NOT NULL")]
        public int PartitionNo { get; set; }
        [Column(DbType = "Int NOT NULL")]
        public int PartitionCount { get; set; }

        public Func<IJobContext, Task> Action 
        { 
            get
            {
                var type = Type.GetType(TypeName);
                if(type == null)
                    throw new InvalidOperationException("Cannot get type " + TypeName);
                var method = type.GetMethod(MethodName, BindingFlags.Static|BindingFlags.Public);
                if(method == null)
                    throw new InvalidOperationException("Cannot find static method " + MethodName + " in type " + TypeName);
                return new Func<IJobContext, Task>(c => (Task)method.Invoke(null, new object[] { c }));
            }
        }
   }

    [Table(Name = "Node")]
    public class SqlNodeRecord : INodeRecord
    {
        [Column(DbType = "Int NOT NULL", IsPrimaryKey = true)]
        public int ID { get; set; }
        [Column(DbType = "Int NOT NULL")]
        public int MemoryUsage { get; set; }
        [Column(DbType = "Int NOT NULL")]
        public int ProcessorUsage { get; set; }
        [Column(DbType = "Int NOT NULL")]
        public int RegularJobsCount { get; set; }
        [Column(DbType = "Int NOT NULL")]
        public int PrivilegedJobsCount { get; set; }
        [Column(DbType = "Int NOT NULL")]
        public int SystemRegularJobsCount { get; set; }
        [Column(DbType = "Int NOT NULL")]
        public int SystemPrivilegedJobsCount { get; set; }
        [Column(DbType = "DateTime NOT NULL")]
        public DateTime LastAccessTime { get; set; }
    }

    public class SqlJobDatabase : IJobDatabase, IDisposable
    {
        protected readonly Subject<IJobRecord> jobStatusChanged = new Subject<IJobRecord>();
        protected readonly Subject<IJobRecord> jobStarted = new Subject<IJobRecord>();

        protected DataContext safeDataContext = null;
        protected DataContext unsafeDataContext = null;

        public SqlJobDatabase(string cstr)
        {
            safeDataContext = new DataContext(cstr);
            unsafeDataContext = new DataContext(cstr);
        }

        public IQueryable<IJobRecord> Jobs
        {
            get { return unsafeDataContext.GetTable<SqlJobRecord>(); }
        }

        public IQueryable<INodeRecord> Nodes
        {
            get { return unsafeDataContext.GetTable<SqlNodeRecord>(); }
        }

        public virtual void SetNodeStatus(int id, int processorUsage, int memoryUsage, int regCount, int privCount, int sysRegCount, int sysPrivCount)
        {
            lock (this)
            {
                Trace.WriteLine(String.Format("Node {0}: going to change node status, sum of jobs =  {1}", id, regCount + privCount + sysPrivCount + sysRegCount));

                var node =
                    (from n in safeDataContext.GetTable<SqlNodeRecord>()
                     where n.ID == id
                     select n).FirstOrDefault();
                if (node == null)
                    throw new ArgumentException("Wrong node ID", "id");

                node.ProcessorUsage = processorUsage;
                node.MemoryUsage = memoryUsage;
                node.RegularJobsCount = regCount;
                node.PrivilegedJobsCount = privCount;
                node.SystemRegularJobsCount = sysRegCount;
                node.SystemPrivilegedJobsCount = sysPrivCount;
                node.LastAccessTime = DateTime.Now;
                safeDataContext.SubmitChanges();

                Trace.WriteLine(String.Format("Node {0}: changed status", id));
            }
        }

        public virtual void SetJobStatus(int jobID, int status)
        {
            SqlJobRecord job = null;
            lock (this)
            {
                job = GetJobRecord(jobID);
                job.Status = status;
                safeDataContext.SubmitChanges();
            }
            jobStatusChanged.OnNext(job);
        }

        public virtual IJobRecord AddJob(int rootID, int parentID, int priority, Func<IJobContext, Task> action, string dataUri, string requestUri, int partitionNo = 0, int partitionCount = 1)
        {
            if(action.GetInvocationList().Count() != 1)
                throw new ArgumentException("No multicast delegates supported", "action");
            if(!action.Method.IsStatic)
                throw new ArgumentException("Action method must be static", "action");

            lock (this)
            {
                var job = new SqlJobRecord
                {
                    ParentID = parentID,
                    RootID = rootID,
                    Priority = priority,
                    TypeName = action.Method.DeclaringType.AssemblyQualifiedName,
                    MethodName = action.Method.Name,
                    DataUri = dataUri,
                    SubmitTime = DateTime.Now,
                    AccessTime = DateTime.Now,
                    NodeID = -1,
                    RequestUri = requestUri,
                    Status = JobStatus.WaitingToRun,
                    PartitionNo = partitionNo,
                    PartitionCount = partitionCount
                };

                safeDataContext.GetTable<SqlJobRecord>().InsertOnSubmit(job);
                safeDataContext.SubmitChanges();

                return job;
            }
        }

        public virtual  bool StartJob(int jobID, int nodeID)
        {
            lock (this)
            {
                var job = GetJobRecord(jobID);
                if (job.NodeID != -1)
                    return false; // Job is already assigned to another node
                job.NodeID = nodeID;
                try
                {
                    safeDataContext.SubmitChanges();
                    return true;
                }
                catch (ChangeConflictException)
                {
                    foreach (var conflict in safeDataContext.ChangeConflicts)
                    {
                        conflict.Resolve();
                    }
                    safeDataContext.SubmitChanges();
                    return false;
                }
            }
        }

        public virtual void DiscardJob(int jobID)
        {
            lock (this)
            {
                var job = GetJobRecord(jobID);
                safeDataContext.GetTable<SqlJobRecord>().DeleteOnSubmit(job);
                safeDataContext.SubmitChanges();
            }
        }

        private SqlJobRecord GetJobRecord(int jobID)
        {
            var job = (from j in safeDataContext.GetTable<SqlJobRecord>()
                       where j.ID == jobID
                       select j).FirstOrDefault();
            if (job == null)
                throw new ArgumentException("Wrong job jobID", "jobID");
            return job;
        }

        public IObservable<IJobRecord> JobStatusChanged
        {
            get { return jobStatusChanged; }
        }

        public IObservable<IJobRecord> JobStarted
        {
            get { return jobStarted; }
        }

        public DataContext DataContext
        {
            get { return unsafeDataContext; }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                safeDataContext.Dispose();
                unsafeDataContext.Dispose();
            }
        }

        ~SqlJobDatabase()
        {
            try
            {
                Dispose(false);
            }
            catch
            {
                GC.SuppressFinalize(this);
            }
        }
    }     
}
