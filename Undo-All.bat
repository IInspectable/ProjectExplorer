@echo off

set /P c=Are you sure you want to continue[Y/N]?
if /I "%c%" EQU "Y" goto :Start
goto :eof

:Start
git checkout -- .
git clean -fd
git clean -fX