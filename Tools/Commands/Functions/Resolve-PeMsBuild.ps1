<#
.SYNOPSIS
    Ermittelt den Pfad zur Full-Framework-MSBuild.exe über vswhere.

.DESCRIPTION
    Sucht via vswhere die neueste Visual-Studio-Installation mit MSBuild-Komponente und
    gibt den Pfad zu MSBuild.exe zurück. Die VS-Extension (VSSDK.BuildTools) baut nur mit
    Full-Framework-MSBuild, nicht mit `dotnet build`. Bei Misserfolg wird ein Hinweis
    ausgegeben und `$null` zurückgegeben. Interner Helper (kein pe-Command).
#>
function Resolve-PeMsBuild {
    [CmdletBinding()]
    param()

    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path $vswhere)) {
        Write-Host "vswhere.exe nicht gefunden — ist Visual Studio installiert?" -ForegroundColor Red
        return $null
    }

    $msbuild = & $vswhere -latest -prerelease -requires Microsoft.Component.MSBuild `
        -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1

    if (-not $msbuild -or -not (Test-Path $msbuild)) {
        Write-Host "MSBuild.exe konnte nicht gefunden werden." -ForegroundColor Red
        return $null
    }

    return $msbuild
}
