using Microsoft.Research.Science.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Microsoft.Research.Science.Data
{
    public class AzureBlobDataSetUri : DataSetUri
    {
        public AzureBlobDataSetUri() : base(typeof(AzureBlobDataSet))
        {
        }

        public AzureBlobDataSetUri(string uri) : base(uri, typeof(AzureBlobDataSet))
        {
        }

        private string GetAzureConnectionString()
        {
            if (ContainsParameter("UseDevelopmentStorage") && string.Compare(this["UseDevelopmentStorage"],"true",true)==0)
                return "UseDevelopmentStorage=true";

            string[] sqlKeys = new string[] { "DevelopmentStorageProxyUri", "DefaultEndpointsProtocol", "AccountName", "AccountKey" };

            StringBuilder sb = new StringBuilder();
            foreach (string key in sqlKeys)
            {
                if (ContainsParameter(key))
                {
                    sb.Append(key);
                    sb.Append("=");
                    sb.Append(GetParameterValue(key));
                    sb.Append(";");
                }
            }
            string configSettings = sb.ToString();

            if (configSettings.EndsWith(";"))
                return configSettings.Substring(0, configSettings.Length - 1);
            else
                return configSettings;
        }


        public static AzureBlobDataSetUri ToUri(string uri)
        {
            string providerName = Microsoft.Research.Science.Data.Factory.DataSetFactory.GetProviderNameByType(typeof(AzureBlobDataSet)) ??
                  ((DataSetProviderNameAttribute)typeof(AzureBlobDataSet).GetCustomAttributes(typeof(DataSetProviderNameAttribute), false)[0]).Name;

            return new AzureBlobDataSetUri(
                String.Format("{0}:{1}?{2}",
                    DataSetUri.DataSetUriScheme, providerName,
                    uri.Replace(';', '&')));
        }

        private string connectionString = null;
        /// <summary>
        /// Gets configuration settings to Windows Azure.
        /// </summary>
        public string ConnectionString
        {
            get
            {
                if (string.IsNullOrEmpty(this.connectionString))
                    this.connectionString = GetAzureConnectionString();
                return this.connectionString;
            }
        }

        /// <summary>
        /// Gets property, which indicates, whether to use Azure development storage or not.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If this parameter has true value, 
        /// all other connection parameters such as DefaultEndpointsProtocol, 
        /// AccountName, AccountKey will be ignored.
        /// Specify this parameter to true, if you eant to work with local Azure
        /// development storage.
        /// </para>
        /// </remarks>
        [Description("Specifies parameter, which indicates, whether to work with development storage, or with cloud.\nIf true, all Cloud related parameters will be ignored.")]
        public bool UseDevelopmentStorage
        {
            get
            {
                if (!ContainsParameter("UseDevelopmentStorage"))
                    return false;
                else
                {
                    bool boolValue;
                    if (!bool.TryParse(GetParameterValue("UseDevelopmentStorage"), out boolValue))
                        return false;
                    else
                        return boolValue;
                }
            }
            set
            {
                SetParameterValue("UseDevelopmentStorage", value.ToString().ToLower());
            }
        }

        /// <summary>
        /// Gets property, which indicates which Endpoint protocol to use.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Endpoint protocol can be http or https.
        /// Working with http is faster but is not always supported
        /// (There can be restrictions in your lan).
        /// </para>
        /// </remarks>
        [Description("Specifies parameter, which indicates which Endpoint protocol to use. Can be http or https.")]
        public EndpointProtocol DefaultEndpointsProtocol
        {
            get
            {
                return (EndpointProtocol)Enum.Parse(typeof(EndpointProtocol), GetParameterValue("DefaultEndpointsProtocol"));
            }
            set
            {
                SetParameterValue("DefaultEndpointsProtocol", value.ToString());
            }
        }


        /// <summary>
        /// Gets Azure account key.
        /// </summary>
        /// <remarks>
        /// Azure account key is key to your Windows Azure account.
        /// </remarks>
        [Description("Specifies Azure account key.")]
        public string AccountKey
        {
            get
            {
                return GetParameterValue("AccountKey");
            }
            set
            {
                SetParameterValue("AccountKey", value);
            }
        }

        /// <summary>
        /// Gets Azure account name.
        /// </summary>
        /// <remarks>
        /// Azure account name of yout Windows Azure account.
        /// </remarks>
        [Description("Specifies Azure account name.")]
        public string AccountName
        {
            get
            {
                return GetParameterValue("AccountName");
            }
            set
            {
                SetParameterValue("AccountName", value);
            }
        }

        /// <summary>
        /// Gets Development Storage Proxy URIe.
        /// </summary>
        /// <remarks>
        /// This parameter works only when UseDevelopmentStorage has true value.
        /// Otherwise, it will be ignored.
        /// </remarks>
        [Description("Specifies Development Storage Proxy URI.\nWorks only with Development Storage.")]
        public string DevelopmentStorageProxyUri
        {
            get
            {
                return GetParameterValue("DevelopmentStorageProxyUri");
            }
            set
            {
                SetParameterValue("DevelopmentStorageProxyUri", value);
            }
        }

        /// <summary>
        /// Gets the name of the Azure blob container in which blob with dataset is stored.
        /// </summary>
        [Description("Specifies Azure blob container in which blob with dataset is stored.")]
        public string Container
        {
            get
            {
                return GetParameterValue("Container");
            }
            set
            {
                SetParameterValue("Container", value);
            }
        }

        /// <summary>
        /// Gets the name of the Azure blob in which dataset is stored.
        /// </summary>
        [Description("Specifies Azure blob in which dataset is stored.")]
        public string Blob
        {
            get
            {
                return GetParameterValue("Blob");
            }
            set
            {
                SetParameterValue("Blob", value);
            }
        }
    }

    /// <summary>
    /// Specifies endpoint protocol, which <see cref="AzureBlobDataSet"/> uses to connect to Azure cloud storage.
    /// </summary>
    public enum EndpointProtocol
    {
        /// <summary>
        /// Http endpoint protocol.
        /// </summary>
        http,
        /// <summary>
        /// Https endpoint protocol.
        /// </summary>
        https
    }
}
