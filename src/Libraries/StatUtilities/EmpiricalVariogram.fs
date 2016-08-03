module EmpiricalVariogram

    open VariogramModule
    
    type Bin = {
        bounds:float*float // [..) left is inclusive, right is exclusive
        count:int
        sum_of_squares:float
        }       

    type ArrayBased_EmpVar(data) =
        member s.data = data : Bin array
        interface IEmpericalVariogram with
            member s.GammaValues = Array.map (fun d -> d.sum_of_squares/float(d.count)) s.data
            member s.Distances = Array.map (fun b -> ((fst b.bounds)+(snd b.bounds))*0.5) s.data           