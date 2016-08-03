module EmpVariogramBuilder
    
    open VariogramModule
    open EmpiricalVariogram

    let private is_nan x = System.Double.IsNaN x       

    type PointSet =
        {
        Lats: float[]
        Lons: float[]
        Values: float[]
        }

    type EmpiricalVariogramBuilder() =        
        static let pairsCount = 1000000;
            
        static member BuildEmpiricalVariogram (points:PointSet) dist : IEmpericalVariogram =
            let pairs = EmpiricalVariogramBuilder.produce_constrained_pairs pairsCount points dist
            //let pairs = EmpiricalVariogramBuilder.produce_pairs points dist
            let vario = EmpiricalVariogramBuilder.build_emp_var pairs (fun x -> x*0.0025) (fun x -> x*400.0) //scaling 20000km(max possible) to 50, thus ~50 bins
            vario

        static member private produce_pairs (points:PointSet) (dist: float*float -> float*float -> float) =
            let lats,lons,vals = points.Lats,points.Lons,points.Values
            assert(lats.Length = lons.Length && lons.Length=vals.Length)
            let N = lats.Length
            seq {
                for i in 1..N-1 do
                    for j in 0..i-1 do
                        yield vals.[i],vals.[j],(dist (lats.[i],lons.[i]) (lats.[j],lons.[j]))
                        }

        static member private produce_constrained_pairs count (points:PointSet) (dist: float*float -> float*float -> float) =
            let lats,lons,vals = points.Lats,points.Lons,points.Values
            let N = lats.Length
            let r = System.Random()
            let total = float(N)*float(N)*0.5
            let count_f = float(count)
            let prob = if count_f>total then 1.0 else count_f/total            
            assert(lats.Length = lons.Length && lons.Length=vals.Length)
            seq {
                for i in 1..N-1 do
                    for j in 0..i-1 do
                        if r.NextDouble()<=prob then
                            yield vals.[i],vals.[j],(dist (lats.[i],lons.[i]) (lats.[j],lons.[j]))
                        }

        static member build_emp_var (value_pairs:(float*float*float) seq) binning_tranform back_binning_transform :IEmpericalVariogram =                 
            //the method do not check for the distinct set of inputs. Verify that pairs do not duplicated before calling the method        
            let filtered = 
                let f (d1,d2,dist) =
                    not ((is_nan d1) || (is_nan d2))
                Seq.filter f value_pairs
            let groupped =
                let projection t =
                    let v1,v2,dist = t
                    int(floor(binning_tranform dist))
                Seq.groupBy projection filtered
            let grouppedSumOfSquare =
                let aggregate pairs =
                    let folder acc elem  =       
                        let (a1,a2),(e1,e2) = acc,elem
                        a1+e1,a2+e2
                    let square_diffs = Seq.map (fun (d1:float,d2,dist) -> let a=(d1-d2) in a*a,1) pairs            
                    Seq.fold folder (0.0,0) square_diffs
                Seq.map (fun (grp_idx,pairs) -> grp_idx,aggregate pairs) groupped
            let bins =
                let bin_constructor (grp_idx,(ssq,count)) =
                    {
                        bounds=back_binning_transform (float(grp_idx)), back_binning_transform (float(grp_idx+1));
                        count=count;
                        sum_of_squares=ssq
                    }
                Seq.map bin_constructor grouppedSumOfSquare |> Seq.sortBy (fun bin -> 0.5*(fst bin.bounds + snd bin.bounds))
            let emp_var = ArrayBased_EmpVar(Array.ofSeq bins)
            emp_var :> IEmpericalVariogram
    
    