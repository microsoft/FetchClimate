using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    public interface IStorageRequest
    {
        string VariableName
        {
            get;
        }

        int[] Origin
        {
            get;
        }

        int[] Stride
        {
            get;
        }

        int[] Shape
        {
            get;
        }
    }       

    public interface IStorageResponse
    {
        Array Data { get;}
        IStorageRequest Request { get; }
    }
   
    public interface IDataStorageDefinition
    {
        ReadOnlyDictionary<string, object> GlobalMetadata { get; }
        ReadOnlyDictionary<string, ReadOnlyDictionary<string, object>> VariablesMetadata { get; }
        ReadOnlyDictionary<string, string[]> VariablesDimensions { get; }
        ReadOnlyDictionary<string, Type> VariablesTypes {get;}
        ReadOnlyDictionary<string, int> DimensionsLengths {get;}
    }

    public interface IDataStorage
    {
        Task<Array> GetDataAsync(string variableName, int[] origin = null, int[] stride = null, int[] shape = null);     
    }

    [Obsolete("Too many responsibilities. Get rid of context. Use interface segregation")]
    public interface IStorageContext : IDataStorage
    {
        IDataStorageDefinition StorageDefinition { get; }

        Task<IStorageResponse[]> GetDataAsync(params IStorageRequest[] requests);

           
    }    
}

