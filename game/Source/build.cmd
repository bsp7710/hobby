@echo off
setlocal
pushd "%~dp0"
for %%F in ("%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" "%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe") do if not defined EUNRAM_COMPILER if exist "%%~F" set "EUNRAM_COMPILER=%%~F"
if not defined EUNRAM_COMPILER (
  echo Windows .NET Framework compiler was not found.
  popd
  exit /b 1
)
"%EUNRAM_COMPILER%" /nologo /target:winexe /optimize+ /platform:anycpu /codepage:65001 /utf8output /out:"TheUncalled.exe" /win32icon:"data\bell.ico" /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Web.Extensions.dll /r:System.IO.Compression.dll /resource:"data\company.cast",company.cast /resource:"manuscript.txt",manuscript /resource:"data\campus.jpg",campus.jpg /resource:cases.txt,cases.txt *.cs
set "EUNRAM_EXIT=%ERRORLEVEL%"
popd
exit /b %EUNRAM_EXIT%

