using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Web;

namespace Frontend
{
    /// <summary>
    /// Typed Frontent configuration settings.
    /// </summary>
    /// <remarks>When run as a role instance, the settings come from a service configuration file. 
    /// Otherwise, the settings come from web.config/configuration/appSettings section.</remarks>
    public class FrontendSettings
    {
        private static FrontendSettings _current;

        private FrontendSettings()
        {
            Trace.TraceInformation(
                RoleEnvironment.IsAvailable ? "Reading settings from the service definition file" : "Reading settings from the web.config file");
            EnableAspnetDiagnosticTrace = GetBool("Frontend.EnableAspnetDiagnosticTrace", false);
            JobsDatabaseConnectionString = GetString("FetchClimate.JobsDatabaseConnectionString", "");
            JobTouchTimeTreshold = GetInt("Frontend.JobTouchTimeTreshold", 120);
            JobStatusCheckIntervalMilisec = GetInt("Frontend.JobStatusCheckIntervalMilisec", 100);
            ConfigurationDatabaseConnectionString = GetString("Frontend.ConfigurationDatabaseConnectionString", "");
            ResultBlobConnectionString = GetString("FetchClimate.JobsStorageConnectionString", "DevelopmentStorage=true");
            MinYearBoundary = GetInt("Frontend.MinYearBoundary", 1961);
            MaxYearBoundary = GetInt("Frontend.MaxYearBoundary", 1991);
            AllowedJobRegistrationSpan = GetDouble("Frontend.AllowedJobRegistrationSpan", 60.0);
            MinPtsPerPartition = GetInt("Frontend.MinPtsPerPartition", 1024);
            MaxPtsPerPartition = GetInt("Frontend.MaxPtsPerPartition", 1024000);
            WaitingFastResultPeriodSec = GetInt("Frontend.WaitingFastResultPeriodSec", 50);
        }
        public static FrontendSettings Current
        {
            get
            {
                if (null == _current) _current = new FrontendSettings();
                return _current;
            }
        }

        public string ConfigurationDatabaseConnectionString { get; private set; }
        public bool EnableAspnetDiagnosticTrace { get; private set; }
        public string JobsDatabaseConnectionString { get; private set; }
        public string ResultBlobConnectionString { get; private set; }
        public int MinYearBoundary { get; private set; }
        public int MaxYearBoundary { get; private set; }
        public double AllowedJobRegistrationSpan { get; private set; }
        public int MinPtsPerPartition { get; private set; }
        public int MaxPtsPerPartition { get; private set; }
        public int WaitingFastResultPeriodSec { get; private set; }
        public int JobTouchTimeTreshold { get; private set; }
        public int JobStatusCheckIntervalMilisec { get; private set; }

        /// <summary>
        /// Gets one setting value from a service configuration or a web.config file.
        /// </summary>
        /// <param name="settingKey">The setting key.</param>
        /// <returns>The setting string value or <c>null</c> if the setting is not found.</returns>
        string GetSetting(string settingKey)
        {
            if (RoleEnvironment.IsAvailable)
                try
                {
                    return RoleEnvironment.GetConfigurationSettingValue(settingKey);
                }
                catch (RoleEnvironmentException)
                {
                    Trace.TraceError("Cannot read role configuration setting " + settingKey);
                    return null;
                }
            else
            {
                if (ConfigurationManager.AppSettings.AllKeys.Contains(settingKey))
                {
                    return ConfigurationManager.AppSettings[settingKey];
                }
                else
                {
                    Trace.TraceError("Cannot read app setting " + settingKey);
                    return null;
                }
            }
        }
        string GetString(string settingKey, string defaultValue)
        {
            string value = GetSetting(settingKey) ?? defaultValue;
            Trace.TraceInformation(
                string.Format("{0} = {1}", settingKey, value));
            return value;
        }

        bool GetBool(string settingKey, bool defaultValue)
        {
            bool value = defaultValue;
            string setting = GetSetting(settingKey);
            if (null != setting && !bool.TryParse(setting, out value))
            {
                Trace.TraceError(
                    string.Format("{0}: cannot convert {1} to bool. Using default value.", settingKey, setting));
            }
            Trace.TraceInformation(
                string.Format("{0} = {1}", settingKey, value));
            return value;
        }
        int GetInt(string settingKey, int defaultValue)
        {
            int value = defaultValue;
            string setting = GetSetting(settingKey);
            if (null != setting && !int.TryParse(setting, out value))
            {
                Trace.TraceError(
                    string.Format("{0}: cannot convert {1} to int. Using default value.", settingKey, setting));
            }
            Trace.TraceInformation(
                string.Format("{0} = {1}", settingKey, value));
            return value;
        }
        double GetDouble(string settingKey, double defaultValue)
        {
            double value = defaultValue;
            string setting = GetSetting(settingKey);
            if (null != setting && !double.TryParse(setting, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                Trace.TraceError(
                    string.Format("{0}: cannot convert {1} to double. Using default value.", settingKey, setting));
            }
            Trace.TraceInformation(
                string.Format("{0} = {1}", settingKey, value));
            return value;
        }
    }
}