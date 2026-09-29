<#
.SYNOPSIS
    Listet alle pe-Commands (Aufruf, Kurzbeschreibung) auf.

.DESCRIPTION
    Ermittelt die Befehle generisch über Get-PeCommandInfo und gibt sie in zwei Gruppen aus:

      * Repo-Commands → Aufruf über den pe-Dispatcher (`pe <token>`)
      * Navigation    → eigenständige Funktionen (z. B. pe:)

.FUNCTIONALITY
    help
#>
function Show-PeCommands {
    [CmdletBinding()]
    param()

    $all  = @(Get-PeCommandInfo)
    $repo = @($all | Where-Object { $_.Kind -eq 'Repo' } | ForEach-Object {
            [pscustomobject]@{ Name = $_.Name; Aufruf = "pe $($_.Token)"; Synopsis = $_.Synopsis }
        })
    $nav  = @($all | Where-Object { $_.Kind -eq 'Nav' } | ForEach-Object {
            [pscustomobject]@{ Name = $_.Name; Aufruf = $_.Name; Synopsis = $_.Synopsis }
        })

    $items       = @($repo + $nav)
    $nameWidth   = (@('Funktion') + $items.Name   | Measure-Object -Maximum -Property Length).Maximum
    $aufrufWidth = (@('Aufruf')   + $items.Aufruf | Measure-Object -Maximum -Property Length).Maximum
    $format      = "  {0,-$nameWidth} {1,-$aufrufWidth} {2}"

    function Write-Section {
        param([string] $Title, [object[]] $Items)

        if (-not $Items) { return }
        Write-Host ''
        Write-Host $Title -ForegroundColor Cyan
        foreach ($item in ($Items | Sort-Object Aufruf)) {
            Write-Host ($format -f $item.Name, $item.Aufruf, $item.Synopsis)
        }
    }

    Write-Host ''
    Write-Host 'ProjectExplorer-Commands' -ForegroundColor Green
    Write-Host ($format -f 'Funktion', 'Aufruf', 'Zweck') -ForegroundColor DarkGray
    Write-Section -Title 'Repo'       -Items $repo
    Write-Section -Title 'Navigation' -Items $nav
    Write-Host ''
    Write-Host 'Details: Tools/Commands/README.md' -ForegroundColor DarkGray
    Write-Host ''
}
