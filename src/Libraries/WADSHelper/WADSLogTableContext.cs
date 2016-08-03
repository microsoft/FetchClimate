using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WADSHelper
{
    public class WADRecord : TableServiceEntity
    {
        public long EventTickCount { get; set; }
        public string DeploymentId { get; set; }
        public string Role { get; set; }
        public string RoleInstance { get; set; }
        public int EventId { get; set; }
        public int Level { get; set; }
        public int Pid { get; set; }
        public int Tid { get; set; }
        public string Message { get; set; }

        public DateTime EventDateTime
        {
            get
            {
                return new DateTime(EventTickCount, DateTimeKind.Utc);
            }
        }

    }

    public class WADTableServiceContext : TableServiceContext
    {
        public static WADTableServiceContext CreateFromRoleSettings()
        {
            string wadConnectionString = "Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString";
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(wadConnectionString));
            return new WADTableServiceContext(storageAccount.TableEndpoint.ToString(), storageAccount.Credentials);
        }

        public WADTableServiceContext(string baseAddress, StorageCredentials credentials)
            : base(baseAddress, credentials)
        {
            ResolveType = s => typeof(WADRecord);
        }

        public IQueryable<WADRecord> Records
        {
            get { return CreateQuery<WADRecord>("WADLogsTable"); }
        }

        public IEnumerable<WADRecord> GetRecords(DateTime start, DateTime stop)
        {
            string startTicks = "0" + start.Ticks;
            string endTicks = "0" + stop.Ticks;
            return Records.Where(r =>
                r.PartitionKey.CompareTo(startTicks) >= 0 &&
                r.PartitionKey.CompareTo(endTicks) < -0);
        }
    }
}