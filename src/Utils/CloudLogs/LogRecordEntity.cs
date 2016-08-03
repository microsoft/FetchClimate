using Microsoft.WindowsAzure.StorageClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudLogs
{
    public class LogRecordEntity : TableServiceEntity
    {
        public LogRecordEntity(string hash, int partNo, DateTime begin)
        {
            this.PartitionKey = hash;
            this.RowKey = partNo.ToString() + " | " + begin.ToString();
            PeakPhysicalMemoryUsage = 0;
            PeakPagedMemoryUsage = 0;
            PeakVirtualMemoryUsage = 0;
        }

        public LogRecordEntity() { }

        public string RequestScheme { get; set; }

        public string Status { get; set; }

        public string RoleID { get; set; }

        public long PeakPhysicalMemoryUsage { get; set; }

        public long PeakPagedMemoryUsage { get; set; }

        public long PeakVirtualMemoryUsage { get; set; }

        public DateTime WorkStart { get; set; }

        public DateTime WorkEnd { get; set; }

        public DateTime SubmissionTime { get; set; }

        public DateTime ComputationFinishTime { get; set; }

        public double DownloadedDataSize { get; set; }
    }
}
