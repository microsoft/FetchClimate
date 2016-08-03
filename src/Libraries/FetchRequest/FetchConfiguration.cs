using Microsoft.Research.Science.FetchClimate2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    /// <summary>Describes the snapshot of the configuration for some moment in time</summary>
    public class FetchConfiguration : IFetchConfiguration
    {
        /// <summary>Constructs the snapshot of the configuration for some moment in time</summary>
        /// <param name="timeStamp">DateTime of the snapshot in UTC</param>
        /// <param name="dataSources">Array of data sources</param>
        /// <param name="variables">Array of environmental variables</param>
        public FetchConfiguration(DateTime timeStamp, IDataSourceDefinition[] dataSources, IVariableDefinition[] variables)
        {
            this.TimeStamp = DateTime.SpecifyKind(timeStamp, DateTimeKind.Utc);
            this.DataSources = dataSources;
            this.EnvironmentalVariables = variables;
        }

        /// <summary>
        /// A time to which the configuration corresponds
        /// </summary>
        public DateTime TimeStamp { get; internal set; }

        /// <summary>
        /// The data source that are enabled
        /// </summary>
        public IDataSourceDefinition[] DataSources { get; internal set; }

        /// <summary>
        /// The fetch variables that are provided at least by one enabled data source
        /// </summary>
        public IVariableDefinition[] EnvironmentalVariables { get; internal set; }

        public override bool Equals(object obj)
        {
            var fc = obj as FetchConfiguration;
            if (fc == null || TimeStamp != fc.TimeStamp || DataSources.Length != fc.DataSources.Length || EnvironmentalVariables.Length != fc.EnvironmentalVariables.Length)
                return false;
            for (int i = 0; i < DataSources.Length; i++)
                if (!DataSources[i].Equals(fc.DataSources[i]))
                    return false;
            for (int i = 0; i < DataSources.Length; i++)
                if (!fc.EnvironmentalVariables.Any(v => v.Equals(EnvironmentalVariables[i])))
                    return false;
            for (int i = 0; i < fc.DataSources.Length; i++)
                if (!fc.EnvironmentalVariables.Any(v => v.Equals(EnvironmentalVariables[i])))
                    return false;
            return true;
        }
    }

    public class DataSourceDefinition : IDataSourceDefinition
    {
        public DataSourceDefinition(ushort id, string name, string description, string copyright, string location, string[] providedVariables)
        {
            this.ID = id;
            this.Name = name;
            this.Description = description;
            this.Location = location;
            this.Copyright = copyright;
            this.ProvidedVariables = providedVariables;
        }

        /// <summary>
        /// An integer ID that is used in the provenance array of fetch request
        /// </summary>
        public ushort ID { get; internal set; }

        /// <summary>
        /// string identifier of data source
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// A textural description of the data source
        /// </summary>
        public string Description { get; internal set; }

        /// <summary>
        /// The copyright information about the data or data source code
        /// </summary>
        public string Copyright { get; internal set; }

        /// <summary>
        /// The service URI that provides the data source
        /// </summary>
        public string Location { get; internal set; }

        /// <summary>
        /// The variables that can be fetched from the data source
        /// </summary>
        public string[] ProvidedVariables { get; internal set; }

        public override bool Equals(object obj)
        {
            var dsd = obj as DataSourceDefinition;
            if (dsd == null || ID != dsd.ID || Name != dsd.Name || Description != dsd.Description || Copyright != dsd.Copyright || Location != dsd.Location || ProvidedVariables.Length != dsd.ProvidedVariables.Length)
                return false;
            for (int i = 0; i < ProvidedVariables.Length; i++)
                if (!dsd.ProvidedVariables.Any(v => ProvidedVariables[i] == v))
                    return false;
            for (int i = 0; i < dsd.ProvidedVariables.Length; i++)
                if (!ProvidedVariables.Any(v => dsd.ProvidedVariables[i] == v))
                    return false;
            return true;
        }
    }

    public class VariableDefinition : IVariableDefinition
    {
        public VariableDefinition(string name, string units, string description)
        {
            this.Name = name;
            this.Units = units;
            this.Description = description;
        }

        public string Name { get; internal set; }
        public string Units { get; internal set; }
        public string Description { get; internal set; }

        public override bool Equals(object obj)
        {
            var vd = obj as VariableDefinition;
            return (vd != null && Name == vd.Name && Units == vd.Units && Description == vd.Description);
        }
    }

}
