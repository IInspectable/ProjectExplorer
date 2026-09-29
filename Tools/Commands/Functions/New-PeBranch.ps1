<#
.SYNOPSIS
    Legt einen Branch samt danebenliegendem Git-Worktree an und wechselt hinein.

.DESCRIPTION
    Erzeugt in einem Schritt einen Branch und einen Worktree-Ordner als Geschwister des
    Haupt-Repos (`<Parent>\ProjectExplorer-<branch-mit-bindestrichen>`).

    Branch-Typ: Ein Name ohne Slash bekommt das Default-Präfix `feature/`
    (`pe newbranch toolwindow-filter` ⇒ Branch `feature/toolwindow-filter`). `-Type bugfix|hotfix`
    überschreibt das Präfix. Ein Name mit Slash (`foo/bar`) wird verbatim als Branch
    genommen (Slash gewinnt, `-Type` wird dann ignoriert).

    Branch-Status entscheidet die `worktree add`-Variante: neuer Branch (von `-Base`,
    Default = aktueller HEAD), bereits lokal vorhandener Branch oder nur-remote Branch
    (mit --track). Sind im aktuellen Worktree uncommittete Änderungen vorhanden, wird
    gefragt, ob sie mitgenommen werden (Move inkl. untracked via stash → Original sauber).

    Nach dem Anlegen wird in den neuen Worktree gewechselt. Vor jeder Schreiboperation wird
    eine Zusammenfassung gezeigt und nachgefragt; -Force überspringt die Rückfragen (und
    nimmt dann keine Änderungen mit). Der Repo-Root wird zur Aufruf-Zeit aufgelöst
    (Resolve-PeRoot), daher von jedem Ort im Repo aufrufbar.

.PARAMETER Name
    Beschreibender Teil des Branches (z. B. toolwindow-filter) oder ein vollständiger Branchname
    mit Slash (z. B. foo/bar, wird dann verbatim genommen).

.PARAMETER Type
    Präfix für Namen ohne Slash: feature (Default), bugfix oder hotfix.

.PARAMETER Base
    Basis-Ref für einen neuen Branch. Default: aktueller HEAD. Bei bereits existierendem
    Branch wirkungslos.

.PARAMETER Force
    Überspringt die Rückfragen (Carry + Bestätigung). Nimmt dabei keine Änderungen mit.

.FUNCTIONALITY
    newbranch
#>
function New-PeBranch {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string] $Name,
        [ValidateSet('feature', 'bugfix', 'hotfix')]
        [string] $Type = 'feature',
        [string] $Base,
        [switch] $Force
    )

    $root = Resolve-PeRoot
    if (-not $root) { return }

    # Kanonische Repo-Koordinaten — funktioniert auch beim Aufruf aus einem Worktree:
    # --git-common-dir zeigt immer auf das .git des Haupt-Repos.
    $commonDir = (& git -C $root rev-parse --path-format=absolute --git-common-dir) -replace '/', '\'
    $mainRoot = Split-Path $commonDir -Parent
    $repoName = Split-Path $mainRoot -Leaf
    $parent = Split-Path $mainRoot -Parent

    # Branch- und Ordnernamen bilden.
    $clean = ($Name.Trim() -replace '\s+', '-')
    if (-not $clean) {
        Write-Host "Bitte einen Branchnamen angeben." -ForegroundColor Yellow
        return
    }
    $branch = if ($clean -like '*/*') { $clean } else { "$Type/$clean" }
    $safe = $branch -replace '/', '-'
    $dir = Join-Path $parent "$repoName-$safe"

    # Branch-Status bestimmen.
    & git -C $root show-ref --verify --quiet "refs/heads/$branch"
    $existsLocal = ($LASTEXITCODE -eq 0)

    $remoteRaw = & git -C $root ls-remote --heads origin $branch 2>$null
    $existsRemote = ($LASTEXITCODE -eq 0 -and $remoteRaw)

    $isNew = (-not $existsLocal) -and (-not $existsRemote)

    $existingWorktree = @(Get-PeWorktree -Root $root | Where-Object { $_.Branch -eq $branch } |
        Select-Object -First 1)

    # Harte Vorprüfungen.
    if ($existingWorktree) {
        Write-Host ""
        Write-Host "  Branch '$branch' ist bereits in einem Worktree ausgecheckt:" -ForegroundColor Red
        Write-Host "    $($existingWorktree.Path)" -ForegroundColor DarkGray
        Write-Host "  Git erlaubt einen Branch nur in einem Worktree." -ForegroundColor Yellow
        Write-Host ""
        return
    }

    if (Test-Path $dir) {
        Write-Host ""
        Write-Host "  Zielordner existiert bereits:" -ForegroundColor Red
        Write-Host "    $dir" -ForegroundColor DarkGray
        Write-Host ""
        return
    }

    # Basis nur bei neuem Branch relevant.
    $base = $null
    if ($isNew) {
        $base = if ($Base) { $Base } else { (& git -C $root rev-parse --abbrev-ref HEAD).Trim() }
    }
    elseif ($Base) {
        Write-Host "  Hinweis: -Base wird ignoriert, der Branch '$branch' existiert bereits." -ForegroundColor DarkYellow
    }

    # Carry-Frage nur, wenn uncommittete Änderungen vorhanden sind und nicht -Force.
    $carry = $false
    $hasChanges = [bool](& git -C $root status --porcelain)
    if ($hasChanges -and -not $Force) {
        $a = Read-Host "  Lokale Änderungen in den neuen Worktree mitnehmen? [j/N]"
        $carry = ($a -match '^(j|ja|y|yes)$')
    }

    # Zusammenfassung + Bestätigung (entfällt bei -Force).
    if (-not $Force) {
        Write-Host ""
        Write-Host "  Anlegen:" -ForegroundColor Cyan
        Write-Host ("    Branch          $branch " + ($(if ($isNew) { '(neu)' } elseif ($existsLocal) { '(bestehend, lokal)' } else { '(bestehend, remote)' })))
        if ($isNew) { Write-Host "    Basis           $base" }
        Write-Host "    Worktree        $dir"
        Write-Host ("    Änderungen      " + ($(if ($carry) { 'mitnehmen (Original wird sauber)' } elseif ($hasChanges) { 'dalassen' } else { '(keine)' })))
        Write-Host "    Danach          hineinwechseln"
        Write-Host ""
        $answer = Read-Host "  Anlegen? [j/N]"
        if ($answer -notmatch '^(j|ja|y|yes)$') {
            Write-Host "Abgebrochen." -ForegroundColor DarkGray
            return
        }
    }

    # Ausführen.
    if ($carry) {
        & git -C $root stash push --include-untracked -m "carry → $branch"
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Stash fehlgeschlagen — Abbruch, nichts wurde angelegt." -ForegroundColor Red
            return
        }
    }

    if ($isNew) {
        & git -C $root worktree add -b $branch $dir $base
    }
    elseif ($existsLocal) {
        & git -C $root worktree add $dir $branch
    }
    else {
        & git -C $root worktree add --track -b $branch $dir "origin/$branch"
    }

    if ($LASTEXITCODE -ne 0) {
        Write-Host "git worktree add fehlgeschlagen — Abbruch." -ForegroundColor Red
        if ($carry) {
            Write-Host "  Deine Änderungen liegen im Stash: 'git stash list' / 'git stash pop'." -ForegroundColor Yellow
        }
        return
    }

    if ($carry) {
        & git -C $dir stash pop
        if ($LASTEXITCODE -ne 0) {
            Write-Host "  Achtung: 'git stash pop' im neuen Worktree hatte Konflikte — bitte dort auflösen." -ForegroundColor Yellow
        }
    }

    Set-Location $dir

    Write-Host ""
    Write-Host "  Angelegt:" -ForegroundColor Green
    Write-Host "    Branch          $branch"
    Write-Host "    Worktree        $dir"
    Write-Host ""
}
