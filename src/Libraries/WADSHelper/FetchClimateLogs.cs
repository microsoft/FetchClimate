using Microsoft.WindowsAzure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WADSHelper
{
    public class FetchClimateLogItem
    {
        private readonly string hash;
        private readonly int partNo, partCount;
        private readonly DateTime dateTime;
        private readonly string instance;
        private readonly string message;

        public FetchClimateLogItem(WADRecord record)
        {
            int idx = record.Message.IndexOf(':');
            if (idx < 10 || idx > 20 || !IsHash(record.Message.Substring(0, idx)))
            {
                hash = null;
                partNo = partCount = -1;
                message = record.Message;
            }
            else
            {
                hash = record.Message.Substring(0, idx);
                message = record.Message.Substring(idx + 1);
                string[] parts = message.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 2 && Int32.TryParse(parts[0], out partNo) && Int32.TryParse(parts[1], out partCount) && partNo >= 0 && partNo < partCount)
                {
                    message = parts.Skip(2).Aggregate((a, b) => String.Concat(a, ":", b));
                }
                else
                {
                    partNo = partCount = -1;
                }
            }
            instance = record.RoleInstance;
            dateTime = new DateTime(record.EventTickCount);
        }

        public string Hash
        {
            get { return hash; }
        }

        public int PartNo
        {
            get { return partNo; }
        }

        public int PartCount
        {
            get { return partCount; }
        }

        public DateTime ItemDateTime
        {
            get { return dateTime; }
        }

        public string Instance
        {
            get { return instance; }
        }

        public string Message
        {
            get { return message; }
        }

        public static bool IsHash(string hash)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(hash, @"\A\b[0-9a-fA-F]+\b\Z");
        }

        public string ToCSVLine()
        {
            return String.Format("{0}, {1}, {2}, {3}, {4}, {5}",
                dateTime,
                instance,
                hash == null ? "" : hash,
                partCount == -1 ? "" : partCount.ToString(),
                partNo == -1 ? "" : partNo.ToString(),
                message.IndexOfAny(new char[] { ',', '\"', '\n', '\r' }) > -1 ? "\"" + message.Replace("\n", "").Replace("\r", "").Replace("\"", "\"\"") + "\"" : message);
        }
    }

    public class FetchClimateLogs
    {
        private readonly WADTableServiceContext context;

        public FetchClimateLogs(WADTableServiceContext context)
        {
            this.context = context;
        }

        public IEnumerable<FetchClimateLogItem> GetLogs(string hash = null, int days = 30)
        {
            DateTime start = DateTime.UtcNow;
            DateTime now = start;
            TimeSpan delta = TimeSpan.FromMinutes(20);
            List<FetchClimateLogItem> buffer = context.GetRecords(start - delta, start).Select(r => new FetchClimateLogItem(r)).ToList();
            int prevCount = buffer.Count;
            while ((now - start).TotalDays <= days)
            {
                start = start - delta;
                buffer.AddRange(context.GetRecords(start - delta, start).Select(r => new FetchClimateLogItem(r)));
                buffer.Sort((a, b) => (int)Math.Sign(b.ItemDateTime.Ticks - a.ItemDateTime.Ticks));
                for (int i = 0; i < prevCount; i++)
                {
                    var li = buffer.First();
                    buffer.RemoveAt(0);
                    if (String.IsNullOrEmpty(hash) || li.Hash == hash)
                        yield return li;
                }
                prevCount = buffer.Count;
            }
        }
    }
}