using Microsoft.WindowsAzure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Research.Science.FetchClimate2.Diagnostics
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
            hash = null;
            partNo = partCount = -1;
            string msg = record.Message;
            Match m = Regex.Match(msg, @"EventName=\""(.*)\"" Message=\""(.*)\"" TraceSource=\""(.*)\""",RegexOptions.Multiline) ;
            if (m.Success) {
                msg = m.Groups[2].Value;
                string[] parts = msg.Split(new char[] { ':' }, 4, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 4 && Int32.TryParse(parts[0], out partNo) && Int32.TryParse(parts[1], out partCount) && partNo >= 0 && partNo <= partCount)
                {
                    hash = parts[0];
                    message = parts[3];
                }
                else
                {
                    message = msg;
                    partNo = partCount = -1;
                }
            }
            else
            {
                message = msg;
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
            return Regex.IsMatch(hash, @"\A\b[0-9a-fA-F]+\b\Z");
        }

        public string ToHtmlTableRow()
        {
            return String.Format("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td><pre>{5}</pre></td><tr>",
                dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                instance,
                hash == null ? "" : hash,
                partCount == -1 ? "" : partCount.ToString(),
                partNo == -1 ? "" : partNo.ToString(),
                message);
        }

        public string ToCSVLine()
        {
            return String.Format("{0}, {1}, {2}, {3}, {4}, {5}",
                dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
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