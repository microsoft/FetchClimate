module VariogramStorage

open Microsoft.Research.Science.Data
open Microsoft.Research.Science.Data.Imperative
open VariogramModule


//anisotropic_id is intended to identify (distinct) different variograms among a set of stored variograms (useful for storing anisotropic variograms)
type DataSetMetadataStorage(dataset:DataSet,var_name:string,anisotropic_id:string) =
    let time = System.DateTime.UtcNow;
    let m = dataset.Variables.[var_name].Metadata
    let nugget_key = sprintf "%s_variogram_nugget" anisotropic_id
    let range_key = sprintf "%s_variogram_range" anisotropic_id
    let sill_key = sprintf "%s_variogram_sill" anisotropic_id
    let family_key = sprintf "%s_variogram_family" anisotropic_id
    let timestamp_key = sprintf "%s_variogram_createtime" anisotropic_id
    interface IVariogramStorage with
        member s.Dematerialize (var_description:IVariogramDescription) =            
            m.[nugget_key] <- var_description.Nugget.ToString(System.Globalization.CultureInfo.InvariantCulture)
            m.[sill_key] <- var_description.Sill.ToString(System.Globalization.CultureInfo.InvariantCulture)
            m.[range_key] <- var_description.Range.ToString(System.Globalization.CultureInfo.InvariantCulture)
            m.[family_key] <- var_description.Family.ToString(System.Globalization.CultureInfo.InvariantCulture)
            m.[timestamp_key] <- time.ToString(System.Globalization.CultureInfo.InvariantCulture)
        member s.Materialize() =            
            if m.ContainsKey(family_key) && m.ContainsKey(sill_key) && m.ContainsKey(range_key) && m.ContainsKey(nugget_key) then                                
                let fix_model_func model =
                    let family = string(m.[family_key])
                    let range,sill,nugget = ref 0.0, ref 0.0, ref 0.0
                    let valid_range = System.Double.TryParse(string(m.[range_key]),System.Globalization.NumberStyles.Any,System.Globalization.CultureInfo.InvariantCulture, range)
                    let valid_nugget = System.Double.TryParse(string(m.[nugget_key]),System.Globalization.NumberStyles.Any,System.Globalization.CultureInfo.InvariantCulture, nugget)
                    let valid_sill = System.Double.TryParse(string(m.[sill_key]),System.Globalization.NumberStyles.Any,System.Globalization.CultureInfo.InvariantCulture, sill)
                    if (valid_range && valid_nugget && valid_sill)
                        then Some(Variogram(model,family,!nugget,!range,!sill) :> IVariogram)
                    else
                        None
                
                match string(m.[family_key]) with
                    |   "Gaussian" -> fix_model_func VariogramModels.gaussian
                    |   "Exponential" -> fix_model_func VariogramModels.exponential
                    |   "Spherical" -> fix_model_func VariogramModels.spherical
                    |   _   -> None
                else
                    None