<#
.SYNOPSIS
    Baut die Solution per MSBuild (Restore + Build) und prüft die Version im VSIX.

.DESCRIPTION
    Ruft `MSBuild.exe IInspectable.ProjectExplorer.sln -t:restore` und anschließend den
    eigentlichen Build. MSBuild wird über vswhere ermittelt (Resolve-PeMsBuild), nicht über
    einen fest verdrahteten Pfad.

    Nach dem Build wird die Version aus dem erzeugten VSIX gelesen und mit Version.props
    verglichen. Weichen sie ab, schlägt der Befehl fehl — so fällt eine nicht durchgeschlagene
    Versionserhöhung sofort auf und nicht erst nach der Installation.

.PARAMETER Configuration
    Build-Konfiguration. Default: Debug.

.PARAMETER Clean
    Vor dem Build alle Build-Artefakte löschen (siehe `pe clean`).

.PARAMETER RemainingArgs
    Zusätzliche, unverändert an den Build-Aufruf durchgereichte MSBuild-Argumente.

.EXAMPLE
    pe build -Configuration Release -Clean

.FUNCTIONALITY
    build
#>
function Invoke-PeBuild {
    [CmdletBinding()]
    param(
        [ValidateSet('Debug', 'Release')]
        [string] $Configuration = 'Debug',
        [switch] $Clean,
        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]] $RemainingArgs
    )

    $ErrorActionPreference = 'Stop'

    $root = Resolve-PeRoot
    if (-not $root) { return }

    $msbuild = Resolve-PeMsBuild
    if (-not $msbuild) { return }

    if ($Clean) { Clear-PeBuildOutput }

    $solution = Join-Path $root 'IInspectable.ProjectExplorer.sln'
    $expected = Get-PeProductVersion -Root $root
    Write-Host "  Produktversion: $expected  ($Configuration)" -ForegroundColor DarkGray

    & $msbuild $solution -t:restore -m -v:m -nologo
    if ($LASTEXITCODE) { throw "Restore fehlgeschlagen (Exit $LASTEXITCODE)." }

    & $msbuild $solution -p:Configuration=$Configuration -m -v:m -nologo @RemainingArgs
    if ($LASTEXITCODE) { throw "Build fehlgeschlagen (Exit $LASTEXITCODE)." }

    $vsix = Join-Path $root "ProjectExplorer.Extension2026\bin\$Configuration\ProjectExplorer.Extension2026.vsix"
    if (-not (Test-Path $vsix)) { throw "Build lief durch, aber das VSIX fehlt: '$vsix'." }

    $actual = Get-PeVsixVersion -Path $vsix
    if ($actual -ne $expected) {
        throw "Version im VSIX ($actual) weicht von Version.props ($expected) ab. Mit 'pe build -Clean' neu bauen."
    }

    Write-Host "  Build OK — VSIX-Version $actual" -ForegroundColor Green
}
