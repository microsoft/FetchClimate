REM   Check if this task is running on the compute emulator.
REM 
IF "%ComputeEmulatorRunning%" == "true" Goto End

echo %DATE% %TIME% >> deploy.log
echo "Deployment script fired!" >> deploy.log
REM vcredist_x64.exe /q /norestart /log msvcr_install.log	
REM dotNetFx40_Client_x86_x64.exe /q /norestart /log dotnet_install.log	
msiexec /i ScientificDataSet.msi /qn /norestart /lv sds_install.log SKIPVCRUNTIMECHECK=TRUE
EXIT /B 0

:End