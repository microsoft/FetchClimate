using AzureBlobSet;
using Microsoft.Research.Science.Data;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;

namespace AzureBlobSetSample
{
    class Program
    {
        static void Main(string[] args)
        {
            string uri = @"msds:nc?file=\\vienna.mslab.cs.msu.su\ClimateData\air.sig995.2007.nc&openMode=readOnly";
            DataSet d = DataSet.Open(uri);
            string blobUri = @"msds:ab?UseDevelopmentStorage=true&Container=testcontainer&Blob=testblob";
            DataSet blobD = /*new AzureBlobDataSet(blobUri);*/AzureBlobDataSet.ArrangeData(blobUri, d, new SerializableVariableSchema[0]);

            AzureBlobDataSetUri azureUri = new AzureBlobDataSetUri(blobUri);

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(azureUri.ConnectionString);

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container 
            CloudBlobContainer container = blobClient.GetContainerReference(azureUri.Container);

            CloudPageBlob blob = container.GetPageBlobReference(azureUri.Blob);
            Int32 schemeSize;

            using (BinaryReader br = new BinaryReader(blob.OpenRead()))
            {
                //schemeSize = br.ReadInt32();
                UTF8Encoding utf8 = new UTF8Encoding();
                byte[] buffer = new byte[8192];
                br.BaseStream.Read(buffer, 0, 8192);
                string sizeStr = utf8.GetString(buffer);
                Console.WriteLine(sizeStr);
                //schemeSize = Int32.Parse(sizeStr);
                //br.BaseStream.Seek(512, SeekOrigin.Begin);
                //DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(DataSetInfo));
                //byte[] scheme = new byte[schemeSize];
                //br.BaseStream.Read(scheme, 0, schemeSize);
                //info = (DataSetInfo)serializer.ReadObject(new MemoryStream(scheme));
            }

            //foreach (var i in blobD.Variables) Console.WriteLine(i.Name);

            Single[] a = (Single[])blobD["lat"].GetData();

            blobD["lat"].PutData(a);

            //foreach (var i in a) Console.WriteLine(i.ToString());

            foreach (var i in blobD["lat"].Metadata) Console.WriteLine(i.Key.ToString() + " = " + i.Value.ToString());
            foreach (var i in blobD.Metadata) Console.WriteLine(i.Key.ToString() + " = " + i.Value.ToString());

            Console.ReadLine();
        }
    }
}
