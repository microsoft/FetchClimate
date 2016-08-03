module VariogramModels

let gaussian dist nugget range sill = (sill-nugget)*(1.0-exp(-(dist*dist)/(range*range)*3.0))+nugget
let spherical dist nugget range sill =
    let a = if dist<range then (3.0*dist/(2.0*range)-(dist*dist*dist)/(2.0*range*range*range)) else 1.0    
    (sill-nugget)*a+nugget
let exponential dist nugget range sill = (sill-nugget)*(1.0-exp(-dist/range))+nugget   
