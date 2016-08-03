module LMDotNetVariogramFitter

open LMDotNet

open StatUtilsTracing
open VariogramModule

type private Parameters =
    {
        Nugget: float
        Sill: float
        Range:float
    }
[<NoEquality>][<NoComparison>]
type private ModelFitCompResult =
        {
            Name: string
            Family: string
            Model: float -> float -> float -> float -> float
            FittingResult: OptimizationResult
            ParametersExtractor: OptimizationResult -> Parameters
        }

type Fitter() =    
    let rangeExtractor nugget sill (optimizationResult:OptimizationResult) = 
        { Nugget=nugget; Sill=sill; Range=optimizationResult.OptimizedParameters.[0] }
    let rangeSillExtractor nugget (optimizationResult:OptimizationResult) = 
        { Nugget=nugget; Range=optimizationResult.OptimizedParameters.[0]; Sill=optimizationResult.OptimizedParameters.[1]}
    let nuggetRangeSillExtractor (optimizationResult:OptimizationResult) = 
        { Nugget=optimizationResult.OptimizedParameters.[0]; Range=optimizationResult.OptimizedParameters.[1]; Sill=optimizationResult.OptimizedParameters.[2]}

    let fit model start_values (gammas:float[]) distances =
        let solverSettings = { LMA.defaultSettings with patience=1000} 
        let findMin = solverSettings |> LMA.init |> LMA.minimize                
        assert (not (Array.exists (fun d -> d < 0.0) distances)) //distances must be non-negative

        let samplesCount = distances.Length

        let computeDiffsSquared (p:float[]) (r:float[]) = 
            for i in 0..samplesCount-1 do
                r.[i] <-  gammas.[i] - (model p distances.[i])                        
        
        let fit = computeDiffsSquared |> findMin start_values samplesCount                

        fit    

    static do
        //extracting native dlls
        let extractEmbededFile reourceName outDir outfileName=
            let currentAssembly = System.Reflection.Assembly.GetExecutingAssembly()            
            use streamIN = currentAssembly.GetManifestResourceStream(reourceName)
            use streamOUT = new System.IO.FileStream((sprintf "%s\%s" outDir outfileName),System.IO.FileMode.Create)
            streamIN.CopyTo(streamOUT)        
        if not (System.IO.File.Exists "lmfit64\lmfit.dll") then            
            if not (System.IO.Directory.Exists "lmfit64") then System.IO.Directory.CreateDirectory("lmfit64") |> ignore                    
            extractEmbededFile "lmfit64.dll" "lmfit64" "lmfit.dll"                
        if not (System.IO.File.Exists "lmfit32\lmfit.dll") then            
            if not (System.IO.Directory.Exists "lmfit32") then System.IO.Directory.CreateDirectory("lmfit32") |> ignore                    
            extractEmbededFile "lmfit32.dll" "lmfit32" "lmfit.dll"             

    interface IVariogramFitter with        
        member s.Fit (empiricalVariogram:IEmpericalVariogram) =
            let adopt_model model (p:float array) dist= 
                model dist p.[0] p.[1] p.[2]
            let adopt_model_fix_nugget nugget model (p:float array) dist= 
                model dist nugget p.[0] p.[1]
            let adopt_model_fix_nugget_sill nugget sill model (p:float array) dist= 
                model dist nugget p.[0] sill
            
            let distances = empiricalVariogram.Distances
            let gammas = empiricalVariogram.GammaValues

            (sprintf "Optimizer: samples count %d" distances.Length) |> trace_verbose

            let distances_mean = Array.average distances
            let gamma_mean = Array.average gammas

            let min_gamma = Array.min gammas
            let max_gamma = Array.max gammas
            let max_distance = Array.max distances

            //nugget, range, sill            
            let start_values = [| 0.0; distances_mean; max_gamma |]
            let start_values_n0 = [|distances_mean; max_gamma|]
            let start_values_range_only = [| max_gamma |]

            (sprintf "Optimizer initial guess n:%g r:%g s:%g" start_values.[0] start_values.[1] start_values.[2]) |> trace_verbose

            let fit_comp name family model start_values adapter extractor = 
                async {return {Name=name; Family=family; Model=model; FittingResult=fit (adapter model) start_values gammas distances; ParametersExtractor=extractor} }

            let gaussian_comp = fit_comp "Gaussian" "Gaussian" VariogramModels.gaussian start_values adopt_model nuggetRangeSillExtractor          
            let exponential_comp =  fit_comp "Exponential"  "Exponential" VariogramModels.exponential start_values adopt_model nuggetRangeSillExtractor
            let spherical_comp = fit_comp "Spherical" "Spherical" VariogramModels.spherical start_values adopt_model nuggetRangeSillExtractor
            let exponential_n0_comp = fit_comp "Exponential_n0" "Exponential" VariogramModels.exponential start_values_n0 (adopt_model_fix_nugget 0.0) (rangeSillExtractor 0.0)
            let spherical_n0_comp = fit_comp "Spherical_n0" "Spherical" VariogramModels.spherical start_values_n0 (adopt_model_fix_nugget 0.0) (rangeSillExtractor 0.0)
            let gaussian_n_s_comp = fit_comp "Gaussian range only" "Gaussian" VariogramModels.gaussian start_values_range_only (adopt_model_fix_nugget_sill min_gamma max_gamma) (rangeExtractor min_gamma max_gamma)
            let exponential_n_s_comp =  fit_comp "Exponential range only" "Exponential" VariogramModels.exponential start_values_range_only (adopt_model_fix_nugget_sill min_gamma max_gamma) (rangeExtractor min_gamma max_gamma)
            let spherical_n_s_comp = fit_comp "Spherical range only" "Spherical" VariogramModels.spherical start_values_range_only (adopt_model_fix_nugget_sill min_gamma max_gamma) (rangeExtractor min_gamma max_gamma)
            //TODO: Add boosting of these regressions as a final one

            //Add additional models here
            let results = Async.Parallel [| gaussian_comp; exponential_comp; spherical_comp; exponential_n0_comp; spherical_n0_comp; gaussian_n_s_comp; exponential_n_s_comp; spherical_n_s_comp |] |> Async.RunSynchronously

            let print_result (result:ModelFitCompResult) =
                let name = result.Name
                let res = result.FittingResult
                let p = result.ParametersExtractor res
                (sprintf "Optimizer result for %s model status:%O error:%g n:%g r:%g s:%g iterations:%d" name res.Outcome res.ErrorNorm p.Nugget p.Range p.Sill res.Iterations) |> trace_verbose
            Array.iter print_result results            

            //filtering functions
            let is_converged (result:ModelFitCompResult) =
                let fit = result.FittingResult
                match fit.Outcome with
                |    LMDotNet.SolverStatus.ConvergedBoth |  LMDotNet.SolverStatus.ConvergedParam |  LMDotNet.SolverStatus.ConvergedSumSq -> true 
                |    LMDotNet.SolverStatus.Underflow when fit.ErrorNorm = 0.0 -> true //CHECK: can we accept underflow
                |    _  -> false

            let is_in_range (result:ModelFitCompResult) =
                let name = result.Name
                let p = result.ParametersExtractor result.FittingResult
                //WARNING! AD-hoc control of odd function function!
                let range = if name.Contains("Gaussian") then abs(p.Range) else p.Range //as range is squared in Gaussian
                let valid_nugget = p.Nugget >= 0.0
                let valid_range =  range >= 0.0 && range <= max_distance * 1.5
                let valid_sill = p.Sill <= max_gamma * 1.5
                valid_nugget && valid_range && valid_sill && (p.Sill>=p.Nugget)

            let error_norm (result:ModelFitCompResult) =
                result.FittingResult.ErrorNorm

            let filtered = results |> Seq.filter is_converged |> Seq.filter is_in_range |> Array.ofSeq
            Array.sortInPlaceBy error_norm filtered

            let variogram = 
                if Array.isEmpty filtered then
                    (sprintf "WARNING: Optimizer failed to fit any available models.") |> trace_warn
                    None                    
                else
                    let best = filtered.[0]                    
                    let p = best.ParametersExtractor best.FittingResult
                    (sprintf "Optimizer has chosen %s model with minimum err_vect_norm and pars n:%g r:%g s:%g" best.Name p.Nugget p.Range p.Sill) |> trace_info
                    Some(Variogram(best.Model,best.Family,p.Nugget,p.Range,p.Sill) :> IDescribedVariogram)
            variogram 
        member s.GetFallback (empiricalVariogram:IEmpericalVariogram) =
            let gammas = empiricalVariogram.GammaValues
            let distances = empiricalVariogram.Distances
            let max_gamma = Array.max gammas
            let max_distance = Array.max distances
            let nugget,range,sill = Array.min gammas, max_distance, max_gamma
            (sprintf "Returning Gaussian model with fallback pars n:%g r:%g s:%g instead" nugget range sill) |> trace_warn
            Variogram(VariogramModels.gaussian,"Gaussian",nugget,range,sill) :> IDescribedVariogram

                



            