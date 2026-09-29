<#
.SYNOPSIS
    Einziger Einstiegspunkt für die ProjectExplorer-Commands — per Dot-Sourcing aus dem
    PowerShell-Profil laden.

.DESCRIPTION
    Lädt alle Command-Funktionen aus dem Unterordner `Functions` (eine Datei pro Command).
    Jede Funktion löst ihren Repo-/Worktree-Root selbst zur Aufruf-Zeit auf (siehe
    Resolve-PeRoot), daher lassen sich die Befehle von jedem Ort im Repo aufrufen.

    Profil-Setup (einmalig, außerhalb des Repos), z. B. in $PROFILE:

        . "C:\ws\git\ProjectExplorer\Tools\Commands\Import-PeCommands.ps1"

    Eine Übersicht der bereitgestellten Funktionen und Aliase steht in README.md.
#>

Get-ChildItem -Path (Join-Path $PSScriptRoot 'Functions') -Filter *.ps1 |
    ForEach-Object { . $_.FullName }
