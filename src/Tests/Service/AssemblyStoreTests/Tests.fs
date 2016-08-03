module AssemblyStoreTests.Tests
type AssemblyStore = Microsoft.Research.Science.FetchClimate2.AssemblyStore
open System.IO
open System.Reflection
open System.Reflection.Emit

open NUnit.Framework
open FsUnit

type ``test with , char`` = Test

[<Test>]
let parse_type_full_name () =
    let typeNameRegEx = 
        typeof<AssemblyStore>
            .GetField("typeNameRegEx", System.Reflection.BindingFlags.NonPublic|||System.Reflection.BindingFlags.Static)
            .GetValue(null) :?> System.Text.RegularExpressions.Regex
    typeNameRegEx |> should not' (be Null)

    let t = typeof<AssemblyStore>
    let m = typeNameRegEx.Match (t.AssemblyQualifiedName)
    m.Success |> should be True
    m.Groups.["type"].Value |> should equal t.FullName
    m.Groups.["assembly"].Value |> should equal t.Assembly.FullName

    let t = typeof<``test with , char``>
    let m = typeNameRegEx.Match (t.AssemblyQualifiedName)
    m.Success |> should be True
    m.Groups.["type"].Value |> should equal t.FullName
    m.Groups.["assembly"].Value |> should equal t.Assembly.FullName

[<Test>]
let folder_store() =
    // create an assembly with a surrogate type out of application base path
    let asmpath = Path.Combine(TestContext.CurrentContext.WorkDirectory,"folder")
    if Directory.Exists asmpath then Directory.Delete(asmpath, true)
    Directory.CreateDirectory asmpath |> ignore
    let surrogateFile = "surrogate.dll"
    let surrogatePath = Path.Combine(asmpath, surrogateFile)
    let surrogateName = "folder_store_surrogate, Version=1.2.3.4"
    let aname = AssemblyName surrogateName
    let abuilder = System.AppDomain.CurrentDomain.DefineDynamicAssembly(aname, AssemblyBuilderAccess.Save, asmpath)
    let mbuilder = abuilder.DefineDynamicModule(aname.Name, surrogateFile)
    let tname = "surrogate"
    let aqname = tname + "," + (aname.ToString())
    let tbuilder = mbuilder.DefineType(tname,TypeAttributes.Public)
    tbuilder.CreateType() |> ignore
    abuilder.Save surrogateFile
    let asm = Assembly.LoadFrom surrogatePath

    // cleanup a folder for a FolderAssemblyStore
    let storepath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "folder_store")
    if Directory.Exists storepath then Directory.Delete(storepath, true)
    // Fresh store should not load the type
    let store = AssemblyStore(storepath, true)
    let success, message = store.TryLoadType aqname
    success |> should be False
    message.StartsWith "Could not load file or assembly" |> should be True
    // Once the assembly is installed, the surrogate type shall be available
    store.Install asm
    let success, message = store.TryLoadType aqname
    success |> should be True
    message.StartsWith (tname+",") |> should be True
    // After cleanup the type shall be unavailable again
    store.Reset()
    let success, message = store.TryLoadType aqname
    success |> should be False
    message.StartsWith "Could not load file or assembly" |> should be True
