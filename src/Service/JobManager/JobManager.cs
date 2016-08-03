
using System;
using System.Data.Linq;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Research.Science.Data;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.Research.Science.FetchClimate2
{
    public class JobManager
    {
        public static readonly AutoRegistratingTraceSource JobManagerTrace = new AutoRegistratingTraceSource("JobManager");

        JobsDBDataContext jobsDataContext;
        CloudBlobContainer resultsContainer;
        bool isDevelopmentStorage;
        string storageAccountKey;

        const int pageWidth = 512; //number, length of a non-last single-dimensional request part should be divisible by
        const string FetchRequestContainer = "requests";
        public const string CleanUpJobHash = @"$CLEANUP";

        string ResultBlobName(string resultId)
        {
            var c = jobsDataContext.Connection;
            var ds = c.DataSource;
            var col = ds.LastIndexOf(':');
            ds = ds.Substring(col + 1);
            return string.Concat(ds, "/", c.Database, "/", resultId);
        }
        public DataSetUri ResultDataSetUri(string resultId, bool canWrite)
        {
            var r = new AzureBlobDataSetUri() { Blob = ResultBlobName(resultId), Container=FetchRequestContainer };
            if (isDevelopmentStorage)
            {
                r.UseDevelopmentStorage = true;
            }
            else
            {
                r.AccountName = resultsContainer.ServiceClient.Credentials.AccountName;
                if (canWrite)
                {
                    r.AccountKey = storageAccountKey;
                    r.DefaultEndpointsProtocol = EndpointProtocol.https;
                }
                else r.OpenMode = ResourceOpenMode.ReadOnly;
            }
            return r;
        }

        /// <summary>Gets count of pending requests</summary>
        public int QueueLength
        {
            get
            {
                return (from j in jobsDataContext.Jobs where j.Status == (byte)JobOrPartState.Pending select j.Hash).Count();
            }
        }

        /// <summary>
        /// Gets the time of the last clean up job submission or DateTime.MinValue if no such jobs were ever submitted.
        /// </summary>
        public DateTime LastCleanUpTime
        {
            get
            {
                return jobsDataContext.Jobs.Where(j => j.Hash == CleanUpJobHash).Select(j => j.SubmitTime).ToArray().Aggregate(DateTime.MinValue, (acc, d) => acc > d ? acc : d);
            }
        }

        /// <summary>
        /// Creates the Jobs table in the database if it doesn't exist or waits until it is created
        /// </summary>
        /// <param name="sqlConnString">a DB to check</param>
        /// <param name="isInitiator">if true the table will be created, otherwise the method is blocked until the table is created by someone else</param>
        public static void InitializeJobTable(string sqlConnString, bool isInitiator)
        {
            var context = new JobsDBDataContext(sqlConnString);
            bool jobsSchemaExists = false;
            while (!jobsSchemaExists)
            {
                try
                {
                    JobManagerTrace.TraceInfo("Connected to jobs database. {0} job(s) are pending.",
                        (from j in context.Jobs where j.Status == (byte)JobOrPartState.Pending select j.Hash).Count());
                    jobsSchemaExists = true;
                }
                catch (SqlException)
                {
                    if (!isInitiator)
                    {
                        JobManagerTrace.TraceInfo("Jobs database doesn't contain expected schema. Waiting for the frontend (role index 0) to initialize the schema. Rechecking JobsDB in couple of seconds");
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                    }
                    else
                    {
                        JobManagerTrace.TraceInfo("Jobs database doesn't contain expected schema. Deploying the schema");

                        StringBuilder sqlText = new StringBuilder();
                        using (System.IO.StreamReader reader = new System.IO.StreamReader(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Microsoft.Research.Science.FetchClimate2.Jobs.sql")))
                        {
                            while (!reader.EndOfStream)
                            {
                                var str = reader.ReadLine();
                                if (!str.Contains("GO"))
                                    sqlText.AppendLine(str);
                            }
                        }
                        context.ExecuteCommand(sqlText.ToString());
                        JobManagerTrace.TraceInfo("Schema for JobsDB successfully deployed");
                    }
                }
            }
        }

        public JobManager(string sqlConnectionString)
            : this(sqlConnectionString, "UseDevelopmentStorage=true")
        { }

        public JobManager(string sqlConnectionString, string resultsStorageConnectionString)
        {

            jobsDataContext = new JobsDBDataContext(sqlConnectionString);

            isDevelopmentStorage = resultsStorageConnectionString.Contains("UseDevelopmentStorage=");
            if (!isDevelopmentStorage)
            {
                // extract a BASE64-encoded 64-byte account key from the connection string
                var m = System.Text.RegularExpressions.Regex.Match(
                    resultsStorageConnectionString,
                    "AccountKey=([A-Za-z0-9+/]{86}==)");
                if (!m.Success)
                {
                    var msg = "Cannot extract account key from the supplied storage connection string";
                    JobManagerTrace.TraceError(msg);
                    throw new Exception(msg);
                }
                storageAccountKey = m.Groups[1].Value;
            }
            CloudStorageAccount csa = CloudStorageAccount.Parse(resultsStorageConnectionString);
            var blobClient = csa.CreateCloudBlobClient();
            resultsContainer = blobClient.GetContainerReference(FetchRequestContainer);
            TryCreateRequestsContainer();
        }

        public JobStatus Submit(IFetchRequest request, string hash, double jobRegistrationPermitedTime, int minPointPerPartition, int maxPointPerPartition, int totalWorkers)
        {
            var jobStatus = GetStatus(hash, false);
            int retries = 0;

            while (true)
            {
                if (jobStatus == null)
                {
                    JobManagerTrace.TraceVerbose("{0}: Request is new (not in jobs table)", hash);

                    // Try to create blob                        
                    try
                    {
                        if (CreateBlobWithJob(request, hash, jobRegistrationPermitedTime)) // Successfully created
                        {
                            DateTime currentUTC = DateTime.UtcNow;

                            int partsCount = GetPartitionsCount(request, minPointPerPartition, maxPointPerPartition, GetFreeWorkersNo(totalWorkers));
                            var partsToInsert = Enumerable.Range(0, partsCount).Select(partNumber => new Job() { Hash = hash, PartNo = partNumber, PartsCount = partsCount, Status = (byte)JobOrPartState.Pending, Priority = 0, SubmitTime = currentUTC, Touchtime = currentUTC }).ToArray();
                            try
                            {
                                jobsDataContext.Jobs.InsertAllOnSubmit(partsToInsert);
                                jobsDataContext.SubmitChanges(System.Data.Linq.ConflictMode.FailOnFirstConflict);
                            }
                            catch (Exception ex)
                            {
                                JobManagerTrace.TraceError("{0}:Exception during insertion of job records into the job table. Reverting job registration (deleting job blob, all job records) {1}", hash, ex.ToString());
                                DiscardDataContextPendingChanges();
                                jobsDataContext.ExecuteCommand("delete from Job where hash='{0}'", hash); //order is important. deleting records before blob!
                                DeleteJobBlob(hash);
                                throw; //to be caught by retrying code
                            }
                            for (int i = 1; i <= partsCount; ++i)
                                JobManagerTrace.TraceVerbose("{0}:{1}:{2}:Record created in the jobs table", hash, i, partsCount);
                            return GetStatus(hash);
                        }
                        else // Blob exists        
                        {
                            jobStatus = JobStatus.GetPendingStatus(hash, Int32.MaxValue); // Unknown queue length
                            JobManagerTrace.TraceVerbose("{0}: Blob for request is created recently by someone else. Returning {1} status.", hash, jobStatus);
                            return jobStatus;
                        }
                    }
                    catch (Exception e)
                    {
                        JobManagerTrace.TraceError("{0}:Request submitting error: {1}", hash, e.ToString());
                        if (retries++ > 3)
                        {
                            JobManagerTrace.TraceError("{0}:3 retries failed for request. Reporting failed state to the client", hash);
                            return JobStatus.GetFailedStatus(e.ToString());
                        }
                    }
                }
                else
                {
                    JobManagerTrace.TraceVerbose("{0}: Request is already submitted. Current state is {1}", hash, jobStatus);
                    return jobStatus;
                }
            }
        }

        public void SubmitCleanUp()
        {
            DateTime currentUTC = DateTime.UtcNow;
            string hash = JobManager.CleanUpJobHash;

            int partsCount = 1;
            var cleanups = jobsDataContext.Jobs.Where(j => j.Hash == CleanUpJobHash).ToArray();
            lock (jobsDataContext)
            {
                if (cleanups.Length == 0)
                {
                    JobManagerTrace.TraceVerbose("{0}:{1}:{2}:Record created in the jobs table", hash, 1, partsCount);
                    jobsDataContext.Jobs.InsertOnSubmit(new Job() { Hash = hash, PartNo = 0, PartsCount = partsCount, Status = (byte)JobOrPartState.Pending, Priority = 0, SubmitTime = currentUTC, Touchtime = currentUTC });
                    jobsDataContext.SubmitChanges(System.Data.Linq.ConflictMode.FailOnFirstConflict);
                }
                else
                {
                    cleanups[0].Status = (byte)JobOrPartState.Pending;
                    cleanups[0].SubmitTime = currentUTC;
                    cleanups[0].Touchtime = currentUTC;
                    jobsDataContext.SubmitChanges(System.Data.Linq.ConflictMode.FailOnFirstConflict);
                }
            }
        }

        /// <summary>Marks all requests that are not updated for a specified time as failed</summary>
        /// <param name="permittedUntouchedIntervalSec">Maximum allowed idle time in seconds</param>
        public void MarkOutdatedAsFailed(int permittedUntouchedIntervalSec)
        {
            DateTime currentUTC = DateTime.UtcNow;
            var recyledParts = (from job in jobsDataContext.Jobs
                                where job.Status == (byte)JobOrPartState.InProgress && (currentUTC - job.Touchtime).TotalSeconds > permittedUntouchedIntervalSec
                                select job).ToArray();
            if (recyledParts.Length > 0)
            {
                lock (jobsDataContext)
                {
                    foreach (var j in recyledParts)
                    {
                        JobManagerTrace.TraceError("{0}:{1}:{2}:TouchTime hasn't been updated for too long. Marking it as failed", j.Hash, j.PartNo, j.PartsCount);
                        j.Status = (byte)JobOrPartState.Failed;
                    }
                    try
                    {
                        jobsDataContext.Refresh(RefreshMode.KeepChanges, recyledParts);
                        jobsDataContext.SubmitChanges(ConflictMode.ContinueOnConflict);
                    }
                    catch (ChangeConflictException)
                    {
                        lock (jobsDataContext)
                        {
                            jobsDataContext.ChangeConflicts.ResolveAll(RefreshMode.KeepChanges);
                            jobsDataContext.SubmitChanges(ConflictMode.FailOnFirstConflict);
                        }
                    }
                }
            }
        }

        public JobStatus GetStatus(string hash, bool failIfNotExists = true)
        {
            var res = (
                from jobPartsEntries in jobsDataContext.Jobs
                where jobPartsEntries.Hash == hash
                group jobPartsEntries by jobPartsEntries.Status into status
                select new { Status = status.Key, Count = status.Count() }
            ).ToArray();

            int pendingParts = res.Where(s => s.Status == (byte)JobOrPartState.Pending).Select(s => s.Count).FirstOrDefault();
            int activeParts = res.Where(s => s.Status == (byte)JobOrPartState.InProgress).Select(s => s.Count).FirstOrDefault();
            int completedParts = res.Where(s => s.Status == (byte)JobOrPartState.Completed).Select(s => s.Count).FirstOrDefault();
            int failedParts = res.Where(s => s.Status == (byte)JobOrPartState.Failed).Select(s => s.Count).FirstOrDefault();
            int totalParts = pendingParts + activeParts + completedParts + failedParts;

            if (pendingParts == 0 && activeParts == 0 && failedParts == 0)
            {
                if (completedParts == 0)
                    return failIfNotExists ? JobStatus.GetFailedStatus(String.Format("The job entry for hash is not found in the registry: {0}", hash)) : null;
                else  //all parts are complete
                {
                    TouchAllRecords(hash);
                    return JobStatus.GetCompletedStatus(ResultDataSetUri(hash, false).ToString());
                }
            }
            else
            {
                if (failedParts == 0)
                {
                    return activeParts > 0 ?
                        JobStatus.GetInProgressStatus(hash, totalParts > 0 ? (int)Math.Round(completedParts * 100.0 / totalParts) : 100) :
                        JobStatus.GetPendingStatus(hash, pendingParts);
                }
                else
                {
                    JobManagerTrace.TraceInfo("{0}:Reporting to the client that job has failed.", hash);
                    if ((from j in jobsDataContext.Jobs where j.Hash == hash && j.Status == (byte)JobOrPartState.InProgress select j).Count() == 0)
                    {
                        JobManagerTrace.TraceInfo("{0}:Deleting job records from the job table. As at least one of its parts is failed and no in progress parts are left", hash);
                        for (int i = 1; i <= totalParts; ++i) JobManagerTrace.TraceVerbose("{0}:{1}:{2}:Record is going to be deleted from the jobs table together with corresponding blob", hash, i, totalParts);
                        DeleteJobRecordsAndBlob(hash);

                    }
                    return JobStatus.GetFailedStatus(String.Format("Request failed. Please see logs for hash {0}", hash));
                }
            }
        }

        /// <summary>
        /// Blocking method. Returns null if asked to stop waiting
        /// </summary>
        /// <returns></returns>
        public RunningJob PeekLockJob(ManualResetEvent stopWaitingForMoreJobs, int queuePollingIntervalMilisec, int permitedLongTasksNum, Func<Job, JobsDBDataContext, RunningJob> createRunningJob)
        {
            while (true)
            {
                if (stopWaitingForMoreJobs.WaitOne(TimeSpan.FromSeconds(0)))
                    break;

                Job dequeued = null;
                int checkCounter = 0;
                while (true) //defueling conflict resolving loop
                {
                    Job pending = null;
                    try
                    {
                        lock (jobsDataContext)
                        {
                            pending = jobsDataContext.ExecuteQuery<Job>( //appending each part entry with corresponding running parts number
            @"SELECT TOP 1 * FROM Job j1 INNER JOIN
		(
		SELECT Hash,Count(InProgressStatus) RunningParts from Job j
		LEFT OUTER JOIN (Select 1 as InProgressStatus) AS pt ON j.Status=pt.InProgressStatus
			Where Hash In
				(SELECT DISTINCT Hash from Job WHERE Status=0 AND Hash NOT IN (SELECT DISTINCT Hash from Job WHERE Status=3))
			GROUP BY Hash
		) PendingJobs ON j1.Hash = PendingJobs.Hash
		WHERE j1.Status=0 AND ((select count(*) from Job where Status=1 and IsHeavyJob=1)<{0} OR j1.IsHeavyJob=0)
		ORDER BY PendingJobs.RunningParts,j1.SubmitTime", permitedLongTasksNum).FirstOrDefault();
                        }
                    }
                    catch (Exception exc)
                    {
                        JobManagerTrace.TraceError("Error accessing Job database: {0}", exc);
                    }
                    if (pending == null)
                    {
                        dequeued = null;
                        break;
                    }
                    try
                    {
                        lock (jobsDataContext)
                        {
                            JobManagerTrace.TraceVerbose(String.Format("{0}:{1}:{2}:Setting status to 1 (in progress)", pending.Hash, pending.PartNo + 1, pending.PartsCount));
                            pending.Status = (byte)JobOrPartState.InProgress;
                            DateTime now = DateTime.UtcNow;
                            pending.StartTime = now;
                            pending.Touchtime = now;
                            jobsDataContext.SubmitChanges();
                            JobManagerTrace.TraceInfo(string.Format("job {0}({1}) status set to 1", pending.Hash, pending.PartNo));
                        }
                        dequeued = pending;
                        break;
                    }
                    catch (ChangeConflictException)
                    {
                        lock (jobsDataContext)
                        {
                            foreach (var conflict in jobsDataContext.ChangeConflicts)
                                conflict.Resolve(RefreshMode.OverwriteCurrentValues);
                        }
                    }

                    if ((++checkCounter) % 5000 == 1)
                        JobManagerTrace.TraceVerbose("Performed {0} lookups to the Job queue. No suitable pending jobs", checkCounter);
                }
                if (dequeued != null)
                {
                    try
                    {
                        //var hash = dequeued.Hash.Trim();
                        //bool clean = hash == CleanUpJobHash;
                        //DataSet dataSet = clean ? null : DataSet.Open(string.Format("msds:ab?Container=requests&Blob={0}&{1}", hash, BlobConnectionString));
                        //return new RunningJob(dequeued, dataSet, jobsDataContext, clean ? BlobConnectionString : null);
                        return createRunningJob(dequeued, jobsDataContext);
                    }
                    catch (Exception exc)
                    {
                        lock (jobsDataContext)
                        {
                            JobManagerTrace.TraceError("{0}:{1}:{2}:Error opening dataset in blob - {3}\n doesn't have corresponding blob. marking as failed", dequeued.Hash, dequeued.PartNo + 1, dequeued.PartsCount, exc);
                            try
                            {
                                dequeued.Status = (byte)JobOrPartState.Failed;
                                jobsDataContext.SubmitChanges();
                            }
                            catch (ChangeConflictException)
                            {
                                foreach (var conflict in jobsDataContext.ChangeConflicts)
                                    conflict.Resolve(RefreshMode.OverwriteCurrentValues);
                            }
                        }
                    }
                }
                else
                {
                    var inProgressJobs = jobsDataContext.Jobs.Count(j => j.Status == (byte)JobOrPartState.InProgress); // One job -- one role instance
                    var totalInstances = Microsoft.WindowsAzure.ServiceRuntime.RoleEnvironment.CurrentRoleInstance.Role.Instances.Count;
                    var pollInterval = queuePollingIntervalMilisec * // reduce frequency when many workers are polling at the same time
                        Math.Max(1, totalInstances - inProgressJobs);
                    Thread.Sleep(pollInterval);
                }
            }
            return null;
        }

        private int GetFreeWorkersNo(int totalInstancesCount)
        {
            //var jobsInProcessRequest =
            //    from jobs in jobsDataContext.Jobs
            //    where jobs.Status == 1
            //    select jobs.Status;
            int jobsInProcess = this.jobsDataContext.Jobs.Count(x => x.Status == (byte)JobOrPartState.InProgress);
            //foreach (var i in jobsInProcessRequest) ++jobsInProcess;

            int totalWorkers = totalInstancesCount;

            return Math.Max(totalWorkers - jobsInProcess, 0);
        }


        public static int GetPartitionsCount(IFetchRequest request, int minPtsPerPartition, int maxPtsPerPartition, int freeWorkers = 0)
        {
            if (minPtsPerPartition < 1) minPtsPerPartition = 1;
            if (maxPtsPerPartition < 1) maxPtsPerPartition = 1;
            int[] shape = request.Domain.GetDataArrayShape();
            long totalPts = 1;
            foreach (var i in shape) totalPts *= i;

            int desiredPartsCount = (int)Math.Min(Math.Max((totalPts + maxPtsPerPartition - 1) / maxPtsPerPartition, Math.Min(freeWorkers, totalPts / minPtsPerPartition)), shape[0]);
            if (shape.Length == 1)
            {
                int desiredSize = (int)((totalPts / desiredPartsCount + 511) / 512) * 512;
                if (totalPts % desiredSize == 0)
                    return (int)(totalPts / desiredSize);
                else
                    return (int)(totalPts / desiredSize) + 1;
            }
            else
                return desiredPartsCount;
        }

        public void TouchAllRecords(string hash)
        {
            jobsDataContext.ExecuteCommand("update Job set Touchtime=GETUTCDATE() where Hash='{0}' AND Status={1}", hash, (byte)JobOrPartState.Completed);                        
        }

        void DeleteJobRecords(string hash)
        {
            jobsDataContext.ExecuteCommand("delete from Job where hash='{0}'", hash);                        
        }

        // Being called from the $CLEANUP job
        public void DeleteJobBlob(string hash)
        {
            try
            {
                TryCreateRequestsContainer();
                var requestBlob = resultsContainer.GetPageBlobReference(ResultBlobName(hash));
                requestBlob.DeleteIfExists();
            }
            catch (Exception e)
            {
                JobManagerTrace.TraceError("Error occurred while deleting blob: {0}", e.ToString());
            }
        }

        public void DeleteJobRecordsAndBlob(string hash)
        {
            DeleteJobRecords(hash);
            DeleteJobBlob(hash);
        }

        private void TryCreateRequestsContainer()
        {
            resultsContainer.CreateIfNotExists();
            resultsContainer.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
        }

        private bool CreateBlobWithJob(IFetchRequest request, string hash, double allowedCreationTimeSec)
        {
            var requestBlob = resultsContainer.GetPageBlobReference(ResultBlobName(hash));

            while (true)
            {
                try
                {
                    JobManagerTrace.TraceVerbose("Trying to create blob ({0})", hash);
                    requestBlob.Create(0, accessCondition: AccessCondition.GenerateIfNotExistsCondition());
                    JobManagerTrace.TraceVerbose("Blob {0} successfully created", hash);

                    //loading request into blob
                    string blobUri = ResultDataSetUri(hash, true).ToString();

                    JobManagerTrace.TraceVerbose("Filling blob with request. uri = {0}", blobUri);

                    RequestDataSetFormat.CreateRequestBlobDataSet(blobUri, request).Dispose();

                    return true;
                }
                catch (StorageException e)
                {
                    // Blob already exist - probably someone already works with it
                    if (e.RequestInformation.HttpStatusCode == 412 /* Procondition Failed*/)
                    {
                        JobManagerTrace.TraceVerbose("Can't create blob {0}. It is already exists", hash);
                        try
                        {
                            requestBlob.FetchAttributes();
                        }
                        catch(StorageException)
                        {
                            JobManagerTrace.TraceWarning("Can't get modification time of blob {0}. Retrying", hash);
                            continue;
                        }
                        double allowdSeconds = allowedCreationTimeSec;
                        if ((DateTime.UtcNow - requestBlob.Properties.LastModified.Value).TotalSeconds > allowdSeconds)
                        {
                            JobManagerTrace.TraceWarning("Job blob {0} exists but there are no job records in the job table exists longer than permitted time ({1}). deleting it", hash, allowdSeconds);
                            requestBlob.DeleteIfExists();
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else throw;
                }
            }
        }

        private void DiscardDataContextPendingChanges()
        {
            jobsDataContext.Refresh(RefreshMode.OverwriteCurrentValues);
            ChangeSet changeSet = jobsDataContext.GetChangeSet();
            if (changeSet != null)
            {
                //Undo inserts
                foreach (object objToInsert in changeSet.Inserts)
                {
                    jobsDataContext.GetTable(objToInsert.GetType()).DeleteOnSubmit(objToInsert);
                }
                //Undo deletes
                foreach (object objToDelete in changeSet.Deletes)
                {
                    jobsDataContext.GetTable(objToDelete.GetType()).InsertOnSubmit(objToDelete);
                }
            }
        }


        /// <summary>
        /// Evaluates subrequest for given partNo and origin point to place results at.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="partsCount"></param>
        /// <param name="partsNo"></param>
        /// <returns></returns>
        public static Tuple<IFetchRequest, int[]> EvaluateSubrequestData(IFetchRequest request, int partsCount, int partsNo)
        {
            var domain = request.Domain;
            int[] shape = domain.GetDataArrayShape();
            int[] origin = new int[shape.Length];
            if (partsCount == 1)
            {
                for (int i = 0; i < origin.Length; ++i) origin[i] = 0;
                return new Tuple<IFetchRequest, int[]>(request, origin);
            }
            //int partWidth = (shape[0] + partsCount - 1) / partsCount;
            int basePartWidth = shape[0] / partsCount;
            int partWidthResidue = shape[0] % partsCount;
            int leftBorder = basePartWidth * partsNo + Math.Min(partsNo, partWidthResidue);
            int rightBorder = leftBorder + basePartWidth;
            if (partsNo < partWidthResidue) ++rightBorder;
            if (domain.SpatialRegionType == SpatialRegionSpecification.CellGrid) ++rightBorder;
            //if ((domain.SpatialRegionType == SpatialRegionSpecification.Points || domain.SpatialRegionType == SpatialRegionSpecification.Cells) && domain.TimeRegion.SegmentsCount == 1)
            if (shape.Length == 1)
            {
                //assuming that it's possible to divide points into partsCount parts with at least (partsCount - 1) of them having equal size divisible by pageWidth
                if (shape[0] % partsCount == 0 && (shape[0] / partsCount) % pageWidth == 0)
                {
                    //everything is already computed in a right way
                }
                else
                {
                    int chunks = shape[0] / pageWidth;
                    int sizeInChunks = chunks / (partsCount - 1);
                    int partWidth = pageWidth * sizeInChunks;
                    leftBorder = partWidth * partsNo;
                    if (partsNo + 1 < partsCount)
                    {
                        rightBorder = leftBorder + partWidth;
                    }
                    else
                    {
                        rightBorder = shape[0];
                    }
                }
                //if (partWidthResidue > 0) ++basePartWidth;
                //int firstPartsWidth = 512 * ((basePartWidth + 511) / 512);
                //int totalFirstParts = shape[0] / firstPartsWidth;
                //if (totalFirstParts < partsCount - 1) throw new ArgumentException(string.Format(@"Can't divide a single dimensional request containing {0} points into {1} parts aligned by 512 points.", shape[0], partsCount));
                //leftBorder = firstPartsWidth * partsNo;
                //if (partsNo < totalFirstParts)
                //{
                //    rightBorder = leftBorder + firstPartsWidth;
                //}
                //else
                //{
                //    rightBorder = shape[0];
                //}
            }
            int width = rightBorder - leftBorder;
            double[] lats = null, lons = null, lats2 = null, lons2 = null;
            if (domain.Lats != null)
            {
                if (domain.SpatialRegionType == SpatialRegionSpecification.Points || domain.SpatialRegionType == SpatialRegionSpecification.Cells)
                {
                    lats = new double[width];
                    for (int i = 0; i < width; ++i) lats[i] = domain.Lats[leftBorder + i];
                }
                else
                {
                    lats = new double[domain.Lats.Length];
                    for (int i = 0; i < domain.Lats.Length; ++i) lats[i] = domain.Lats[i];
                }
            }
            if (domain.Lats2 != null)
            {
                if (domain.SpatialRegionType == SpatialRegionSpecification.Points || domain.SpatialRegionType == SpatialRegionSpecification.Cells)
                {
                    lats2 = new double[width];
                    for (int i = 0; i < width; ++i) lats2[i] = domain.Lats2[leftBorder + i];
                }
                else
                {
                    lats2 = new double[domain.Lats2.Length];
                    for (int i = 0; i < domain.Lats2.Length; ++i) lats2[i] = domain.Lats2[i];
                }
            }
            if (domain.Lons != null)
            {
                lons = new double[width];
                for (int i = 0; i < width; ++i) lons[i] = domain.Lons[leftBorder + i];
            }
            if (domain.Lons2 != null)
            {
                lons2 = new double[width];
                for (int i = 0; i < width; ++i) lons2[i] = domain.Lons2[leftBorder + i];
            }
            IFetchDomain newDomain;
            switch (domain.SpatialRegionType)
            {
                case SpatialRegionSpecification.Cells:
                    newDomain = FetchDomain.CreateCells(lats, lons, lats2, lons2, domain.TimeRegion, (bool[,])domain.Mask);
                    break;
                case SpatialRegionSpecification.PointGrid:
                    newDomain = FetchDomain.CreatePointGrid(lats, lons, domain.TimeRegion, (bool[, ,])domain.Mask);
                    break;
                case SpatialRegionSpecification.CellGrid:
                    newDomain = FetchDomain.CreateCellGrid(lats, lons, domain.TimeRegion, (bool[, ,])domain.Mask);
                    break;
                case SpatialRegionSpecification.Points:
                default:
                    newDomain = FetchDomain.CreatePoints(lats, lons, domain.TimeRegion, (bool[,])domain.Mask);
                    break;
            }
            origin[0] = leftBorder;
            for (int i = 1; i < origin.Length; ++i) origin[i] = 0;
            IFetchRequest newRequest = new FetchRequest(request.EnvironmentVariableName, newDomain, request.ReproducibilityTimestamp, request.ParticularDataSource);
            return new Tuple<IFetchRequest, int[]>(newRequest, origin);
        }
    }
}
