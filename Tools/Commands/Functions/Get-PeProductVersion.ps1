<#
.SYNOPSIS
    Liest die Produktversion aus Version.props.

.DESCRIPTION
    `Version.props` ist die einzige Quelle der Wahrheit für die Version (Property
    `ProductVersion`). Von dort gelangt sie beim Build per `UpdateProductVersion.targets` in
    `ThisAssembly.generated.cs` (Assembly-/Dateiversion) und in das `source.extension.vsixmanifest`
    (VSIX-Version). Interner Helper (kein pe-Command).

.PARAMETER Root
    Repo-/Worktree-Root.
#>
function Get-PeProductVersion {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string] $Root
    )

    $file = Join-Path $Root 'Version.props'
    $text = [System.IO.File]::ReadAllText($file)
    $match = [regex]::Match($text, '<ProductVersion>\s*([^<\s]+)\s*</ProductVersion>')
    if (-not $match.Success) { throw "Keine <ProductVersion> in '$file' gefunden." }

    return [version] $match.Groups[1].Value
}
