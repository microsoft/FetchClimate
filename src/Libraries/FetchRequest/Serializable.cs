using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    
    namespace Serializable
    {
        /// <summary>
        /// the class that is used by JSON deserializer. it is not intended to be used elsewhere in the system except deserialization.
        /// Use ConvertFromSerializable to generate original FetchConfiguration.
        /// </summary>
        public class FetchRequest
        {
            public string EnvironmentVariableName { get; set; }
            public DateTime ReproducibilityTimestamp { get; set; }
            public string[] ParticularDataSources { get; set; }
            public FetchDomain Domain { get; set; }

            public FetchRequest() { }
            public FetchRequest(FetchClimate2.IFetchRequest request)
            {
                EnvironmentVariableName = request.EnvironmentVariableName;
                ReproducibilityTimestamp = DateTime.SpecifyKind(request.ReproducibilityTimestamp, DateTimeKind.Utc);
                ParticularDataSources = request.ParticularDataSource;
                Domain = new FetchDomain(request.Domain);
            }

            public FetchClimate2.IFetchRequest ConvertFromSerializable()
            {
                return new FetchClimate2.FetchRequest(EnvironmentVariableName, Domain.ConvertFromSerializable(), ReproducibilityTimestamp, ParticularDataSources);
            }
        }

        /// <summary>
        /// the class that is used by JSON deserializer. it is not intended to be used elsewhere in the system except deserialization.
        /// Use ConvertFromSerializable to generate original FetchConfiguration.
        /// </summary>
        public class FetchDomain
        {
            public double[] Lats { get; set; }
            public double[] Lons { get; set; }
            public double[] Lats2 { get; set; }
            public double[] Lons2 { get; set; }
            public TimeRegion TimeRegion { get; set; }
            public string SpatialRegionType { get; set; }
            public Array Mask { get; set; }

            public FetchDomain()
            { }

            public FetchDomain(FetchClimate2.IFetchDomain domain)
            {
                Lats = domain.Lats;
                Lons = domain.Lons;
                Lats2 = domain.Lats2;
                Lons2 = domain.Lons2;
                TimeRegion = new TimeRegion(domain.TimeRegion);
                SpatialRegionType = domain.SpatialRegionType.ToString();
                Mask = domain.Mask;
            }

            public FetchClimate2.FetchDomain ConvertFromSerializable()
            {
                SpatialRegionSpecification regType;
                switch (SpatialRegionType)
                {
                    case "Points": regType = SpatialRegionSpecification.Points; break;
                    case "Cells": regType = SpatialRegionSpecification.Cells; break;
                    case "PointGrid": regType = SpatialRegionSpecification.PointGrid; break;
                    case "CellGrid": regType = SpatialRegionSpecification.CellGrid; break;
                    default: throw new InvalidOperationException(string.Format("unsupported SpatialRegionType ({0})", SpatialRegionType));
                }
                return new FetchClimate2.FetchDomain(Lats, Lons, Lats2, Lons2, TimeRegion.ConvertFromSerializable(), regType, Mask);
            }
        }

        /// <summary>
        /// the class that is used by JSON deserializer. it is not intended to be used elsewhere in the system except deserialization.
        /// Use ConvertFromSerializable to generate original FetchConfiguration.
        /// </summary>
        public class TimeRegion
        {
            public int[] Years { get; set; }
            public int[] Days { get; set; }
            public int[] Hours { get; set; }

            public bool IsIntervalsGridYears { get; set; }
            public bool IsIntervalsGridDays { get; set; }
            public bool IsIntervalsGridHours { get; set; }

            public TimeRegion() { }
            public TimeRegion(FetchClimate2.ITimeRegion timeRegion)
            {
                Years = timeRegion.Years;
                Days = timeRegion.Days;
                Hours = timeRegion.Hours;
                IsIntervalsGridDays = timeRegion.IsIntervalsGridDays;
                IsIntervalsGridHours = timeRegion.IsIntervalsGridHours;
                IsIntervalsGridYears = timeRegion.IsIntervalsGridYears;
            }

            public FetchClimate2.TimeRegion ConvertFromSerializable()
            {
                return new FetchClimate2.TimeRegion(this.Years,this.Days,this.Hours,this.IsIntervalsGridYears,this.IsIntervalsGridDays,this.IsIntervalsGridHours);
            }
        }
    }

    #region serializable wrappers

    namespace Serializable
    {
        /// <summary>
        /// the class that is used by JSON deserializer. it is not intended to be used elsewhere in the system except deserialization.
        /// Use ConvertFromSerializable to generate original FetchConfiguration.
        /// </summary>
        [DataContract]
        public class FetchConfiguration
        {
            public FetchConfiguration() { }

            public FetchConfiguration(FetchClimate2.IFetchConfiguration configuration) {
                this.TimeStamp = configuration.TimeStamp;
                this.DataSources = configuration.DataSources.Select(ds => new DataSourceDefinition(ds)).ToArray();
                this.EnvironmentalVariables = configuration.EnvironmentalVariables.Select(ev => new VariableDefinition(ev)).ToArray();
            }

            [DataMember]
            public DateTime TimeStamp { get; set; }
            [DataMember]
            public DataSourceDefinition[] DataSources { get; set; }
            [DataMember]
            public VariableDefinition[] EnvironmentalVariables { get; set; }

            public FetchClimate2.FetchConfiguration ConvertFromSerializable()
            {
                return new FetchClimate2.FetchConfiguration(
                    TimeStamp,
                    DataSources.Select(ds => ds.ConvertFromSerializable()).ToArray(),
                    EnvironmentalVariables.Select(v => v.ConvertFromSerializable()).ToArray());
            }
        }

        [DataContract]
        public class DataSourceDefinition 
        {
            public DataSourceDefinition()
            { }

            public DataSourceDefinition(FetchClimate2.IDataSourceDefinition definition)
            {
                this.ID = definition.ID;
                this.Name = definition.Name;
                this.Description = definition.Description;
                this.Copyright = definition.Copyright;
                this.Location = definition.Location;
                this.ProvidedVariables = definition.ProvidedVariables.ToArray();
            }

            [DataMember]
            public ushort ID { get; set; }
            [DataMember]
            public string Name { get; set; }
            [DataMember]
            public string Description { get; set; }
            [DataMember]
            public string Copyright { get; set; }
            [DataMember]
            public string Location { get; set; }
            [DataMember]
            public string[] ProvidedVariables { get; set; }

            public FetchClimate2.DataSourceDefinition ConvertFromSerializable()
            {
                return new FetchClimate2.DataSourceDefinition(ID, Name, Description, Copyright, Location, ProvidedVariables);
            }
        }

        [DataContract]
        public class VariableDefinition 
        {
            public VariableDefinition()
            { }

            public VariableDefinition(FetchClimate2.IVariableDefinition definition)
            {
                this.Name = definition.Name;
                this.Units = definition.Units;
                this.Description = definition.Description;
            }

            [DataMember]
            public string Name { get; set; }
            [DataMember]
            public string Units { get; set; }
            [DataMember]
            public string Description { get; set; }

            public FetchClimate2.VariableDefinition ConvertFromSerializable()
            {
                return new FetchClimate2.VariableDefinition(Name, Units, Description);
            }
        }

    }

    #endregion
}
