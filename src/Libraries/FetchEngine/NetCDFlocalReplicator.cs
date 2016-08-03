using Microsoft.Research.Science.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.Research.Science.FetchClimate2
{
    /// <summary>
    /// Downloads the .NC file from WEB and stores them for imidiate access on the local drive. Once the file was downloaded and stored, it is opened without redownloading until deleted from local file system
    /// </summary>
    public class NetCDFlocalReplicator
    {
        static internal readonly AutoRegistratingTraceSource traceSource = new AutoRegistratingTraceSource("NetCDFlocalReplicator", SourceLevels.All);
        static readonly ConcurrentDictionary<string, DataSet> dict = new ConcurrentDictionary<string, DataSet>();

        readonly static string cachePath = Path.Combine(Path.GetTempPath(),"ncFilesCache");

        /// <summary>
        /// Opens a predownloaded version of the DataSet or downloads it and opens upen download finished
        /// </summary>
        /// <param name="uriToDownloadFrom"></param>
        /// <returns></returns>
        public async static Task<DataSet> OpenOrCloneAsync(string uriToDownloadFrom)
        {
            traceSource.TraceEvent(TraceEventType.Information, 1, "Request for local replication of \"{0}\" NetCDF dataset", uriToDownloadFrom);
            lock("ncFilesCacheDirectoryCreating") {
                if(!Directory.Exists(cachePath))
                    Directory.CreateDirectory(cachePath);
            }

            byte[] bytes = uriToDownloadFrom.SelectMany(c => BitConverter.GetBytes(c)).ToArray();

            long hash = await SHA1Hash.HashAsync(bytes);
            DataSet ds;
            if (dict.TryGetValue(uriToDownloadFrom, out ds))
            {
                return ds;
            }

            traceSource.TraceEvent(TraceEventType.Information, 2, "Opening dataset corresponding to \"{0}\"", uriToDownloadFrom);
            string filename = string.Format("{0}.nc", hash);
            string fullFileName = Path.Combine(cachePath, filename);
            if (File.Exists(fullFileName))
            {
                traceSource.TraceEvent(TraceEventType.Information, 3, "NetCDF dataset \"{0}\" with hash {1} has already been downloaded previously and found on local FS at \"{2}\". opening it.", uriToDownloadFrom, hash, fullFileName);
                DataSet ds1 = DataSet.Open(string.Format("msds:nc?file={0}&openMode=readOnly", fullFileName));
                return dict.GetOrAdd(uriToDownloadFrom, ds1);
            }
            else
            {
                Stopwatch sw = Stopwatch.StartNew();
                string tempName = Path.GetTempFileName();
                traceSource.TraceEvent(TraceEventType.Information, 4, "NetCDF dataset \"{0}\" with hash {1} is not found localy. Initiating download to \"{2}\"", uriToDownloadFrom, hash, tempName);
                WebClient wc = new WebClient();
                await wc.DownloadFileTaskAsync(uriToDownloadFrom, tempName);
                sw.Stop();
                traceSource.TraceEvent(TraceEventType.Information, 5, "NetCDF dataset \"{0}\" with hash {1} has been downloaded in {2}.", uriToDownloadFrom, hash, sw.Elapsed);
                traceSource.TraceEvent(TraceEventType.Information, 5, "Copying downloaded file \"{0}\" into \"{1}\"", tempName, fullFileName);
                File.Copy(tempName, fullFileName, true);
                File.Delete(tempName);
                traceSource.TraceEvent(TraceEventType.Information, 5, "Opening \"{0}\" for \"{1}\"", fullFileName, uriToDownloadFrom);
                DataSet ds2 = DataSet.Open(string.Format("msds:nc?file={0}&openMode=readOnly", fullFileName));
                return dict.GetOrAdd(uriToDownloadFrom, ds2);
            }
        }
    }
}
