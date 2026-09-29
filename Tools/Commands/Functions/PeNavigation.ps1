<#
.SYNOPSIS
    In den Root eines (gewählten) Worktrees wechseln.

.DESCRIPTION
    Listet die Git-Worktrees des Repos und wechselt per Set-Location in den gewählten.
    Bei mehreren Worktrees erscheint ein Pfeiltasten-Menü (Show-PeSelectionMenu, ↑/↓ · Enter ·
    Esc); ein optionales Branch-Argument filtert direkt (Teilstring, case-insensitiv,
    z. B. "pe: filter"). Genau ein Treffer springt ohne Menü; kein Treffer meldet sich
    freundlich (kein Stacktrace).

    Navigations-Command: kein `.FUNCTIONALITY`-Token und kein Bindestrich im Namen, daher
    klassifiziert Get-PeCommandInfo die Funktion als 'Nav' und sie erscheint in der
    Übersicht (`pe help`) unter "Navigation". Datenquelle ist der Helper Get-PeWorktree, der
    aus jedem Worktree dieselbe Gesamtliste liefert.

.PARAMETER Branch
    Optionaler Filter auf den Worktree-Branch (Teilstring, case-insensitiv).
#>
function pe: {
    [CmdletBinding()]
    param([string] $Branch)

    $worktrees = @(Get-PeWorktree)
    if ($worktrees.Count -eq 0) {
        Write-Warning 'Keine Worktrees gefunden.'
        return
    }

    if ($Branch) {
        $hits = @($worktrees | Where-Object { $_.Branch -like "*$Branch*" })
        if ($hits.Count -eq 0) {
            Write-Warning "Kein Worktree passt zu '$Branch'."
            return
        }
        # Mehrere Treffer: Menü auf die gefilterte Liste einschränken.
        $worktrees = $hits
    }

    $wt = if ($worktrees.Count -eq 1) {
        $worktrees[0]
    }
    else {
        Show-PeSelectionMenu -Items $worktrees -Label { $_.Branch } `
            -Header 'Worktree wählen  (↑/↓ · Enter · Esc)'
    }
    if (-not $wt) { return }

    if (-not (Test-Path $wt.Path)) {
        Write-Warning "Pfad existiert nicht: '$($wt.Path)'."
        return
    }
    Set-Location $wt.Path
}
