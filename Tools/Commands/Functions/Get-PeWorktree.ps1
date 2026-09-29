<#
.SYNOPSIS
    Liefert alle Git-Worktrees des Repos als Objekte { Branch; Path }.

.DESCRIPTION
    Parst `git worktree list --porcelain` zu Objekten mit Kurzname-Branch
    (z. B. "feature/toolwindow-filter") bzw. "(detached)" bei losem HEAD und Windows-Pfad. Da
    "git worktree list" aus jedem Worktree dieselbe Gesamtliste liefert, genügt ein
    beliebiger Anker im Repo. Interner Helper für New-PeBranch/Remove-PeBranch (Verb-Noun
    ohne .FUNCTIONALITY → kein pe-Command).

.PARAMETER Root
    Anker-Pfad im Repo. Default: aus dem eigenen Speicherort abgeleitet.
#>
function Get-PeWorktree {
    [CmdletBinding()]
    param(
        [string] $Root = ((& git -C $PSScriptRoot rev-parse --show-toplevel 2>$null) -replace '/', '\')
    )

    if (-not $Root) { return }

    $current = $null
    git -C $Root worktree list --porcelain | ForEach-Object {
        if ($_ -like 'worktree *') {
            $current = [pscustomobject]@{
                Branch = '(detached)'
                Path   = ($_ -replace '^worktree ', '') -replace '/', '\'
            }
        }
        elseif ($_ -like 'branch *') {
            $current.Branch = $_ -replace '^branch refs/heads/', ''
        }
        elseif ($_ -eq '') {
            if ($current) { $current; $current = $null }
        }
    }
    # Letzter Block ohne abschließende Leerzeile
    if ($current) { $current }
}
