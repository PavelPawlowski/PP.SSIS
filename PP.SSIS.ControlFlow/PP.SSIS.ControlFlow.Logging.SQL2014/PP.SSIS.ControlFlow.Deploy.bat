@echo off
SET target64="C:\Program Files\Microsoft SQL Server"
SET target32="C:\Program Files (x86)\Microsoft SQL Server"

echo ***********************************
echo PP.SSIS.ControlFlow Deploy Utilitiy
echo ***********************************
echo.

REM Detect Gacutil
set gacutil=
FOR /R "C:\Program Files (x86)\Microsoft SDKs\Windows" %%a  in (gacutil.exe) DO (
    IF EXIST "%%~fa" (
        SET gacutil=%%~fa
    )
)

if exist "%gacutil%" (
	echo Found gacutil.exe: "%gacutil%"
) else (
	echo gacutil.exe not found.
	goto :Usage
)


goto :ParseArgs


:ParseArgs


if /i {%1} == {} goto :Usage
if /i {%1} == {-h} goto :Usage
if /i {%1} == {-help} goto :Usage


REM call appropriate deployment
if /i {%1} == {all} goto :DeployAll
if /i {%1} == {100} goto :Deploy2008
if /i {%1} == {110} goto :Deploy2012
if /i {%1} == {120} goto :Deploy2014
if /i {%1} == {130} goto :Deploy2016
if /i {%1} == {140} goto :Deploy2017
if /i {%1} == {150} goto :Deploy2019


goto :EOF




:Usage
REM PRINT USAGE help
echo.
echo Usage:
echo PP.SSIS.ControlFlow.Deploy.bat versionToDeploy pathToGacUtil
echo.
echo Example:
echo PP.SSIS.ControlFlow.Deploy.bat all             (Install all versions of components, detect gacutil)
echo PP.SSIS.ControlFlow.Deploy.bat all "C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\gacutil.exe"    (Install all versions of components, use specified gacutil location)
echo PP.SSIS.ControlFlow.Deploy.bat 2012            (Install 2012 versions of components, detect gacutil)

REM PRINT Availale versions
echo.
echo Available versions:
echo all - All available versions below
if exist .\100\ (echo 100 - SSIS 2008)
if exist .\110\ (echo 110 - SSIS 2012)
if exist .\120\ (echo 120 - SSIS 2014)
if exist .\130\ (echo 130 - SSIS 2016)
if exist .\140\ (echo 140 - SSIS 2017)
if exist .\150\ (echo 150 - SSIS 2019)

goto :EOF

REM DEPLOY all available versions
:DeployAll
if exist "%gacutil%" (
call :Deploy2008
call :Deploy2012
call :Deploy2014
call :Deploy2016
call :Deploy2017
call :Deploy2019
)
goto :EOF



REM Deployment of single parameterized version of components
:DeploySingle
if exist .\%version%\ (
	echo ---------------------------------------------
	echo Deploying PP.SSIS.ControlFlow %versionRelease% components
	echo ---------------------------------------------
	xcopy "%version%" %target64%\%version% /S /Y /F
	xcopy "%version%" %target32%\%version% /S /Y /F
	if exist ".\%version%\DTS\Tasks\PP.SSIS.ControlFlow.Logging.dll" (
		"%gacutil%" /i ".\%version%\DTS\Tasks\PP.SSIS.ControlFlow.Logging.dll" /f
	)
	if exist ".\%version%\DTS\Tasks\PP.SSIS.ControlFlow.Waiting.dll" (
		"%gacutil%" /i ".\%version%\DTS\Tasks\PP.SSIS.ControlFlow.Waiting.dll" /f
	)
	if exist ".\%version%\DTS\Tasks\PP.SSIS.ControlFlow.Logging.SQL%versionRelease%.dll" (
		"%gacutil%" /i ".\%version%\DTS\Tasks\PP.SSIS.ControlFlow.Logging.SQL%versionRelease%.dll" /f
	)
	if exist ".\%version%\DTS\Tasks\PP.SSIS.ControlFlow.Waiting.SQL%versionRelease%.dll" (
		"%gacutil%" /i ".\%version%\DTS\Tasks\PP.SSIS.ControlFlow.Waiting.SQL%versionRelease%.dll" /f
	)
)

goto :EOF

REM Deploy SSIS 2008 components
:Deploy2008
set version=100
set versionRelease=2008
call :DeploySingle

goto :EOF

REM Deploy SSIS 2012 components
:Deploy2012
set version=110
set versionRelease=2012
call :DeploySingle


goto :EOF

REM Deploy SSIS 2014 components
:Deploy2014
set version=120
set versionRelease=2014
call :DeploySingle


goto :EOF

REM Deploy SSIS 2016 components
:Deploy2016
set version=130
set versionRelease=2016
call :DeploySingle


goto :EOF

REM Deploy SSIS 2017 components
:Deploy2017
set version=140
set versionRelease=2017
call :DeploySingle

goto :EOF

REM Deploy SSIS 2019 components
:Deploy2019
set version=150
set versionRelease=2019
call :DeploySingle

goto :EOF



