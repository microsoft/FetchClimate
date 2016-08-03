namespace FC1LegacySupport

open Microsoft.Research.Science.FetchClimate2.DataHandlers

type Gauss = float * float

type LegacyVariables() = 
    inherit VirtualDataSource()

    static member bathymetry (elev:Gauss) =
        match elev with
        | (mean, _) when mean > 0.0 -> (nan, nan)
        | (mean, sd) -> (-mean, sd)

    static member oceanTemp (airTemp:Gauss) (elev:Gauss) =
        match airTemp, elev with
        | _, (elevMean, _) when System.Double.IsNaN(elevMean) || elevMean > 0.0 -> (nan, nan)
        | temp, _ -> temp

    static member landTemp (airTemp:Gauss) (elev:Gauss) =
        match airTemp, elev with
        | _, (elevMean, _) when System.Double.IsNaN(elevMean) || elevMean < 0.0 -> (nan, nan)
        | temp, _ -> temp

    static member landRelhum (relhum:Gauss) (elev:Gauss) =
        match relhum, elev with
        | _, (elevMean, _) when System.Double.IsNaN(elevMean) || elevMean < 0.0 -> (nan, nan)
        | hum, _ -> hum