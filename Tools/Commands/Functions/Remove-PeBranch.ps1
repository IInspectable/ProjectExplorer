<#
.SYNOPSIS
    Löscht einen Branch vollständig: lokaler Branch, zugehöriger Worktree-Ordner und
    Remote-Branch.

.DESCRIPTION
    Entfernt genau einen Branch in einem Rutsch — den lokalen Branch, den danebenliegenden
    Worktree-Ordner (`git worktree remove`) und den Remote-Branch (`git push origin
    --delete`). Ohne Branch-Argument erscheint ein Pfeiltasten-Menü über alle lokalen
    Branches (außer `master`).

    `master` ist hart gesperrt und wird niemals gelöscht — auch nicht mit -Force.

    Harte Vorprüfungen (jeweils Abbruch mit Hinweis, auch mit -Force):
      - Aufrufer steht im zu löschenden Worktree → zuerst in einen anderen Ordner wechseln.
      - Worktree ist nicht sauber (dirty/untracked) → erst commit/stash/clean.

    Ist der Branch nicht in `master` gemergt, bricht der Befehl ohne -Force ab (Hinweis:
    nur mit -Force löschbar). Mit -Force wird er dennoch gelöscht — dann jedoch erst nach
    roter Warnung und Rückfrage (die Rückfrage bleibt in diesem Fall bestehen).

    Vor dem Löschen wird eine Zusammenfassung gezeigt und nachgefragt; -Force überspringt
    diese Rückfrage nur bei sicheren (gemergten) Löschungen. Der Repo-/Worktree-Root
    wird zur Aufruf-Zeit aufgelöst (Resolve-PeRoot), daher von jedem Ort im Repo
    aufrufbar.

.PARAMETER Branch
    Der zu löschende Branch (Kurzname, z. B. feature/foo). Ohne Angabe: Auswahl-Menü.

.PARAMETER Force
    Überspringt die Rückfrage bei sicheren (gemergten) Löschungen und erlaubt zusätzlich das
    Löschen nicht-gemergter Branches (`git branch -D`). Bei einem nicht-gemergten Branch
    bleibt die Rückfrage (mit Warnung) bestehen. Dirty-Worktree, Aufrufer-im-Worktree und
    der master-Guard bleiben auch mit -Force harte Abbrüche.

.FUNCTIONALITY
    rmbranch
#>
function Remove-PeBranch {
    [CmdletBinding()]
    param(
        [string] $Branch,
        [switch] $Force
    )

    $root = Resolve-PeRoot
    if (-not $root) { return }

    # Haupt-Worktree (master) als sicheren "anderswohin wechseln"-Hinweis ermitteln.
    $masterRoot = @(Get-PeWorktree -Root $root | Where-Object { $_.Branch -eq 'master' } |
        Select-Object -First 1).Path

    # Branch ermitteln: Argument oder Auswahl-Menü über alle lokalen Branches außer master.
    if (-not $Branch) {
        $branches = @(git -C $root for-each-ref --format='%(refname:short)' refs/heads |
            Where-Object { $_ -and $_ -ne 'master' })
        if ($branches.Count -eq 0) {
            Write-Host "Keine löschbaren Branches vorhanden (nur master)." -ForegroundColor Yellow
            return
        }
        $Branch = Show-PeSelectionMenu -Items $branches `
            -Header 'Branch löschen  (↑/↓ · Enter · Esc)'
        if (-not $Branch) { return }   # Esc / nicht-interaktiv → stiller Abbruch
    }

    # master-Guard: vor allem anderen, greift auch aus dem master-Worktree heraus.
    if ($Branch -ieq 'master') {
        Write-Host ""
        Write-Host "  master ist gesperrt und wird niemals gelöscht." -ForegroundColor Red
        Write-Host ""
        return
    }

    # Existiert der lokale Branch überhaupt?
    git -C $root show-ref --verify --quiet "refs/heads/$Branch"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Kein lokaler Branch '$Branch' vorhanden." -ForegroundColor Yellow
        return
    }

    # Infos sammeln.
    $worktree = @(Get-PeWorktree -Root $root | Where-Object { $_.Branch -eq $Branch } |
        Select-Object -First 1)
    $wtPath = if ($worktree) { $worktree.Path } else { $null }

    $callerInWorktree = $wtPath -and [string]::Equals(
        $root.TrimEnd('\', '/'), $wtPath.TrimEnd('\', '/'),
        [StringComparison]::OrdinalIgnoreCase)

    $dirty = $false
    if ($wtPath) {
        $status = git -C $wtPath status --porcelain
        $dirty = [bool]$status
    }

    $merged = [bool](@(git -C $root branch --merged master --format='%(refname:short)' |
        Where-Object { $_ -eq $Branch }))

    $remoteExists = $false
    $remoteRaw = git -C $root ls-remote --heads origin $Branch 2>$null
    if ($LASTEXITCODE -eq 0 -and $remoteRaw) { $remoteExists = $true }

    # Sicherheits-Vorprüfungen — harte Abbrüche, bevor irgendetwas gelöscht wird.
    if ($callerInWorktree) {
        Write-Host ""
        Write-Host "  Du stehst im zu löschenden Worktree:" -ForegroundColor Red
        Write-Host "    $wtPath" -ForegroundColor DarkGray
        $hint = if ($masterRoot) { $masterRoot } else { "einen anderen Worktree" }
        Write-Host "  Wechsle erst woanders hin (z. B. '$hint') und ruf den Befehl erneut auf." -ForegroundColor Yellow
        Write-Host ""
        return
    }

    if ($dirty) {
        Write-Host ""
        Write-Host "  Worktree ist nicht sauber:" -ForegroundColor Red
        Write-Host "    $wtPath" -ForegroundColor DarkGray
        Write-Host "  Bitte zuerst committen, stashen oder bereinigen (git clean)." -ForegroundColor Yellow
        Write-Host ""
        return
    }

    if (-not $merged -and -not $Force) {
        Write-Host ""
        Write-Host "  Branch '$Branch' ist nicht in master gemergt." -ForegroundColor Red
        Write-Host "  Löschen nur mit -Force möglich (pe rmbranch '$Branch' -Force)." -ForegroundColor Yellow
        Write-Host ""
        return
    }

    # Zusammenfassung + Rückfrage. -Force überspringt sie nur bei sicheren (gemergten)
    # Löschungen; bei einem nicht-gemergten Branch wird trotz -Force gewarnt und nachgefragt.
    $confirm = (-not $Force) -or (-not $merged)
    if ($confirm) {
        Write-Host ""
        Write-Host "  Löschen:" -ForegroundColor Cyan
        Write-Host "    Branch (lokal)  $Branch"
        Write-Host ("    Worktree        " + ($(if ($wtPath) { $wtPath } else { '(keiner)' })))
        Write-Host ("    Remote-Branch   " + ($(if ($remoteExists) { "origin/$Branch" } else { '(keiner)' })))
        if (-not $merged) {
            Write-Host ""
            Write-Host "  ACHTUNG: Branch ist NICHT in master gemergt — nicht gemergte Commits gehen verloren." -ForegroundColor Red
        }
        Write-Host ""
        $answer = Read-Host "  Wirklich löschen? [j/N]"
        if ($answer -notmatch '^(j|ja|y|yes)$') {
            Write-Host "Abgebrochen." -ForegroundColor DarkGray
            return
        }
    }

    # Ausführen. Reihenfolge: Worktree → lokaler Branch → Remote. Lokaler Branch immer via -D:
    # Die Merge-Prüfung gegen master ist oben bereits erfolgt (bzw. per -Force übersteuert).
    # `git branch -d` prüft dagegen gegen Upstream/HEAD und würde z. B. aus einem anderen
    # Feature-Worktree heraus scheitern — nachdem der Worktree schon entfernt wurde.
    if ($wtPath) {
        git -C $root worktree remove $wtPath
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Worktree konnte nicht entfernt werden — Abbruch." -ForegroundColor Red
            return
        }
    }

    git -C $root branch -D $Branch
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Lokaler Branch konnte nicht gelöscht werden — Abbruch." -ForegroundColor Red
        return
    }

    $remoteDeleted = $false
    if ($remoteExists) {
        try {
            git -C $root push origin --delete $Branch
            if ($LASTEXITCODE -ne 0) { throw "Exit $LASTEXITCODE" }
            $remoteDeleted = $true
        }
        catch {
            Write-Host "Remote-Branch 'origin/$Branch' konnte nicht gelöscht werden ($_)." -ForegroundColor Yellow
        }
    }

    git -C $root worktree prune

    Write-Host ""
    Write-Host "  Gelöscht:" -ForegroundColor Green
    Write-Host "    Branch (lokal)  $Branch"
    if ($wtPath)       { Write-Host "    Worktree        $wtPath" }
    if ($remoteDeleted) { Write-Host "    Remote-Branch   origin/$Branch" }
    Write-Host ""
}
