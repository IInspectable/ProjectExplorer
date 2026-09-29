<#
.SYNOPSIS
    Installiert das VSIX aus deploy\ in Visual Studio.

.DESCRIPTION
    Nimmt `deploy\ProjectExplorer-<Version>.vsix` (Version aus Version.props) und startet den
    VSIXInstaller der neuesten Visual-Studio-Installation (über vswhere ermittelt). Fehlt das
    VSIX, weist der Befehl auf `pe vsix` hin, statt einen alten Stand zu installieren.

.FUNCTIONALITY
    install
#>
function Install-PeExtension {
    [CmdletBinding()]
    param()

    $ErrorActionPreference = 'Stop'

    $root = Resolve-PeRoot
    if (-not $root) { return }

    $version = Get-PeProductVersion -Root $root
    $vsix = Join-Path $root "deploy\ProjectExplorer-$version.vsix"
    if (-not (Test-Path $vsix)) {
        Write-Host "VSIX nicht gefunden: '$vsix'" -ForegroundColor Red
        Write-Host "  Zuerst mit 'pe vsix' erzeugen." -ForegroundColor Yellow
        return
    }

    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path $vswhere)) {
        Write-Host "vswhere.exe nicht gefunden — ist Visual Studio installiert?" -ForegroundColor Red
        return
    }
    $installPath = & $vswhere -latest -prerelease -property installationPath | Select-Object -First 1
    $installer = Join-Path $installPath 'Common7\IDE\VSIXInstaller.exe'
    if (-not (Test-Path $installer)) {
        Write-Host "VSIXInstaller.exe nicht gefunden unter '$installer'." -ForegroundColor Red
        return
    }

    Write-Host "Installiere $vsix ..." -ForegroundColor Cyan
    & $installer $vsix
}
