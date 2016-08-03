using System;
using System.Reflection;
using System.IO;

namespace Microsoft.Research.Science.FetchClimate2
{
    public partial class AssemblyStore 
    {
        class FolderBlobStore : BlobStore
        {
            private string basePath;
            public FolderBlobStore(string connectionString)
            {
                basePath = Path.GetFullPath(connectionString);
                if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);
                Trace.Log.Connected(AppDomain.CurrentDomain.Id, "folder", basePath);
            }

            private string GetFileName(AssemblyName aname)
            {
                return Path.Combine(basePath, aname.Name + "—" + aname.Version.ToString());
            }
            public override bool Contains(AssemblyName aname)
            {
                return File.Exists(GetFileName(aname));
            }

            public override Assembly Load(AssemblyName aname)
            {
                var fname = GetFileName(aname);
                Trace.Log.StartDownload(fname);
                var buf = File.ReadAllBytes(fname);
                Trace.Log.FinishDownload(fname);
                return Assembly.Load(buf);
            }

            public override void Save(Assembly asm)
            {
                Trace.Log.StartUpload(asm.FullName);
                var buf = File.ReadAllBytes(asm.Location);
                File.WriteAllBytes(GetFileName(asm.GetName()), buf);
                Trace.Log.FinishUpload(asm.FullName);
            }
            public override void Clear()
            {
                var blobs = Directory.EnumerateFiles(basePath);
                foreach (var b in blobs)
                    File.Delete(b);
            }
        }
    }
}
