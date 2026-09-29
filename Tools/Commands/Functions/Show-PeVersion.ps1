<#
.SYNOPSIS
    Zeigt die Version aus Version.props, Quell-Manifest und den gebauten VSIX-Dateien.

.DESCRIPTION
    Diagnose-Übersicht, wo welche Version steht: `Version.props` (Quelle der Wahrheit), das
    Quell-Manifest `source.extension.vsixmanifest` sowie die tatsächlich in den VSIX-Dateien
    (`bin\Debug`, `bin\Release`, `deploy\`) enthaltene Version. Abweichungen zu Version.props
    werden gelb markiert — typischer Fall: Visual Studio hat nach einer Versionserhöhung nicht
    neu gebaut.

.FUNCTIONALITY
    version
#>
function Show-PeVersion {
    [CmdletBinding()]
    param()

    $root = Resolve-PeRoot
    if (-not $root) { return }

    $expected = Get-PeProductVersion -Root $root

    $manifest = Join-Path $root 'ProjectExplorer.Extension2026\source.extension.vsixmanifest'
    $manifestVersion = [version] ([xml] [System.IO.File]::ReadAllText($manifest)).PackageManifest.Metadata.Identity.Version

    $rows = @(
        [pscustomobject]@{ Quelle = 'Version.props'; Version = $expected }
        [pscustomobject]@{ Quelle = 'source.extension.vsixmanifest'; Version = $manifestVersion }
    )

    $vsixFiles = @(
        foreach ($config in 'Debug', 'Release') {
            Join-Path $root "ProjectExplorer.Extension2026\bin\$config\ProjectExplorer.Extension2026.vsix"
        }
        Get-ChildItem (Join-Path $root 'deploy') -Filter *.vsix -ErrorAction SilentlyContinue |
            ForEach-Object FullName
    ) | Where-Object { Test-Path $_ }

    foreach ($vsix in $vsixFiles) {
        $rows += [pscustomobject]@{
            Quelle  = $vsix.Substring($root.Length + 1)
            Version = Get-PeVsixVersion -Path $vsix
        }
    }

    $width = ($rows.Quelle | Measure-Object -Maximum -Property Length).Maximum
    Write-Host ''
    foreach ($row in $rows) {
        $color = if ($row.Version -eq $expected) { 'Gray' } else { 'Yellow' }
        Write-Host ("  {0,-$width}  {1}" -f $row.Quelle, $row.Version) -ForegroundColor $color
    }
    Write-Host ''
}
