using Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    /// <summary>
    /// Loads a time-dimension variograms from the metadata stored info for all variables in the supplied dataset
    /// </summary>
    public class StorageContextMetadataTimeVarianceExtractor : IGaussianProcessDescriptionFactory
    {
        readonly Dictionary<string, IGaussianProcessDescription> processes = new Dictionary<string, IGaussianProcessDescription>();


        

        public StorageContextMetadataTimeVarianceExtractor(IDataStorageDefinition storageDefinition, double axisPeriod = double.NaN)
        {
            var dict = storageDefinition.VariablesMetadata;
            var dimDict = storageDefinition.VariablesDimensions;

            string timeDimName = TimeAxisAutodetection.GetTimeDimension(storageDefinition);
            
            foreach (var variable in dict.Keys)
            {
                //checking for temporal variogram. Storing it if found
                if (timeDimName != null)
                {
                    int idx = Array.IndexOf(dimDict[variable], timeDimName);
                    if (idx >= 0)
                    {
                        var temporalVariogramStorage = new StorageContextVariogramStorage.StorageContextMetadataStorage(storageDefinition, variable, idx.ToString()) as VariogramModule.IVariogramStorage;
                        var materializationResult = temporalVariogramStorage.Materialize();
                        if (FSharp.Core.FSharpOption<VariogramModule.IVariogram>.get_IsSome(materializationResult))
                            processes.Add(variable, new GaussianProcessDescription(materializationResult.Value, axisPeriod));
                    }
                }

            }
        }

        class GaussianProcessDescription : IGaussianProcessDescription
        {
            private readonly VariogramModule.IVariogram variogram;
            private readonly double axisPeriod;

            public GaussianProcessDescription(VariogramModule.IVariogram variogram, double axisPeriod)
            {
                this.variogram = variogram;
                this.axisPeriod = axisPeriod;
            }

            public double Dist(double location1, double location2)
            {
                if (double.IsNaN(axisPeriod))
                    return Math.Abs(location1 - location2);
                else
                    return Math.Min((location1 - location2 + axisPeriod) % axisPeriod, (location2 - location1 + axisPeriod) % axisPeriod);
            }

            public VariogramModule.IVariogram Variogram
            {
                get { return variogram; }
            }
        }

        public IGaussianProcessDescription Create(string varName)
        {
            IGaussianProcessDescription result = null;
            processes.TryGetValue(varName, out result);
            return result;
        }
    }

    /// <summary>
    /// Loads a spatial variograms from the metadata stored info for all variables in the supplied dataset
    /// </summary>
    public class StorageContextMetadataSpatialVarianceExtractor : IGaussianFieldDescriptionFactory
    {
        private const string spatialKey = "spatial";

        readonly Dictionary<string, IGaussianFieldDescription> fields = new Dictionary<string, IGaussianFieldDescription>();


        public StorageContextMetadataSpatialVarianceExtractor(IDataStorageDefinition storageDefinition)
        {
            var dict = storageDefinition.VariablesMetadata;
            var dimDict = storageDefinition.VariablesDimensions;
            foreach (var variable in dict.Keys)
            {
                //checking for spatial variogram. Storing it if found
                var spatialVariogramStorage = new StorageContextVariogramStorage.StorageContextMetadataStorage(storageDefinition, variable, spatialKey) as VariogramModule.IVariogramStorage;
                var materializationResult = spatialVariogramStorage.Materialize();
                if (FSharp.Core.FSharpOption<VariogramModule.IVariogram>.get_IsSome(materializationResult))
                    fields.Add(variable, new GaussianFieldDescrption(materializationResult.Value));
            }
        }

        class GaussianFieldDescrption : IGaussianFieldDescription
        {
            private readonly VariogramModule.IVariogram variogram;

            public GaussianFieldDescrption(VariogramModule.IVariogram variogram)
            {
                this.variogram = variogram;
            }

            public double Dist(double lat1, double lon1, double lat2, double lon2)
            {
                return SphereMath.GetDistance(lat1, lon1, lat2, lon2);
            }

            public VariogramModule.IVariogram Variogram
            {
                get { return variogram; }
            }
        }

        public IGaussianFieldDescription Create(string varName)
        {
            IGaussianFieldDescription result = null;
            fields.TryGetValue(varName, out result);
            return result;
        }
    }
}
