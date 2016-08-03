namespace UnitTestProject1

open System
open Microsoft.VisualStudio.TestTools.UnitTesting

open Microsoft.Research.Science.FetchClimate2
open Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators

[<TestClass>]
type UnitTest2() = 
    [<TestMethod>]
    [<TestCategory("Local")>]
    [<TestCategory("BVT")>]
    member x.ManualGetTemporalVarianceTest() = 
        let t = PrivateType(typedefof<LinearCombination1DVarianceCalc>)
        let nodeLocations = [| 0.0; 4.0; 8.0; 12.0|]
        let targetLocations = [| 5.0; 7.0 |]

        let weights = 
            let w = IPs()
            let bb  = IndexBoundingBox()

            bb.first <- 1
            bb.last <- 2

            w.BoundingIndices <- bb
            w.Indices <- [| 1;2 |]
            w.Weights <- [| 0.5; 0.5|]
            w

        let covStructure =
            let covariogram dist = 9.0 - dist
            let privateStructure = PrivateObject("FetchMath","Microsoft.Research.Science.FetchClimate2.TemporalVarianceProperties",16.0 :> obj, Func<float,float,float>(fun c1 c2 -> covariogram(abs(c1-c2))) :> obj)            
            privateStructure                

        let actual = t.InvokeStatic("GetTemporalVariance",[| weights :> obj; nodeLocations :> obj; targetLocations :> obj; covStructure.Target |])
        //(16 + (16*0.25 + 16*0.25 + 5*0.25 + 5*0.25) - 2*(0.5*7 + 0.5*7))*0.5 = 6.25
        Assert.AreEqual(6.25,actual)

    [<TestMethod>]
    [<TestCategory("Local")>]
    [<TestCategory("BVT")>]
    member x.AlignTest() =
        let t = PrivateType(typedefof<LinearCombinationOnSphereVarianceCalculator>)
        let nodeLocations = [| 0.0; 4.0; 8.0; 12.0|]
        let targets =   [|-1.0;0.0;0.2;3.0;4.0;5.0;7.0;8.0;12.0;16.0|]
        let expected =  [| 0.0;0.0;0.0;4.0;4.0;4.0;8.0;8.0;12.0;12.0|]
        let aligned = t.InvokeStatic("Align",nodeLocations,targets) :?> float array
        Assert.IsTrue(Array.map2 (fun a b -> a=b) expected aligned |> Seq.forall (fun r -> r))
    

    [<TestMethod>]
    [<TestCategory("Local")>]
    [<TestCategory("BVT")>]
    member x.ManualGetSpatialVarianceTest() = 
        let t = PrivateType(typedefof<LinearCombinationOnSphereVarianceCalculator>)
        let nodeLocations = [| 0.0; 4.0; 8.0; 12.0|] //the same for two dimensions
        let targetLocations1 = [| 6.0 |]
        let targetLocations2 = [| 8.0 |]

        let weights1 = 
            let w = IPs()
            let bb  = IndexBoundingBox()

            bb.first <- 1
            bb.last <- 2

            w.BoundingIndices <- bb
            w.Indices <- [| 1;2 |]
            w.Weights <- [| 0.5; 0.5|]
            w

        let weights2 = 
            let w = IPs()
            let bb  = IndexBoundingBox()

            bb.first <- 2
            bb.last <- 2

            w.BoundingIndices <- bb
            w.Indices <- [| 2 |]
            w.Weights <- [| 1.0 |]
            w

        let covStructure =
            let covariogram dist = 9.0 - dist
            let dist x1 y1 x2 y2 = sqrt ((x1-x2)*(x1-x2)+(y1-y2)*(y1-y2))
            let privateStructure = PrivateObject("FetchMath" ,"Microsoft.Research.Science.FetchClimate2.SpatialVarianceProperties", (16.0 :> obj),(Func<float,float,float,float,float>(fun x1 y1 x2 y2 -> dist x1 y1 x2 y2) :> obj),(Func<float,float>(fun dist -> covariogram dist)))
            privateStructure                

        let actual = t.InvokeStatic("GetSpatialVariance",[| weights1 :> obj; weights2 :> obj; nodeLocations :> obj; nodeLocations :> obj; targetLocations1 :> obj; targetLocations2 :> obj; covStructure.Target |]) :?> float
        //total weights = 0.0625 0.1875 0.1875 0.5625
        //cov0 = 16
        //sum sum( wi wj cov i j) = 16*2*0.25+5*2*0.25 = 10.5
        //sum w_i cov x_i x = 0.5*2*7 = 7
        //actual =(16 + 10.5 - 2.0 * 7)*0.5
        Assert.AreEqual(6.25,actual)

