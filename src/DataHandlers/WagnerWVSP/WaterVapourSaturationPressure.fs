namespace WaterVapourSatPressure

open Microsoft.Research.Science.FetchClimate2.DataHandlers

type FunctionClass() = 
    inherit VirtualDataSource()
    

    // water vapour saturation pressure
    //W. Wagner et al. "The IAPWS Formulation 1995 for the Thermodynamics Properties of Ordinary Water Substance for General and Scientific use"
    //Journal of Physical and Chemcal Reference data, June 2000, Volume 31, Issue 2

    static let tC = 647.096 // Kelvins. Critical temperature
    static let tN = 273.16 //Kelvins. Triple point temperature
    static let pC = 220640.0 // hPa. Critical pressure   
    static let pN = 6.11657 //hPa, Vapour pressure at triple point temperature
    static let c1 = -7.85951783
    static let c2 = 1.84408259
    static let c3 = -11.7866497
    static let c4 = 22.6807411
    static let c5 = -15.9618719
    static let c6 = 1.80122502

    static let a0 = -13.928169
    static let a1 = 34.707823

    static let C = 2.16679 //gK/J

    ///airTemp is in degrees C
    ///ReturnedValue is water vapour saturation pressure in hPa
    static member wvsp airTemp =         
        let tempK= airTemp+tN        
        
        if airTemp>=0.0 then
            let v = 1.0 - tempK/tC
            pC*(exp (tC/tempK*(c1*v + c2*(v**1.5) + c3*v*v*v + c4*(v**3.5) + c5*v*v*v*v + c6*(v**7.5))))
        else
            let theta = tempK/tN
            pN*(exp (a0*(1.0 - theta**(-1.5))+a1*(1.0 - theta**(-1.25))))

    /// relhum is in percents, wvsp is water vapour saturation pressure in hPa
    ///returned value is water vapour pressure in hPa
    static member wvp relHum wvsp =
        relHum*0.01*wvsp

    ///wvp is water vapour pressure in hPa, airTemp is in degrees C
    /// returns an absolute humidity in g/m^3
    static member absHum wvp airTemp =
        C*(wvp*100.0)/(airTemp+tN) //*100.0 to convert hPa to Pa

