@echo off
if "%config%"=="" set config=Debug
echo Using %config% assemblies configuration
echo on
copy ..\..\..\..\DataHandlers\GHCNv2DataSource\bin\%config%\*.dll .\
copy ..\..\..\..\DataHandlers\BRAZILmodelDataSource\bin\%config%\*.dll .\
copy ..\..\..\..\DataHandlers\CESM1BGCDataSource\bin\%config%\*.dll .\
copy ..\..\..\..\DataHandlers\GFDLDataSource\bin\%config%\*.dll .\
copy ..\..\..\..\DataHandlers\HADCM3DataSource\bin\%config%\*.dll .\
copy ..\..\..\..\DataHandlers\LandFilterDataSource\bin\%config%\*.dll .\
copy ..\..\..\..\DataHandlers\MDLDataSource\bin\%config%\*.dll .\
copy ..\..\..\..\DataHandlers\CESM1BGCDataSource\bin\%config%\*.dll .\
copy ..\..\..\..\DataHandlers\ETOPO1DataSource\bin\%config%\*.dll .\
copy ..\..\..\..\DataHandlers\GTOPO30DataSource\bin\%config%\*.dll .\
copy ..\..\..\..\DataHandlers\CRUCL2DataSource\bin\%config%\*.dll .\
copy ..\..\..\..\DataHandlers\WorldClim14DataSource\bin\%config%\*.dll .\
copy ..\..\..\..\DataHandlers\NCEPDataSource\bin\%config%\*.dll .\
copy ..\..\..\..\DataHandlers\CPCDataSource\bin\%config%\*.dll .\
copy ..\..\..\..\DataHandlers\WagnerWVSP\bin\%config%\*.dll .\
copy ..\..\..\..\DataHandlers\MalmstromPET\bin\%config%\*.dll .\
copy ..\..\..\..\DataHandlers\FC1LegacySupport\bin\%config%\*.dll .\
