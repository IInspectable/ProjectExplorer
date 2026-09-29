<#
.SYNOPSIS
    Erhöht die Produktversion (Major, Minor oder Build) in Version.props und im Quell-Manifest.

.DESCRIPTION
    Schreibt die neue Version in `Version.props` und zusätzlich direkt in
    `ProjectExplorer.Extension2026\source.extension.vsixmanifest`. Das Manifest würde der Build
    zwar ohnehin per XmlPoke nachziehen, aber so stimmt es auch dann, wenn Visual Studio den
    Build überspringt. Beide Dateien werden per Regex ersetzt (Formatierung bleibt erhalten) und
    als UTF-8 mit BOM geschrieben. Liefert die neue Version. Interner Helper (kein pe-Command) für
    Invoke-PeIncreaseBuild/Minor/Major.

.PARAMETER Root
    Repo-/Worktree-Root.

.PARAMETER Part
    Welcher Teil erhöht wird: Major (X+1.0.0), Minor (X.Y+1.0) oder Build (X.Y.Z+1).
#>
function Step-PeProductVersion {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string] $Root,
        [Parameter(Mandatory = $true)]
        [ValidateSet('Major', 'Minor', 'Build')]
        [string] $Part
    )

    $old = Get-PeProductVersion -Root $Root
    $new = switch ($Part) {
        'Major' { [version]::new($old.Major + 1, 0, 0) }
        'Minor' { [version]::new($old.Major, $old.Minor + 1, 0) }
        'Build' { [version]::new($old.Major, $old.Minor, $old.Build + 1) }
    }

    $utf8Bom = [System.Text.UTF8Encoding]::new($true)

    $props = Join-Path $Root 'Version.props'
    $text = [System.IO.File]::ReadAllText($props)
    $text = [regex]::Replace($text, '(<ProductVersion>)[^<]*(</ProductVersion>)', "`${1}$new`${2}")
    [System.IO.File]::WriteAllText($props, $text, $utf8Bom)

    $manifest = Join-Path $Root 'ProjectExplorer.Extension2026\source.extension.vsixmanifest'
    $text = [System.IO.File]::ReadAllText($manifest)
    $text = [regex]::Replace($text, '(<Identity\b[^>]*\bVersion=")[^"]*(")', "`${1}$new`${2}")
    [System.IO.File]::WriteAllText($manifest, $text, $utf8Bom)

    Write-Host "  Version: $old → $new" -ForegroundColor Green
    return $new
}
