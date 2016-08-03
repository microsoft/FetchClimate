using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Table.DataServices;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Research.Science.FetchClimate2.Diagnostics
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
            CloudTableClient client = new CloudTableClient(storageAccount.TableEndpoint, storageAccount.Credentials);
            return new WADTableServiceContext(client);
        }

        public WADTableServiceContext(CloudTableClient client)
            : base(client)
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

        public void DeleteOldRecords(DateTime stop)
        {
            string endTicks = "0" + stop.Ticks;
            var toDel = Records.Where(r => r.PartitionKey.CompareTo(endTicks) < -0).AsTableServiceQuery(this);
            foreach (var e in toDel)
            {
                this.DeleteObject(e);
            }
            this.SaveChangesWithRetries(System.Data.Services.Client.SaveChangesOptions.ContinueOnError);
        }
    }
}