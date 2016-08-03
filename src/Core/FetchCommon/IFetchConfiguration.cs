using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.Research.Science.FetchClimate2
{
    /// <summary>Describes the snapshot of the configuration for some moment in time</summary>
    public interface IFetchConfiguration
    {
        /// <summary>
        /// A time to which the configuration corresponds
        /// </summary>
        DateTime TimeStamp { get; }

        /// <summary>
        /// The data source that are enabled
        /// </summary>
        IDataSourceDefinition[] DataSources { get; }

        /// <summary>
        /// The fetch variables that are provided at least by one enabled data source
        /// </summary>
        IVariableDefinition[] EnvironmentalVariables { get; }
    }

    public interface IDataSourceDefinition
    {
        /// <summary>
        /// An integer ID that is used in the provenance array of fetch request
        /// </summary>
        ushort ID { get; }

        /// <summary>
        /// string identifier of data source
        /// </summary>
        string Name { get; }

        /// <summary>
        /// A textural description of the data source
        /// </summary>
        string Description { get; }

        /// <summary>
        /// The copyright information about the data or data source code
        /// </summary>
        string Copyright { get;  }

        /// <summary>
        /// The service URI that provides the data source
        /// </summary>
        string Location { get; }

        /// <summary>
        /// The variables that can be fetched from the data source
        /// </summary>
        string[] ProvidedVariables { get; }
    }

    public interface IVariableDefinition
    {
        string Name { get; }
        string Units { get; }
        string Description { get; }
    }

    
    //public class SharedConstants
    //{
    //     //Users local storage path
    //    public static readonly string FcFolderPath;        
    //    public static readonly string LocalConfigurationConnectionString;
    //    public static readonly string TestsConfigurationConnectionString;                   

    //    static SharedConstants()
    //    {
    //        FcFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FetchClimate2");            
            
    //        LocalConfigurationConnectionString = String.Format(
    //            @"Data Source=(localdb)\v11.0;Integrated Security=True;Connect Timeout=15;Encrypt=False;TrustServerCertificate=False;database=FetchClimate2Configuration");
    //        TestsConfigurationConnectionString = String.Format(
    //            @"Data Source=(localdb)\v11.0;Integrated Security=True;Connect Timeout=15;Encrypt=False;TrustServerCertificate=False;database=FetchClimate2ConfigurationTests");
    //    }
    //}

    


}

