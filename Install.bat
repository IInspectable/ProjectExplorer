@Echo Off
powershell.exe -noprofile -Command "& {. '%~dp0IInspectable.ProjectExplorer.Extension\bin\Debug\IInspectable.ProjectExplorer.Extension.vsix'}"
