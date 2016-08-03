using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    static class SqlHelper
    {
        /// <summary>
        /// Can the connection to the SQL server be established
        /// </summary>
        public static bool IsSqlServerAvailable(string connectionString)
        {
            SqlConnectionStringBuilder builder = new System.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
            builder.InitialCatalog = string.Empty;
            using (SqlConnection conn = new System.Data.SqlClient.SqlConnection(builder.ConnectionString))
            {
                try
                {
                    conn.Open();
                }
                catch (SqlException)
                {
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// True is the sql server is available and the database with the name specified in the connection string exists
        /// </summary>
        public static bool DoesDataBaseExist(string connectionString)
        {
            if (!IsSqlServerAvailable(connectionString))
                    return false;
                SqlConnectionStringBuilder builder = new System.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
                string dbName = builder.InitialCatalog;
                builder.InitialCatalog = string.Empty;
                using (SqlConnection conn = new System.Data.SqlClient.SqlConnection(builder.ConnectionString))
                {
                    conn.Open();
                    var command = conn.CreateCommand();
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = "use [master]";
                    command.ExecuteNonQuery();
                    command.CommandText = string.Format("SELECT COUNT(*) FROM sys.databases where name=N'{0}' or name=N'{1}'", dbName, dbName.TrimStart('[').TrimEnd(']'));
                    int exists = (int)command.ExecuteScalar();
                    return exists == 1;
                }
        }

        /// <summary>
        /// true if the sql server is available, database with specified name exists and its schema is not empty
        /// </summary>
        public static bool IsDataBaseDeployed(string connectionString)
        {
            if (!DoesDataBaseExist(connectionString))
                return false;
            SqlConnectionStringBuilder builder = new System.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
            string dbName = builder.InitialCatalog;
            using (SqlConnection conn = new System.Data.SqlClient.SqlConnection(connectionString))
            {
                conn.Open();
                var command = conn.CreateCommand();
                command.CommandType = System.Data.CommandType.Text;
                command.CommandText = string.Format("use [{0}]", dbName);
                command.ExecuteNonQuery();
                command.CommandText = string.Format("SELECT COUNT(*) FROM sys.tables");
                int tablesCount = (int)command.ExecuteScalar();
                return tablesCount > 1; //as fetch workers can create Jobs table
            }
        }

        /// <summary>
        /// creates an empty database with the name specified in the connection string
        /// </summary>
        public static void CreateDatabase(string connectionString)
        {
            SqlConnectionStringBuilder builder = new System.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
            string dbName = builder.InitialCatalog;
            builder.InitialCatalog = string.Empty;
            using (SqlConnection conn = new System.Data.SqlClient.SqlConnection(builder.ConnectionString))
            {
                conn.Open();
                var command = conn.CreateCommand();
                command.CommandType = System.Data.CommandType.Text;
                command.CommandText = "use [master]";
                command.ExecuteNonQuery();
                command.CommandText = string.Format("create database {0} collate SQL_Latin1_General_CP1_CS_AS", dbName);
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scriptText">A script without SQLCMD operators (but with GO operators) to execute for the database specified in the connection string.</param>        
        public static  void ExecuteSqlScript(string connectionString, string scriptText)
        {
            Regex regex = new Regex("^GO", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            string[] lines = regex.Split(scriptText);

            using (SqlConnection connection = new System.Data.SqlClient.SqlConnection(connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();
                using (SqlCommand cmd = connection.CreateCommand())
                {
                    cmd.Connection = connection;
                    cmd.Transaction = transaction;

                    foreach (string line in lines)
                    {
                        if (line.Length > 0)
                        {
                            cmd.CommandText = line;
                            cmd.CommandType = System.Data.CommandType.Text;

                            try
                            {
                                //Trace.WriteLine("{0}:{1}", i++, cmd.CommandText);
                                cmd.ExecuteNonQuery();
                            }
                            catch (SqlException)
                            {
                                transaction.Rollback();
                                throw;
                            }
                        }
                    }
                }

                transaction.Commit();
            }
        }
    }
}
