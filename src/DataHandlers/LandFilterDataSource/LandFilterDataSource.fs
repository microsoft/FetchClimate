namespace Microsoft.Research.Science.FetchClimate2.DataHandlers

type Gauss = float * float

type LandOnlyFilterDataSource() = 
    inherit VirtualDataSource()
    
    static member landOnlyVar (variable:Gauss) (elevation:Gauss) =
        let (varMu,varSigma) = variable;
        let (elevationMu,elevationSigma) = elevation;
        if(System.Double.IsNaN(varMu) || System.Double.IsNaN(varSigma) || System.Double.IsNaN(elevationMu) || System.Double.IsNaN(elevationSigma)) then
            (nan,nan)
        else if(elevationMu>=0.0) then
            (varMu,varSigma)
        else
            (nan,varSigma)

type OceanOnlyFilterDataSource() = 
    inherit VirtualDataSource()
    
    static member landOnlyVar (variable:Gauss) (elevation:Gauss) =
        let (varMu,varSigma) = variable;
        let (elevationMu,elevationSigma) = elevation;
        if(System.Double.IsNaN(varMu) || System.Double.IsNaN(varSigma) || System.Double.IsNaN(elevationMu) || System.Double.IsNaN(elevationSigma)) then
            (nan,nan)
        else if(elevationMu<=0.0) then
            (varMu,varSigma)
        else
            (nan,varSigma)
