using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.Data
{
    /// <summary>
    /// An enumeration of the Climate parameter that incapsulates within itself a climate parameter for fetching and corresponding spatial coverage type
    /// </summary>
    /// <remarks>It is supposed to be used at the client side</remarks>
    public enum ClimateParameter
    {
        FC_TEMPERATURE,
        FC_PRECIPITATION,
        FC_LAND_AIR_TEMPERATURE,
        FC_OCEAN_AIR_TEMPERATURE,
        FC_ELEVATION,
        FC_LAND_ELEVATION,
        FC_OCEAN_DEPTH,
        FC_SOIL_MOISTURE,
        FC_RELATIVE_HUMIDITY,
        FC_LAND_AIR_RELATIVE_HUMIDITY,
        FC_LAND_WIND_SPEED,
        FC_LAND_DIURNAL_TEMPERATURE_RANGE,
        FC_LAND_FROST_DAY_FREQUENCY,
        FC_LAND_WET_DAY_FREQUENCY,
        FC_LAND_SUN_PERCENTAGE
    }

    /// <summary>
    /// Describe the data source for FetchClimate
    /// </summary>
    public enum EnvironmentalDataSource
    {
        /// <summary>
        /// The data source with lowest uncertainty
        /// </summary>
        ANY,
        /// <summary>
        /// High-resolution grid of the average climate in the recent past. CRU CL 2.0
        /// </summary>
        CRU_CL_2_0,
        /// <summary>
        /// NCEP/NCAR Reanalysis 1 model output
        /// </summary>
        NCEP_REANALYSIS_1,
        /// <summary>
        /// The Global Historical Climatology Network. The dataset version 2.
        /// </summary> 
        GHCNv2,
        /// <summary>
        /// The monthly data set consists of a file containing monthly averaged soil moisture water height equivalents.
        /// </summary>
        CPC_SOIL_MOSITURE,
        /// <summary>
        /// ETOPO1 is a 1 arc-minute global relief model of Earth's surface that integrates land topography and ocean bathymetry.
        /// </summary>
        ETOPO1_ICE_SHEETS,
        /// <summary>
        /// A global digital elevation model GTOPO30
        /// </summary>
        GTOPO30,
        /// <summary>
        /// WorldClim 1.4 - Global Climate Data. 30 sec grid
        /// </summary>
        WORLD_CLIM_1_4
    }

    public abstract class GlobalConsts
    {
        public const int DefaultValue = -999;
        public const int StartYear = 1961;
        public const int EndYear = 1990;
        public const double tempSpatDerivative = 0.1 / 0.008333;
        public const double tempSpatSecondDerivative = 0.0001 / 0.008333;
        public const double tempTimeSecondDerivative = 5.0 * Math.PI * Math.PI / 144.0;
    }
}

