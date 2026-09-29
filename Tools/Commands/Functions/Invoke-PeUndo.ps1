<#
.SYNOPSIS
    Verwirft alle lokalen Änderungen und bereinigt das Arbeitsverzeichnis.

.DESCRIPTION
    Setzt den Worktree komplett zurück: `git checkout -- .` (getrackte Änderungen verwerfen),
    `git clean -fd` (untracked Dateien/Ordner) und `git clean -fdX` (ignorierte Dateien und
    Ordner wie bin/obj). Vor der Ausführung wird nachgefragt (außer mit -Force). Der Repo-/Worktree-Root
    wird zur Aufruf-Zeit aufgelöst (Resolve-PeRoot).

    ACHTUNG: Destruktiv — nicht committete Arbeit geht unwiderruflich verloren.

.PARAMETER Force
    Überspringt die Sicherheitsabfrage.

.FUNCTIONALITY
    undo
#>
function Invoke-PeUndo {
    [CmdletBinding()]
    param(
        [switch] $Force
    )

    $root = Resolve-PeRoot
    if (-not $root) { return }

    if (-not $Force) {
        Write-Host ""
        Write-Host "  Alle lokalen Änderungen in '$root' werden verworfen:" -ForegroundColor Red
        Write-Host "    git checkout -- .   (getrackte Änderungen)" -ForegroundColor DarkGray
        Write-Host "    git clean -fd       (untracked Dateien/Ordner)" -ForegroundColor DarkGray
        Write-Host "    git clean -fdX      (ignorierte Dateien/Ordner, z. B. bin/obj)" -ForegroundColor DarkGray
        Write-Host ""
        $answer = Read-Host "  Wirklich fortfahren? [j/N]"
        if ($answer -notmatch '^(j|ja|y|yes)$') {
            Write-Host "Abgebrochen." -ForegroundColor DarkGray
            return
        }
    }

    git -C $root checkout -- .
    git -C $root clean -fd
    git -C $root clean -fdX

    Write-Host ""
    Write-Host "Arbeitsverzeichnis bereinigt." -ForegroundColor Green
}
