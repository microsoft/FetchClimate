namespace MalmstromPET

open Microsoft.Research.Science.FetchClimate2.DataHandlers

type FunctionClass() = 
    inherit VirtualDataSource()
    static let ps0 = 610.78; /// The saturation vapour pressure at zero degrees C (pascals)    
    
    ///airTemp is in degrees C
    ///ReturnedValue is in mm/month    
    static member Pet airTemp =         
        let psa = /// predicts vapour pressure as a function of temperature            
            if airTemp < 0.0 then // With different functions depending on whether the temperaure is above or below 0 degrees C
                exp((-6140.4 / (273.15 + airTemp)) + 28.916)
            else
                610.78 * exp((airTemp / (airTemp + 238.3)) * 17.2694)
        25.0*(psa /ps0)    