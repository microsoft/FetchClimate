using Microsoft.Research.Science.Data;
using Microsoft.Research.Science.Data.Factory;
using Microsoft.Research.Science.Data.Memory;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.Research.Science.FetchClimate2;
using System.Reflection;
using Microsoft.Research.Science.Data.Factory;

namespace FetchWorker
{
    public class WorkerRole : RoleEntryPoint
    {
        private ManualResetEvent stopRequested = new ManualResetEvent(false);

        private SqlExtendedConfigurationProvider configProvider;
        private AssemblyStore azureGAC;
        private JobManager manager;
        private int touchPeriodInSeconds = 10;
        RunningJob current = null;


        public void Initialize()
        {
            if (!DataSetFactory.ContainsProvider("memory"))
                DataSetFactory.Register(typeof(MemoryDataSet));
            if (!DataSetFactory.ContainsProvider("ab"))
                DataSetFactory.Register(typeof(AzureBlobDataSet));
            string customTempLocalResourcePath =
                RoleEnvironment.GetLocalResource("localStorage1").RootPath;
            Environment.SetEnvironmentVariable("TMP", customTempLocalResourcePath);
            Environment.SetEnvironmentVariable("TEMP", customTempLocalResourcePath);

            string jobsDatabaseConnectionString = RoleEnvironment.GetConfigurationSettingValue("FetchClimate.JobsDatabaseConnectionString");
            string jobsStorageConnectionString = RoleEnvironment.GetConfigurationSettingValue("FetchClimate.JobsStorageConnectionString");
            foreach (TraceListener item in Trace.Listeners)
                if (!(item is DefaultTraceListener)) // The default trace listener is always in any TraceSource.Listeners collection.
                {
                    AutoRegistratingTraceSource.RegisterTraceListener(item);
                    WorkerTrace.TraceEvent(TraceEventType.Information, 19, string.Format("TraceListener \"{0}\" registered for accepting data from all AutoRegistratingTraceSources", item.ToString()));
                }

            JobManager.InitializeJobTable(jobsDatabaseConnectionString, false);

            configProvider = new SqlExtendedConfigurationProvider(
                RoleEnvironment.GetConfigurationSettingValue("ConfigurationDatabaseConnectionString"));
            WorkerTrace.TraceEvent(TraceEventType.Information, 6, string.Format("Connected to configuration database. Latest timestamp {0}",
                configProvider.GetConfiguration(DateTime.MaxValue).TimeStamp));

            azureGAC = new AssemblyStore(RoleEnvironment.GetConfigurationSettingValue("FetchWorker.AssemblyStoreConnectionString"));


            //overriding bug-containing default azure provider with the fixed one            
            Type t = typeof(DataSetFactory);
            var dict = (System.Collections.IDictionary)t.InvokeMember("providersByName", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.GetField, null, null, null);
            dict.Remove("az");
            DataSetFactory.Register(typeof(Microsoft.Research.Science.Data.Azure.AzureDataSet));
            WorkerTrace.TraceEvent(TraceEventType.Verbose, 9, "Available Scientific DataSet providers:\n" + DataSetFactory.RegisteredToString());


            if (int.TryParse(RoleEnvironment.GetConfigurationSettingValue("JobTouchPeriod"), out touchPeriodInSeconds))
            {
                WorkerTrace.TraceEvent(TraceEventType.Verbose, 10, string.Format("Touch period for processing job is set to {0}", touchPeriodInSeconds));
            }
            else
                WorkerTrace.TraceEvent(TraceEventType.Warning, 11, string.Format("Failed to read touch period from config. Parsing of value failed. touch period is set to {0}", touchPeriodInSeconds));

            manager = new JobManager(jobsDatabaseConnectionString, jobsStorageConnectionString);
                
            //Scheduling cleanup
            string instanceId = RoleEnvironment.CurrentRoleInstance.Id;
            int.TryParse(instanceId.Substring(instanceId.LastIndexOf(".") + 1), out instanceIndex);
            if (instanceIndex == 0)
            {
                double cleanPeriod = 0;
                if (!double.TryParse(RoleEnvironment.GetConfigurationSettingValue("HoursBetweenCleanup"), out cleanPeriod))
                {
                    cleanPeriod = 23;
                    WorkerTrace.TraceEvent(TraceEventType.Warning, 12, "Failed to parse period between clean-ups from configuration. Setting it to default 23 hours.");
                }
                cleanupTimeSpan = TimeSpan.FromHours(cleanPeriod);
                lastCleanUpTime = manager.LastCleanUpTime;
                DateTime now = DateTime.UtcNow;
                if (now - lastCleanUpTime >= cleanupTimeSpan)
                {
                    manager.SubmitCleanUp();
                    lastCleanUpTime = now;
                }
            }

            WorkerTrace.TraceEvent(TraceEventType.Verbose, 13, string.Format("starting Allocated memory: {0}Mb", GC.GetTotalMemory(false) / 1024 / 1024));

        }

        TimeSpan cleanupTimeSpan;
        int instanceIndex = 0;
        DateTime lastCleanUpTime;

        private Process currentProcess = Process.GetCurrentProcess();

        public override void Run()
        {
            WorkerTrace.TraceEvent(TraceEventType.Information, 0, "Running");
            WorkerTrace.TraceEvent(TraceEventType.Information, 1, string.Format("Worker assembly: {0}", Assembly.GetExecutingAssembly().FullName));

            WorkerTrace.TraceEvent(TraceEventType.Start, 2, "Initializing FetchWorker...");

            try
            {
                Initialize();
            }
            catch (Exception exc)
            {
                WorkerTrace.TraceEvent(TraceEventType.Critical, 2, string.Format("FetchWorker initialization failed: {0}", exc.ToString()));
                throw;
            }
            WorkerTrace.TraceEvent(TraceEventType.Stop, 2, "FetchWorker initialization complete");

            int heavyJobsPermitedCount = int.Parse(RoleEnvironment.GetConfigurationSettingValue("HeavyJobsPermitedCount"));
            int lightJobExecutionPermitedTimeSec = int.Parse(RoleEnvironment.GetConfigurationSettingValue("LightJobExecutionTimeLimitSec"));
            TimeSpan daysBeforeJobDeletion;
            {
                double days = 0;
                if (!double.TryParse(RoleEnvironment.GetConfigurationSettingValue("DaysBeforeJobDeletion"), out days))
                {
                    days = 60;
                    WorkerTrace.TraceEvent(TraceEventType.Warning, 3, string.Format("{0}: Bad \"DaysBeforeJobDeletion\" setting! Default value (60) is used.", JobManager.CleanUpJobHash));
                }
                daysBeforeJobDeletion = TimeSpan.FromDays(days);
            }
            while (true)
            {
                //Scheduling cleanup
                if (instanceIndex == 0)
                {
                    var utcnow = DateTime.UtcNow;
                    if (utcnow - lastCleanUpTime >= cleanupTimeSpan)
                    {
                        manager.SubmitCleanUp();
                        lastCleanUpTime = utcnow;
                        WorkerTrace.TraceEvent(TraceEventType.Information, 4, "Clean up job submited");
                    }
                }
                current = manager.PeekLockJob(stopRequested, int.Parse(RoleEnvironment.GetConfigurationSettingValue("JobQueuePollingMilisec")), heavyJobsPermitedCount,
                    (job, context) =>
                    {
                        if (job.Hash.Trim() == JobManager.CleanUpJobHash)
                        {
                            return new CleanUpJob(job, context, manager, daysBeforeJobDeletion, new JobSettings() { PermitedHeavyPartWorkers = heavyJobsPermitedCount, LightJobExecutionPermitedTimeSec = lightJobExecutionPermitedTimeSec, TouchPeriodInSeconds = touchPeriodInSeconds });
                        }
                        else
                            return new ComputationJob(job, context, configProvider, new JobSettings() { PermitedHeavyPartWorkers = heavyJobsPermitedCount, LightJobExecutionPermitedTimeSec = lightJobExecutionPermitedTimeSec, TouchPeriodInSeconds = touchPeriodInSeconds }, manager.ResultDataSetUri(job.Hash, true));
                    });
                if (current == null)
                    break;
                try
                {
                    WorkerTrace.TraceEvent(TraceEventType.Start, 5, string.Format("{0}: start processing", current.ToShortString()));
                    current.Perform();
                }
                catch (Exception exc)
                {
                    WorkerTrace.TraceEvent(TraceEventType.Error, 14, string.Format("{0}: error processing: {1}", current.ToShortString(), exc.ToString()));

                    current.Complete(false);

                    Exception toCheckForOutOfMemory = exc;
                    do
                    {
                        if (toCheckForOutOfMemory.GetType() == typeof(OutOfMemoryException))
                        {
                            WorkerTrace.TraceEvent(TraceEventType.Critical, 15, string.Format("{0}:Requesting role instance recycling due to OutOfMemory exception during calculation", current.ToShortString()));
                            if (current != null)
                                current.Abandon();
                            Thread.Sleep(25);
                            RoleEnvironment.RequestRecycle();
                        }
                        toCheckForOutOfMemory = toCheckForOutOfMemory.InnerException;
                    } while (toCheckForOutOfMemory != null);

                }
                finally
                {
                    if (current != null)
                        current.Dispose(); //dispose stops TouchThread
                    current = null;
                }
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            RoleEnvironment.Changed += RoleEnvironment_Changed;
            RoleEnvironment.Stopping += RoleEnvironment_Stopping;
            return base.OnStart();
        }

        void RoleEnvironment_Stopping(object sender, RoleEnvironmentStoppingEventArgs e)
        {
            WorkerTrace.TraceEvent(TraceEventType.Information, 16, "Role is stopping");
            stopRequested.Set();
            // Wait until processing stops            
            if (current != null)
                current.Abandon();
            WorkerTrace.TraceEvent(TraceEventType.Information, 17, "Stopping FetchWorker");
            Thread.Sleep(25); //waiting for logs transfer
        }

        void RoleEnvironment_Changed(object sender, RoleEnvironmentChangedEventArgs e)
        {
            WorkerTrace.TraceEvent(TraceEventType.Information, 18, "Role configuration has been changed. Requesting instance recycle");
            if (current != null)
                current.Abandon();
            Thread.Sleep(25); //waiting for logs transfer
            RoleEnvironment.RequestRecycle();

        }

        public static readonly AutoRegistratingTraceSource WorkerTrace = new AutoRegistratingTraceSource("FetchWorker", SourceLevels.All);
    }
}
