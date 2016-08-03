module VariogramModule
    type IEmpericalVariogram =        
        abstract Distances: float[]
        abstract GammaValues: float[]
    
    type SerializtionInfo = System.Collections.Generic.Dictionary<string,string>

    type IVariogramParameters =
        abstract Sill: float
        abstract Nugget: float
        abstract Range: float

    type IVariogram =
        inherit IVariogramParameters
        abstract GetGamma: float -> float
        

    type IVariogramDescription =
        inherit IVariogramParameters
        abstract Family: string        
    
    type IDescribedVariogram =
        inherit IVariogram
        inherit IVariogramDescription
      
    type IVariogramFitter =
        //if the fitting does not converge, the "fallback" approximate variogram that fits into the data range
        abstract GetFallback: IEmpericalVariogram -> IDescribedVariogram
        abstract Fit: IEmpericalVariogram -> IDescribedVariogram option

    type IVariogramStorage =
        abstract Dematerialize: IVariogramDescription -> unit
        abstract Materialize: unit -> IVariogram option

    type Variogram(model,family,nugget,range,sill) =
        member s.Family = family
        interface IDescribedVariogram with
            member s.Family = family
            member s.GetGamma dist = model dist nugget range sill   
            member s.Sill = sill
            member s.Range = range
            member s.Nugget = nugget 