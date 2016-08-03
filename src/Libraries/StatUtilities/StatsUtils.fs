module StatsUtils

//adopted from Angara.ComputeRuntime
let private isNan = System.Double.IsNaN
let private isInf = System.Double.IsInfinity

type summaryType = {count:int; min:float; max:float; mean:float; variance:float}
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


type cosummaryType = {count:int; corr:float; diffVariance:float; cov: float}

/// produces comulant statistics for the pair of data series
let cosummary series1 series2 =
    let mutable xy_sum = 0.0
    let mutable x_sum = 0.0
    let mutable y_sum = 0.0
    let mutable xx_sum = 0.0
    let mutable yy_sum = 0.0
    let mutable n = 0
    let zipped = Seq.zip series1 series2
    if (Seq.truncate 2 zipped) |> Seq.isEmpty
    then
        {
        count = 0;
        corr = nan;
        diffVariance = nan;
        cov=nan
        }
    else
        for (x,y) in zipped do
            x_sum <- x_sum + x
            y_sum <- y_sum + y
            xy_sum <- xy_sum + x*y
            xx_sum <- xx_sum + x*x
            yy_sum <- yy_sum + y*y
            n <- n + 1
        let r = let n = float(n) in (n*xy_sum-x_sum*y_sum)/((sqrt(n*xx_sum-x_sum*x_sum))*(sqrt(n*yy_sum-y_sum*y_sum)))
        let cov = let n = float(n) in xy_sum/n-(x_sum*y_sum)/(n*n)
        let diffSumm = Seq.map (fun (x,y) -> x-y) zipped |> summary
        {
            count=n;
            corr=r;
            diffVariance=diffSumm.variance;
            cov=cov
        }
