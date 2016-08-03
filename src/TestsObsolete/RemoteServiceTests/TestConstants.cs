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
                    UriCru = @"msds:nc?openMode=readOnly&file=D:\ClimateData\cru20.nc";
                    //UriReanalysisRegular = @"msds:nc?openMode=readOnly&file=C:\ClimateData\ReanalysisRegular.nc";
                    //UriReanalysisGauss = @"msds:nc?openMode=readOnly&file=C:\ClimateData\ReanalysisGaussT62.nc";
                    //UriReanalysisRegular = @"msds:az?name=ReanalysisRegular&DefaultEndpointsProtocol=http&AccountName=fc2chunkedstorage&AccountKey=dnPQl1Zjwpzm2qLPW/J9MFrhPWYocz3h/2zzuQ+RxCTE+ClFfKIriu4aCwJpPt+P6sU8hJfiWfQaBYc4nDSY/Q==";
                    UriReanalysisRegular = @"msds:az?id=72&DefaultEndpointsProtocol=http&AccountName=fc2chunkedstorage&AccountKey=dnPQl1Zjwpzm2qLPW/J9MFrhPWYocz3h/2zzuQ+RxCTE+ClFfKIriu4aCwJpPt+P6sU8hJfiWfQaBYc4nDSY/Q==";
                    UriReanalysisGauss = @"msds:az?name=ReanalysisGaussT62&DefaultEndpointsProtocol=http&AccountName=fc2chunkedstorage&AccountKey=dnPQl1Zjwpzm2qLPW/J9MFrhPWYocz3h/2zzuQ+RxCTE+ClFfKIriu4aCwJpPt+P6sU8hJfiWfQaBYc4nDSY/Q==";
                    UriWorldClim = @"msds:nc?openMode=readOnly&file=D:\ClimateData\WorldClimCurr.nc";
                    UriEtopo = @"msds:nc?openMode=readOnly&file=D:\ClimateData\ETOPO1_Ice_g_gmt4.nc";
                    UriGtopo = @"msds:nc?openMode=readOnly&file=D:\ClimateData\GTOPO30.nc";
                    UriCpc = @"msds:nc?openMode=readOnly&file=D:\ClimateData\soilw.mon.mean.v2.nc";
                    UriHADCM3_sra_tas = @"msds:nc?openMode=readOnly&file=C:\Users\Dmitry\SharePoint\CEES DEV - Documents 1\Fetch Climate 2\HADCM3\HADCM3_SRA1B_1_N_tas_1-2399.nc";
                    UriGHCN = @"msds:nc?openMode=readOnly&file=D:\ClimateData\ghcnV2_20111214.nc";
                    break;
                case "cockroach":
                    UriCru = @"msds:nc?openMode=readOnly&file=C:\ClimateData\cru20.nc";
                    //UriReanalysisRegular = @"msds:nc?openMode=readOnly&file=C:\ClimateData\ReanalysisRegular.nc";
                    //UriReanalysisGauss = @"msds:nc?openMode=readOnly&file=C:\ClimateData\ReanalysisGaussT62.nc";
                    UriReanalysisRegular = @"msds:az?id=72&DefaultEndpointsProtocol=http&AccountName=fc2chunkedstorage&AccountKey=dnPQl1Zjwpzm2qLPW/J9MFrhPWYocz3h/2zzuQ+RxCTE+ClFfKIriu4aCwJpPt+P6sU8hJfiWfQaBYc4nDSY/Q==";
                    UriReanalysisGauss = @"msds:az?name=ReanalysisGaussT62&DefaultEndpointsProtocol=http&AccountName=fc2chunkedstorage&AccountKey=dnPQl1Zjwpzm2qLPW/J9MFrhPWYocz3h/2zzuQ+RxCTE+ClFfKIriu4aCwJpPt+P6sU8hJfiWfQaBYc4nDSY/Q==";
                    UriWorldClim = @"msds:nc?openMode=readOnly&file=C:\ClimateData\WorldClimCurr.nc";
                    UriEtopo = @"msds:nc?openMode=readOnly&file=C:\ClimateData\ETOPO1_Ice_g_gmt4.nc";
                    UriGtopo = @"msds:nc?openMode=readOnly&file=C:\ClimateData\GTOPO30.nc";
                    UriCpc = @"msds:nc?openMode=readOnly&file=C:\ClimateData\soilw.mon.mean.v2.nc";
                    UriHADCM3_sra_tas = @"msds:nc?openMode=readOnly&file=C:\ClimateData\HADCM3_SRA1B_1_N_tas_1-2399.nc";
                    UriGHCN = @"msds:nc?openMode=readOnly&file=C:\ClimateData\ghcnV2_20111214.nc";
                    break;
                default:
                    UriCru = @"msds:az?name=CRU_CL_2_0&DefaultEndpointsProtocol=http&AccountName=fc2chunkedstorage&AccountKey=dnPQl1Zjwpzm2qLPW/J9MFrhPWYocz3h/2zzuQ+RxCTE+ClFfKIriu4aCwJpPt+P6sU8hJfiWfQaBYc4nDSY/Q==";
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
