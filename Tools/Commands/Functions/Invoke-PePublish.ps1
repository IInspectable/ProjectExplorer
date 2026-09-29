<#
.SYNOPSIS
    Clean + Release-Build und legt das VSIX versioniert unter deploy\ ab.

.DESCRIPTION
    Löscht alle Build-Artefakte (`pe clean`), baut die Solution frisch (`pe build`, inklusive
    Versionsprüfung im VSIX) und kopiert das VSIX nach
    `deploy\ProjectExplorer-<Version>.vsix`. Von dort installiert `pe install`.

.PARAMETER Configuration
    Build-Konfiguration. Default: Release.

.FUNCTIONALITY
    vsix
#>
function Invoke-PePublish {
    [CmdletBinding()]
    param(
        [ValidateSet('Debug', 'Release')]
        [string] $Configuration = 'Release'
    )

    $ErrorActionPreference = 'Stop'

    $root = Resolve-PeRoot
    if (-not $root) { return }

    Invoke-PeBuild -Configuration $Configuration -Clean

    $version = Get-PeProductVersion -Root $root
    $source = Join-Path $root "ProjectExplorer.Extension2026\bin\$Configuration\ProjectExplorer.Extension2026.vsix"
    $deploy = Join-Path $root 'deploy'
    $target = Join-Path $deploy "ProjectExplorer-$version.vsix"

    New-Item -ItemType Directory -Path $deploy -Force | Out-Null
    Copy-Item -LiteralPath $source -Destination $target -Force

    Write-Host ""
    Write-Host "  VSIX bereit: $target" -ForegroundColor Green
    Write-Host "  Installieren mit 'pe install'." -ForegroundColor DarkGray
    Write-Host ""
}
