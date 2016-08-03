///Imported from Angara project
module Angara

//
// Statistical library
//

type Distribution = 
  | Bernoulli of float // fraction of success [1e-16 to 1.0-1e-16] -> success/failure outcome, 1 or 0
  | Binomial of int*float // number of trials, probability of success -> number of successes, [0 to max_int]
  | NegativeBinomial of float*float // mean (0 to inf), number of failures or 'shape' for fractional values (0 to inf) -> number of successes, [0 to max_int]
  | Poisson of float // mean a.k.a. lambda [0, maxint] -> number of events [0 to maxint]
  | Normal of float*float // mean, standard deviation -> continuous (-infinity to infinity)
  | Gamma of float*float // alpha (>0), beta (>0) -> continuous (0 to infinity)
  | Exponential of float // rate lambda (>0) -> continuaous [0 to infinity)
  | LogNormal of float*float // log mean, standard deviation of logarithm -> continuous (0.0 to infinity)
  | Uniform of float*float // lower bound, upper bound -> continuous [lower bound to upper bound)
  | Mixture of (float*Distribution) list

let improbable = System.Double.Epsilon // 5e-324
let log_improbable = log(improbable) // -745
let rec private found_tolerance x = let x2=0.5*x in if 1.0=1.0-x2 then x else (found_tolerance x2)
let tolerance = found_tolerance 1.0 // 1e-16
let log_tolerance = log tolerance // -36.7
let maxint = 1.0/tolerance; // 9e15 -- 6 orders of magnitude alrger than int.maxvalue

let pi = 3.14159265358979323846264338327950288 // π
let pi2 = 6.283185307179586476925286 // 2π
let e = 2.71828182845904523536028747135266250 // natural logarithm base

let sqrt2pi = sqrt(pi2)
let log2pi = 0.5*log(pi2)

let private isNan = System.Double.IsNaN
let private isInf = System.Double.IsInfinity

type summaryType = 
    {count:int; min:float; max:float; mean:float; variance:float}
    override me.ToString() = sprintf "%A" me

/// Produces cumulant summary of the data using fast one-pass algorithm.
let summary data =
    let folder summary d =
        if isNan(d) || isInf(d) then 
            summary 
        else
            let delta = d - summary.mean
            let n = summary.count + 1
            let mean = summary.mean + delta/float n
            {
                count = n
                min = (min d summary.min)
                max = (max d summary.max)
                mean = mean
                variance = summary.variance + delta*(d-mean)
            }
    let pass =
        Seq.fold folder {
                            count=0
                            min=System.Double.PositiveInfinity
                            max=System.Double.NegativeInfinity
                            mean=0.0
                            variance=0.0
                            } data
    if pass.count<2 then
        pass
    else
        let pass = {pass with variance=pass.variance/(float(pass.count-1))}
        pass

type qsummaryType = 
    {min:float; lb95:float; lb68:float; median:float; ub68:float; ub95:float; max:float}
    override me.ToString() = sprintf "%A" me

/// Produces quantile summary of the data.
let qsummary data =
    let a = data |> Seq.filter(fun d -> not (System.Double.IsNaN(d) || System.Double.IsInfinity(d))) |> Seq.toArray
    Array.sortInPlace a
    let n = a.Length
    if n<1 then {min=nan; lb95=nan; lb68=nan; median=nan; ub68=nan; ub95=nan; max=nan}
    else
        let q p =
            let h = p*(float n + 1./3.)-2./3.
            if h <= 0.0 then a.[0]
            elif h >= float (n-1) then a.[n-1]
            else
                let fh = floor h
                a.[int fh]*(1.0-h+fh) + a.[int fh + 1]*(h - fh)
        {min=a.[0]; lb95=q(0.025); lb68=q(0.16); median=q(0.5); ub68=q(0.84); ub95=q(0.975); max=a.[n-1]}

// adopted from Numerical Recipes: The Art of Scientific Computing, Third Edition (2007), p.257
let private log_gamma x =
    let cof=[|57.1562356658629235; -59.5979603554754912; 14.1360979747417471; -0.491913816097620199; 0.339946499848118887e-4; 0.465236289270485756e-4; -0.983744753048795646e-4; 0.158088703224912494e-3; -0.210264441724104883e-3; 0.217439618115212643e-3; -0.164318106536763890e-3; 0.844182239838527433e-4; -0.261908384015814087e-4; 0.368991826595316234e-5|]
    if x<=0.0 then nan else
        let t = x + 5.24218750000000000 // Rational 671/128.
        let t = (x+0.5)*log(t)-t
        let ser,_ = cof |> Seq.fold (fun (ser,x) c -> let y=x+1.0 in ser+c/y,y) (0.999999999999997092,x)
        t+log(2.5066282746310005*ser/x)

let sfe = Seq.unfold (fun (n, lognf) -> 
    if n=0 then 
        Some(0.0, (1,0.0))
    elif n<16 then
        let logn = log(float n)
        Some(lognf+float(n)-log2pi-(float(n)-0.5)*logn, (n+1, lognf+logn)) 
    else None) (0, 0.0) |> Array.ofSeq


let private stirlerr n =
    if (n<16) then sfe.[n]
    else
        let S0 = 1.0/12.0
        let S1 = 1.0/360.0
        let S2 = 1.0/1260.0
        let S3 = 1.0/1680.0
        let S4 = 1.0/1188.0
        let n1 = 1.0/float(n)
        let n2 = n1*n1
        if (n>500) then ((S0-S1*n2)*n1)
        elif (n>80) then ((S0-(S1-S2*n2)*n2)*n1)
        elif (n>35) then ((S0-(S1-(S2-S3*n2)*n2)*n2)*n1)
        else ((S0-(S1-(S2-(S3-S4*n2)*n2)*n2)*n2)*n1)

let private bd0 (x: float, np: float) =
    if (abs(x-np) < 0.1*(x+np)) then
        let v = (x-np)/(x+np)
        let rec next j ej s =
            let s1 = s + ej/float(2*j+1)
            if s1=s then s1
            else next (j+1) (ej*v*v) s1
        next 1 (2.0*x*v*v*v) ((x-np)*v)
    else x*log(x/np)+np-x

let private dbinom (x: int, n: int, p: float) =
    assert((p>=0.0) && (p<=1.0))
    assert(n>=0)
    assert((x>=0) && (x<=n))
    if (p=0.0) then if x=0 then 1.0 else 0.0
    elif (p=1.0) then if x=n then 1.0 else 0.0
    elif (x=0) then exp(float n*log(1.0-p))
    elif (x=n) then exp(float n*log(p))
    else
        let lc = stirlerr(n) - stirlerr(x) - stirlerr(n-x) - bd0(float x, float(n)*p) - bd0(float(n-x), float(n)*(1.0-p))
        exp(lc)*sqrt(float(n)/(pi2*float(x)*float(n-x)));

let private dpois(x: int, lb: float) =
    if (lb=0.0) then if x=0 then 1.0 else 0.0
    elif (x=0) then exp(-lb)
    else exp(-stirlerr(x)-bd0(float x,lb))/sqrt(pi2*float(x));

let rec log_pdf d v = // log-probability distribution function
    if System.Double.IsNaN(v) then log_improbable
    else
        let result =
            match d with
            | Normal(mean,stdev) -> let dev = (mean-v)/stdev in -0.5*dev*dev - log(sqrt2pi*stdev)
            | LogNormal(mean,stdev) -> let dev = (log mean-log v)/stdev in -0.5*dev*dev - log(sqrt2pi*stdev*v)
            | Uniform(lb,ub) ->
                if (v<lb || v>ub || lb>=ub) then log_improbable
                else -log(ub-lb)
            | Exponential(lambda) -> 
                if lambda<=0.0 then log_improbable else
                    log(lambda) - lambda*v
            | Gamma(a,b) ->
                a*log(b) - log_gamma(a) + (a-1.0)*log(v) - b*v
            | Bernoulli(fraction) -> 
                if (fraction<tolerance || fraction>1.0-tolerance) then log_improbable
                elif v>0.5 then log(fraction) 
                else log(1.0-fraction)
            | Binomial(n, p) ->
                // log(dbinom(int v, int n, p))
                if (p<0.0) || (p>1.0) || (n<0) || (v<0.0) || (v>float n) then log_improbable
                else log(dbinom(int v, int n, p))
            | NegativeBinomial(mean, r) ->
                if mean<=0.0 || r<=0.0 || v<0.0 || v>maxint then log_improbable else
                    let k = round v
                    r*log(r/(mean+r))+k*log(mean/(mean+r))+log_gamma(r+k)-log_gamma(k+1.0)-log_gamma(r)
            | Poisson(lambda) ->
                if (lambda<0.0 || lambda>float System.Int32.MaxValue) then log_improbable
                // log(dpois(int v, lambda))
                elif (lambda=0.0) then if v=0.0 then 0.0 else log_improbable
                elif (v=0.0) then -lambda
                else let x = int v in - stirlerr(x) - bd0(float x,lambda) - 0.5*log(pi2*float(x));
            | Mixture(components) -> log(components |> List.fold (fun s (w,d) -> s+w*exp(log_pdf d v)) 0.0)
            //| _ -> raise (System.NotImplementedException())
        if System.Double.IsNaN(result) || System.Double.IsInfinity(result) then 
            log_improbable
        else
            result
let ``shared rng`` = System.Random()
let private rand_NextDouble(rng: System.Random) =
    try
        System.Threading.Monitor.Enter(``shared rng``)
        ``shared rng``.NextDouble()
    finally
        System.Threading.Monitor.Exit(``shared rng``)
let mutable rnorm_phase = false
let mutable rnorm_2 = 0.0
let mutable rnorm_f = 0.0
let private rnorm (mean,stdev,rng) =
    let z =
        if rnorm_phase then
            rnorm_phase <- false
            rnorm_2*rnorm_f
        else
            rnorm_phase <- true
            let mutable rnorm_1 = 0.0
            let mutable s = 1.0
            while (s>=1.0) do
                rnorm_1 <- rand_NextDouble(rng)*2.0-1.0
                rnorm_2 <- rand_NextDouble(rng)*2.0-1.0
                s <- rnorm_1*rnorm_1 + rnorm_2*rnorm_2
            rnorm_f <- sqrt(-2.0*log(s)/s)
            rnorm_1*rnorm_f
    z*stdev+mean

let rec rng d rng = // random number generator
    match d with
    | Normal(mean,stdev) -> rnorm(mean,stdev,rng)
    | _ ->failwith "not implemented"
    
//let inline (+)   (x:float)    (y:float)    = (# "add" x y : float #)
//let inline (-)   (x:float)    (y:float)    = (# "sub" x y : float #)
//let inline ( * )   (x:float)    (y:float)    = (# "mul" x y : float #)
//let inline (/)   (x:float)    (y:float)    = (# "div" x y : float #)

// Computes Pearson's correlation coefficient for two float arrays
// The Pearson correlation is defined only if both of the standard deviations are finite and both of them are nonzero.
// Returns NaN, otherwise.
let correlation (x:float[]) (y:float[]) =
    if x.Length <> y.Length then failwith "Different lengths of arrays"            
    let filtered = Seq.zip x y |> Seq.filter (fun(u,v) -> not (isNan(u) || isNan(v) || isInf(u) || isInf(v))) |> Array.ofSeq;
    let n = filtered.Length 
    if n <= 1 then System.Double.NaN else
    let _x, _y = Array.map fst filtered, Array.map snd filtered
    let sx, sy = summary _x, summary _y
    let stdx, stdy = sqrt sx.variance, sqrt sy.variance
    if stdx = 0.0 || stdy = 0.0 || isInf(stdx) || isInf(stdy) then System.Double.NaN else
    let d1 = float(n) * sx.mean * sy.mean
    let d2 = float(n-1) * stdx * stdy
    ((filtered |> Array.map (fun(s,t)->s*t) |> Array.sum) - d1)/d2

[<System.Reflection.AssemblyVersion("1.1.0.0")>] do ()