using System;
using System.Reflection;
using System.IO;
using Microsoft.WindowsAzure.Storage.Blob;
using CloudStorageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount;

namespace Microsoft.Research.Science.FetchClimate2
{
    public partial class AssemblyStore : MarshalByRefObject
    {
        class AzureBlobStore : BlobStore
        {
            private CloudBlobContainer azureGacContainer;
            public const string GACContainerName = "azure-gac";
            public AzureBlobStore(string connectionString)
            {
                var csa = CloudStorageAccount.Parse(connectionString);
                var blobClient = csa.CreateCloudBlobClient();
                azureGacContainer = blobClient.GetContainerReference(GACContainerName);
                azureGacContainer.CreateIfNotExists();
                Trace.Log.Connected(AppDomain.CurrentDomain.Id, "azure", azureGacContainer.Uri.ToString());
            }

            public override bool Contains(AssemblyName aname)
            {
                return azureGacContainer
                    .GetBlobReference(aname.FullName)
                    .Exists();
            }

            public override Assembly Load(AssemblyName aname)
            {
                var blob = azureGacContainer.GetBlockBlobReference(aname.FullName);
                using (var buf = new MemoryStream())
                {
                    Trace.Log.StartDownload(blob.Uri.ToString());
                    blob.DownloadToStream(buf);
                    Trace.Log.FinishDownload(blob.Uri.ToString());
                    return Assembly.Load(buf.ToArray());
                }
            }

            public override void Save(Assembly asm)
            {
                var blob = azureGacContainer.GetBlockBlobReference(asm.FullName);
                using (var fileStream = File.OpenRead(asm.Location))
                {
                    Trace.Log.StartUpload(asm.FullName);
                    blob.UploadFromStream(fileStream);
                    Trace.Log.FinishUpload(asm.FullName);
                }
            }
            public override void Clear()
            {
                var blobs = azureGacContainer.ListBlobs();
                foreach (var b in blobs)
                    if (b is CloudBlob)
                        ((CloudBlob)b).Delete();
            }
        }
    }
}
