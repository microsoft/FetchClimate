module NetNumericsVariogramFitter

type IVariogram = VariogramModule.IVariogram
type IVariogramFitter = VariogramModule.IVariogramFitter
type IEmpericalVariogram = VariogramModule.IEmpericalVariogram
type IList<'a> = System.Collections.Generic.IList<'a>

let private gaussian dist nugget range sill = (nugget-sill)*exp(-(dist*dist)/(range*range))+nugget

type private GaussianVariogram(nugget,range,sill) =
    interface IVariogram with
        member s.GetGamma dist = gaussian dist nugget range sill   

 type Fitter =
    interface IVariogramFitter with
        member s.Fit (empiricalVariogram:IEmpericalVariogram) =
            let optimizer = ShoNS.Optimizer.QuasiNewton()
            let ssq (parameters:float array) = 
                let nugget,range,sill = parameters.[0],parameters.[1],parameters.[2]
                let diff dist gamma = gamma - gaussian dist nugget range sill
                let pairs = Array.zip empiricalVariogram.Distances empiricalVariogram.GammaValues
                Seq.map (fun (d,g) -> diff d g) pairs |> Seq.sum            
            let toOpt (parameters:IList<float>) (gradients:IList<float>)=
                Array.ofSeq parameters |> ssq                
            let f = ShoNS.Optimizer.DiffFunc(toOpt)
            let starting = [| 0.0; empiricalVariogram.Distances.[empiricalVariogram.Distances.Length-1] ;Array.max empiricalVariogram.GammaValues |]
            let res = optimizer.MinimizeDetail(f,starting)
            System.Diagnostics.Trace.WriteLine(sprintf "optimized gaussian variagram n:%g r:%g s:%g ssq:%g evals:%d" res.solution.[0] res.solution.[1] res.solution.[2] res.funcValue res.EvaluationCallCount)
            GaussianVariogram(res.solution.[0],res.solution.[1],res.solution.[2]) :> IVariogram
