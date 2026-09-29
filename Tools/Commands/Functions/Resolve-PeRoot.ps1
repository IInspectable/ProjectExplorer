<#
.SYNOPSIS
    Ermittelt den Repo-/Worktree-Root für die ProjectExplorer-Commands.

.DESCRIPTION
    Löst den Root zur Aufruf-Zeit aus dem angegebenen Pfad (Default: aktuelles
    Verzeichnis) über Git auf — `git -C <Pfad> rev-parse --show-toplevel`. Dadurch
    funktionieren die Commands von jedem Unterordner aus und treffen bei mehreren
    Worktrees automatisch den, in dem man gerade steht. Liefert einen Windows-Pfad
    (Backslashes). Liegt der Pfad in keinem Git-Repository, wird ein freundlicher Hinweis
    ausgegeben und `$null` zurückgegeben — die aufrufenden Commands brechen dann still ab.

.PARAMETER Path
    Startpunkt der Auflösung. Default: aktuelles Arbeitsverzeichnis.
#>
function Resolve-PeRoot {
    [CmdletBinding()]
    param(
        [string] $Path = (Get-Location).Path
    )

    $root = (& git -C $Path rev-parse --show-toplevel 2>$null) -replace '/', '\'
    if (-not $root) {
        Write-Host ""
        Write-Host "  Kein Git-Repository unter '$Path'." -ForegroundColor Red
        Write-Host "  Die pe-Commands müssen innerhalb des Repos (oder eines Worktrees) laufen." -ForegroundColor Yellow
        Write-Host ""
        return $null
    }
    if (-not (Test-Path (Join-Path $root 'IInspectable.ProjectExplorer.sln'))) {
        Write-Host ""
        Write-Host "  '$root' ist kein ProjectExplorer-Repo (IInspectable.ProjectExplorer.sln fehlt)." -ForegroundColor Red
        Write-Host ""
        return $null
    }
    return $root
}
