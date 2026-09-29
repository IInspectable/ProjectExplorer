<#
.SYNOPSIS
    Konvertiert Quelldateien des Repos auf UTF-8 mit BOM.

.DESCRIPTION
    Führt das begleitende Repair-PeEncoding.cs (liegt neben dieser Funktion) per `dotnet run --file`
    aus und übergibt das Zielverzeichnis (Default: Repo-/Worktree-Root, aufgelöst via
    Resolve-PeRoot). Dateien, die bereits valides UTF-8 mit BOM sind, bleiben unangetastet
    (idempotent, minimale Diffs); Dateien ohne BOM bekommen den BOM ergänzt; Dateien, die kein
    valides UTF-8 sind, werden als Windows-1252 gelesen und umkodiert (verhindert
    U+FFFD-Ersatzzeichen durch lossy UTF-8-Reads). Einmal-Wartungswerkzeug, kein regelmäßiger
    Lauf.

.PARAMETER Path
    Zielverzeichnis, absolut oder relativ zum aktuellen Verzeichnis — z. B. nur ein Teilbaum wie
    Nav.Language\SemanticModel. Default: Repo-/Worktree-Root.

.PARAMETER Extensions
    Kommagetrennte Dateiendungen, die konvertiert werden (z. B. ".cs,.md"). Default siehe
    Repair-PeEncoding.cs: .cs,.csproj,.props,.targets,.slnx,.sln,.md,.ps1

.FUNCTIONALITY
    fixenc
#>
function Repair-PeEncoding {
    [CmdletBinding()]
    param(
        [string] $Path,
        [string] $Extensions
    )

    $ErrorActionPreference = 'Stop'

    $root = Resolve-PeRoot
    if (-not $root) { return }

    $target = if ($Path) { (Resolve-Path $Path).Path } else { $root }

    $script = Join-Path $PSScriptRoot 'Repair-PeEncoding.cs'

    $scriptArgs = @($target)
    if ($Extensions) { $scriptArgs += $Extensions }

    dotnet run --file $script -- @scriptArgs
    if ($LASTEXITCODE) {
        throw "Repair-PeEncoding fehlgeschlagen (Exit $LASTEXITCODE)."
    }
}
