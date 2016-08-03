using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudLogs
{
    public class LogManager
    {
        CloudStorageAccount storageAccount;
        CloudTableClient tableClient;
        string tableName;

        public LogManager(string storageConnectionString, string tableName)
        {
            storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            tableClient = storageAccount.CreateCloudTableClient();
            this.tableName = tableName;
            tableClient.CreateTableIfNotExist(tableName);
        }

        public void Insert(params TableServiceEntity[] entries)
        {
            foreach (var i in entries)
            {
                var serviceContext = tableClient.GetDataServiceContext();
                serviceContext.AddObject(tableName, i);
                serviceContext.SaveChangesWithRetries();
            }
        }

    }
}
