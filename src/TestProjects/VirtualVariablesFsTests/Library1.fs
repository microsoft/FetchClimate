namespace VirtVarTests

open Microsoft.Research.Science.FetchClimate2.DataHandlers

type Gauss = float * float

type FunctionClass() = 
    inherit VirtualDataSource()
    
    static member A B C D =
        B + 2.0*C + 3.0*D
    
    static member E F G H I =
        G + 2.0 *H + 3.0* I+ 4.0*F 
        
    static member X (Y:Gauss) (Z:Gauss) : Gauss =
        (fst(Y) - 2.0 * fst(Z), snd(Y) + 2.0 * snd(Z))