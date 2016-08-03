using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Research.Science.Data;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace LargeAzureBlobDsSample
{
    class Program
    {
        static void Main(string[] args)
        {
            var schema = new SerializableDataSetSchema(
                new SerializableDimension[] {
                    new SerializableDimension("i", 30000),
                    new SerializableDimension("j", 30000)
                },
                new SerializableVariableSchema[] {
                    new SerializableVariableSchema("vals", typeof(double), new string[] {"i", "j"}, null)
                },
                null
                );
            string BlobConnectionAccountName = @"fetchclimate2";
            string BlobConnectionAccountKey = @"vQpyUA7h5QFX6VlEH944gyv/h2Kx//WDy32brNip+YKDpsrN5/pxcSOnP2igQQ5pkA8lRXkmqmAYrgB29nwo/w==";
            string uri = @"msds:ab?DefaultEndpointsProtocol=http&Container=testcontainer&Blob=testBlob30000x30000&AccountName=" + BlobConnectionAccountName + @"&AccountKey=" + BlobConnectionAccountKey;
            try
            {
                var ds = AzureBlobDataSet.CreateEmptySet(uri, schema);
                double[,] data = new double[1, 30000];
                for (int i = 0; i < 30000; ++i) data[0, i] = (double)i;
                ds["vals"].PutData(new int[] { 29999, 0 }, data);
                var recvData = (double[,])ds["vals"].GetData(new int[] { 29999, 0 }, new int[] { 1, 30000 });
                for (int i = 0; i < 30000; ++i) if (data[0, i] != recvData[0, i]) throw new Exception("difference at " + i.ToString());
                Console.WriteLine("Everything is successful!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.ReadLine();

            //delete test blob
            try
            {
                AzureBlobDataSetUri azureUri = null;
                if (DataSetUri.IsDataSetUri(uri))
                    azureUri = new AzureBlobDataSetUri(uri);
                else
                    azureUri = AzureBlobDataSetUri.ToUri(uri);

                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(azureUri.ConnectionString);

                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(azureUri.Container);
                CloudPageBlob blob = container.GetPageBlobReference(azureUri.Blob);
                blob.DeleteIfExists();
                Console.WriteLine("Deleted test blob successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.ReadLine();
        }
    }
}
