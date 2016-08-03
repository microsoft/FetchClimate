namespace VariogramTests

open System
open Microsoft.VisualStudio.TestTools.UnitTesting

type IEmpericalVariogram = VariogramModule.IEmpericalVariogram

[<TestClass>]
type EmpiricalVariogramTests() = 
    [<TestMethod>]
    [<TestCategory "Local">]
    [<TestCategory("BVT")>]
    member x.EmpiricalVariogramTest () = 
        let ds1 = [| 0.0; 0.0; 0.0; 1.0; 1.0; 3.0|]
        let ds2 = [| 1.0; 3.0; 2.0; 3.0; 2.0; 2.0|]
        let dists = [| 3.0; 5.0; 4.0;4.0; 5.0;3.0|]
        let zipped = Array.zip3 ds1 ds2 dists
        let var = EmpVariogramBuilder.EmpiricalVariogramBuilder.build_emp_var zipped (fun (x:float) -> x) (fun (x:float) -> x)

        Assert.AreEqual(3,var.Distances.Length)
        Assert.AreEqual(3,var.GammaValues.Length)

        for i in 0..2 do
            let d_idx = var.Distances.[i]
            let gamma = var.GammaValues.[i]
            match d_idx with
            |   3.5 -> Assert.AreEqual(1.0,gamma)
            |   4.5 -> Assert.AreEqual(4.0,gamma)
            |   5.5 -> Assert.AreEqual(5.0,gamma)
            |   _   ->  Assert.Fail "Such value of Gamma must not be present here"

    [<TestMethod>]
    [<TestCategory "Local">]
    [<TestCategory("BVT")>]
    member x.EmpiricalScaledVariogramTest () = 
        let ds1 = [| 0.0; 0.0; 0.0; 1.0; 1.0; 3.0|]
        let ds2 = [| 1.0; 3.0; 2.0; 3.0; 2.0; 2.0|]
        let dists = [| 3.0; 5.0; 4.0;4.0; 5.0;3.0|] |> Array.map (fun x -> x*20.0)
        let zipped = Array.zip3 ds1 ds2 dists
        let var = EmpVariogramBuilder.EmpiricalVariogramBuilder.build_emp_var zipped (fun (x:float) -> x*0.05) (fun (x:float) -> x*20.0)

        System.Diagnostics.Trace.WriteLine(System.IO.Directory.GetCurrentDirectory())

        Assert.AreEqual(3,var.Distances.Length)
        Assert.AreEqual(3,var.GammaValues.Length)

        for i in 0..2 do
            let d_idx = var.Distances.[i]
            let gamma = var.GammaValues.[i]
            match d_idx with
            |   70.0 -> Assert.AreEqual(1.0,gamma)
            |   90.0 -> Assert.AreEqual(4.0,gamma)
            |   110.0 -> Assert.AreEqual(5.0,gamma)
            |   _   ->  Assert.Fail "Such value of Gamma must not be present here"

type EmpVar_stub(data) =
    let nugget = 10.0
    let sill = 1000.0
    let range = 500.0
    let gaussian dist nugget range sill =  (sill-nugget)*(1.0-exp(-(dist*dist)/(range*range)*3.0))+nugget
    let g dist = gaussian dist nugget range sill

    interface IEmpericalVariogram with
        member s.GammaValues = Array.init 600 (fun i -> g(double(i)))
        member s.Distances = Array.init 600 (fun i -> double(i))

//base class for testing all avialable fitters
type VariogramFitterTests(f:VariogramModule.IVariogramFitter) =      
    [<TestMethod>]
    [<TestCategory "Local">]
    [<TestCategory("BVT")>]                
    member x.VariogramFitTest () =        
        try //VS bug. see https://connect.microsoft.com/VisualStudio/feedback/details/844483/getting-invalidprogramexception-when-assembly-with-f-unit-test-is-compiled-in-release-mode-and-it-is-using-runsettings-file
            let emp_var = EmpVar_stub()
            let variogram = f.Fit emp_var
            match variogram with
                |   Some(v)  ->
                    
                    Assert.AreEqual(1000.0,v.Sill,1e-5)
                    Assert.AreEqual(500.0,v.Range,1e-5)
                    Assert.AreEqual(10.0,v.Nugget,1e-5)
                |   None -> Assert.Fail("The fitter did not converge")
        with
        |   :? System.InvalidProgramException -> Assert.Inconclusive("VS bug prevented test from execution. Got InvalidProgramException. See https://connect.microsoft.com/VisualStudio/feedback/details/844483/getting-invalidprogramexception-when-assembly-with-f-unit-test-is-compiled-in-release-mode-and-it-is-using-runsettings-file")

[<TestClass>]
[<DeploymentItem("lmfit64","lmfit64")>]
[<DeploymentItem("lmfit32","lmfit32")>]
type LMDotNetVariogramFitterTests() =
    inherit VariogramFitterTests(LMDotNetVariogramFitter.Fitter() :> VariogramModule.IVariogramFitter)    
