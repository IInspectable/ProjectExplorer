@echo off

set config=%1
if "%config%" == "" (
   set config=Debug
)

"%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" /t:restore"
"%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" IInspectable.ProjectExplorer.sln /p:Configuration="%config%"

REM Pause