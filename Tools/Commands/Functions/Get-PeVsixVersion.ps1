<#
.SYNOPSIS
    Liest die Version aus einem gebauten VSIX (extension.vsixmanifest im ZIP).

.DESCRIPTION
    Öffnet das VSIX als ZIP-Archiv und liefert `PackageManifest/Metadata/Identity/@Version`
    aus dem enthaltenen `extension.vsixmanifest`. Das ist die Version, die Visual Studio beim
    Installieren tatsächlich sieht — unabhängig davon, was in Version.props oder im
    Quell-Manifest steht. Interner Helper (kein pe-Command).

.PARAMETER Path
    Pfad zur .vsix-Datei.
#>
function Get-PeVsixVersion {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path $Path).Path)
    try {
        $entry = $zip.Entries | Where-Object { $_.FullName -eq 'extension.vsixmanifest' } | Select-Object -First 1
        if (-not $entry) { throw "Kein extension.vsixmanifest in '$Path'." }

        $reader = [System.IO.StreamReader]::new($entry.Open())
        try { $xml = [xml] $reader.ReadToEnd() } finally { $reader.Dispose() }
    }
    finally {
        $zip.Dispose()
    }

    return [version] $xml.PackageManifest.Metadata.Identity.Version
}
