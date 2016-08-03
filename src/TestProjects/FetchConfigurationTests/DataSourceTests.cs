using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Reflection;
using System.Data.SqlClient;
using Microsoft.Research.Science.Data;

namespace DataSourceCatalogTests
{
    [TestClass]
    public class DataSourceTests
    {
        /// <summary>Tests connection to local database. 
        /// Database should be empty and zero timestamp is returned</summary>
        [TestMethod]
        public void LocalConnectionTest()
        {
            var dc = new FetchConfigurationDataContext (GetTestConnectionString());
            Assert.AreEqual(dc.GetTimeStamp().ReturnValue, 0);
        }

        protected static string GetTestConnectionString()
        {
            return String.Format(@"Data Source=(LocalDB)\v11.0;AttachDbFilename={0};Integrated Security=True;",
                Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName),
                    "FetchConfiguration.mdf"));
        }

        [TestCleanup]
        public void ResetDB()
        {
            SqlConnection conn = new SqlConnection(GetTestConnectionString());
            conn.Open();
            try
            {
                SqlCommand cmd = new SqlCommand("DELETE FROM DataSource", conn);
                cmd.ExecuteNonQuery();
            }
            finally
            {
                conn.Close();
            }
            
        }
    }
}
