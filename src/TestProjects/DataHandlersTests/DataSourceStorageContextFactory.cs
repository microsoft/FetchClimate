using Microsoft.Research.Science.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Tests
{
    static class TestDataStorageFactory
    {
        public static IStorageContext GetStorageContext(string uri)
        {
            return new LinearizingStorageContext(DataSet.Open(uri));            
        }
    }
}
