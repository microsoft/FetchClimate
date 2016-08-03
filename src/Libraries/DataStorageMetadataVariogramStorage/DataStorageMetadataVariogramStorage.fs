module StorageContextVariogramStorage

open VariogramModule

type IDataStorage = Microsoft.Research.Science.FetchClimate2.IDataStorageDefinition

//anisotropic_id is intended to identify (distinct) different variograms among a set of stored variograms (useful for storing anisotropic variograms)
type StorageContextMetadataStorage(context:IDataStorage,var_name:string,anisotropic_id:string) =    
    let m = context.VariablesMetadata.[var_name]
    let nugget_key = sprintf "%s_variogram_nugget" anisotropic_id
    let range_key = sprintf "%s_variogram_range" anisotropic_id
    let sill_key = sprintf "%s_variogram_sill" anisotropic_id
    let family_key = sprintf "%s_variogram_family" anisotropic_id
    interface IVariogramStorage with
        member s.Dematerialize (var_description:IVariogramDescription) =            
            failwith "Not supported. The data storage metadata is readonly"
        member s.Materialize() =            
            if m.ContainsKey(family_key) && m.ContainsKey(sill_key) && m.ContainsKey(range_key) && m.ContainsKey(nugget_key) then                                
                let fix_model_func model =
                    let family = string(m.[family_key])                    
                    let valid_range,range = System.Double.TryParse(string(m.[range_key]),System.Globalization.NumberStyles.Any,System.Globalization.CultureInfo.InvariantCulture)
                    let valid_nugget,nugget = System.Double.TryParse(string(m.[nugget_key]),System.Globalization.NumberStyles.Any,System.Globalization.CultureInfo.InvariantCulture)
                    let valid_sill,sill = System.Double.TryParse(string(m.[sill_key]),System.Globalization.NumberStyles.Any,System.Globalization.CultureInfo.InvariantCulture)
                    if (valid_range && valid_nugget && valid_sill)
                        then Some(Variogram(model,family,nugget,range,sill) :> IVariogram)
                    else
                        None
                
                match string(m.[family_key]) with
                    |   "Gaussian" -> fix_model_func VariogramModels.gaussian
                    |   "Exponential" -> fix_model_func VariogramModels.exponential
                    |   "Spherical" -> fix_model_func VariogramModels.spherical
                    |   _   -> None
                else
                    None