@echo off

set config=%1
if "%config%" == "" (
   set config=Debug
)

"%ProgramFiles(x86)%\MSBuild\14.0\bin\msbuild" IInspectable.ProjectExplorer.sln /p:Configuration="%config%"

Pause