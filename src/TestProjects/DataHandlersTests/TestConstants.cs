using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Tests
{
    static class TestConstants
    {
        public const double FloatPrecision = 1e-5;
        public const double DoublePrecision = 1e-13;

        
        public static readonly string UriCru;
        public static readonly string UriReanalysisRegular;
        public static readonly string UriReanalysisGauss;
        public static readonly string UriWorldClim;
        public static readonly string UriEtopo;
        public static readonly string UriGtopo;
        public static readonly string UriCpc;
        public static readonly string UriHADCM3_sra_tas;
        public static readonly string UriGHCN;

        public static readonly string CloudServiceURI = @"http://fetchclimate2.cloudapp.net";

        static TestConstants()
        {
            switch (Environment.MachineName.ToLower())
            {
                case "quadro":
                    UriCru = @"D:\ClimateData\cru2_wo_strings_with_variograms.nc?openMode=readOnly";
                    UriReanalysisRegular = @"D:\ClimateData\ReanalysisRegular_with_variograms.nc?openMode=readOnly";
                    UriReanalysisGauss = @"D:\ClimateData\ReanslysisGaussT62_with_variograms.nc?openMode=readOnly";
                    UriWorldClim = @"D:\ClimateData\WorldClimCurr_with_variograms.nc?openMode=readOnly";
                    UriEtopo = @"D:\ClimateData\ETOPO1_Ice_g_gmt4_with_variograms.nc?openMode=readOnly";
                    UriGtopo = @"D:\ClimateData\GTOPO30_with_variograms.nc?openMode=readOnly";
                    UriCpc = @"D:\ClimateData\soilw.mon.mean.v2_with_variograms.nc?openMode=readOnly";
                    UriHADCM3_sra_tas = @"D:\ClimateData\HADCM3_SRA1B_1_N_tas_1-2399.nc?openMode=readOnly";
                    UriGHCN = @"D:\ClimateData\GHCNv2_201107_wo_strings.nc?openMode=readOnly";
                    break;

                case "cockroach":
                //    UriCru = @"C:\ClimateData\cru2_wo_strings_with_variograms.nc?openMode=readOnly";
                //    UriReanalysisRegular = @"C:\ClimateData\ReanalysisRegular_with_variograms.nc?openMode=readOnly";
                //    UriReanalysisGauss = @"C:\ClimateData\ReanslysisGaussT62_with_variograms.nc?openMode=readOnly";
                //    UriWorldClim = @"C:\ClimateData\WorldClimCurr_with_variograms.nc?openMode=readOnly";
                //    UriEtopo = @"C:\ClimateData\ETOPO1_Ice_g_gmt4_with_variograms.nc?openMode=readOnly";
                    UriGtopo = @"C:\ClimateData\GTOPO30.nc?openMode=readOnly";
                //    UriCpc = @"C:\ClimateData\soilw.mon.mean.v2_with_variograms.nc?openMode=readOnly";
                //    UriHADCM3_sra_tas = @"C:\ClimateData\HADCM3_SRA1B_1_N_tas_1-2399.nc?openMode=readOnly";
                //    UriGHCN = @"C:\ClimateData\GHCNv2_201107_wo_strings.nc?openMode=readOnly";
                    break;

                default:
                    UriCru = @"msds:memory"; //no azure dataset availalbe
                    UriReanalysisRegular = @"msds:az?id=72&DefaultEndpointsProtocol=http&AccountName=fc2chunkedstorage&AccountKey=dnPQl1Zjwpzm2qLPW/J9MFrhPWYocz3h/2zzuQ+RxCTE+ClFfKIriu4aCwJpPt+P6sU8hJfiWfQaBYc4nDSY/Q==";
                    UriReanalysisGauss = @"msds:az?name=ReanalysisGaussT62&DefaultEndpointsProtocol=http&AccountName=fc2chunkedstorage&AccountKey=dnPQl1Zjwpzm2qLPW/J9MFrhPWYocz3h/2zzuQ+RxCTE+ClFfKIriu4aCwJpPt+P6sU8hJfiWfQaBYc4nDSY/Q==";       
                    UriWorldClim = @"msds:az?name=WorldClimCurrent&DefaultEndpointsProtocol=http&AccountName=fc2chunkedstorage&AccountKey=dnPQl1Zjwpzm2qLPW/J9MFrhPWYocz3h/2zzuQ+RxCTE+ClFfKIriu4aCwJpPt+P6sU8hJfiWfQaBYc4nDSY/Q==";
                    UriEtopo = @"msds:az?name=ETOPO1&DefaultEndpointsProtocol=http&AccountName=fc2chunkedstorage&AccountKey=dnPQl1Zjwpzm2qLPW/J9MFrhPWYocz3h/2zzuQ+RxCTE+ClFfKIriu4aCwJpPt+P6sU8hJfiWfQaBYc4nDSY/Q==";
                    UriGtopo = @"msds:az?name=gtopo30&DefaultEndpointsProtocol=http&AccountName=fc2chunkedstorage&AccountKey=dnPQl1Zjwpzm2qLPW/J9MFrhPWYocz3h/2zzuQ+RxCTE+ClFfKIriu4aCwJpPt+P6sU8hJfiWfQaBYc4nDSY/Q==";
                    UriCpc = @"msds:az?name=CpcSoilMoisture&DefaultEndpointsProtocol=http&AccountName=fc2chunkedstorage&AccountKey=dnPQl1Zjwpzm2qLPW/J9MFrhPWYocz3h/2zzuQ+RxCTE+ClFfKIriu4aCwJpPt+P6sU8hJfiWfQaBYc4nDSY/Q==";
                    UriHADCM3_sra_tas = @"msds:az?AccountName=fc2chunkedstorage&AccountKey=dnPQl1Zjwpzm2qLPW/J9MFrhPWYocz3h/2zzuQ+RxCTE+ClFfKIriu4aCwJpPt+P6sU8hJfiWfQaBYc4nDSY/Q==&DefaultEndpointsProtocol=http&name=HADCM3_SRA1B";
                    UriGHCN = @"msds:az?AccountName=fc2chunkedstorage&AccountKey=dnPQl1Zjwpzm2qLPW/J9MFrhPWYocz3h/2zzuQ+RxCTE+ClFfKIriu4aCwJpPt+P6sU8hJfiWfQaBYc4nDSY/Q==&DefaultEndpointsProtocol=http&name=GHCNv2";
                    break;
            }
        }

    }
}
