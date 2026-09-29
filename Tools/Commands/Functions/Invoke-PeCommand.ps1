<#
.SYNOPSIS
    Dispatcher für die ProjectExplorer-Repo-Commands — Alias `pe`.

.DESCRIPTION
    `pe <command>` führt den passenden Repo-Command aus (z. B. `pe build`, `pe vsix`); der
    Sub-Command (Token) wird generisch über Get-PeCommandInfo aufgelöst. Restliche
    Argumente werden über $args unverändert an die Zielfunktion durchgereicht
    (`pe build -Configuration Release`, `pe newbranch foo -Force`).

    Bewusst KEINE advanced function (kein [CmdletBinding]): nur dann befüllt PowerShell $args
    mit den nicht an $Command gebundenen Argumenten, und nur das automatische $args reicht beim
    Splatten (@args) benannte Parameter und Switches korrekt durch. Die Tab-Completion hängt
    deshalb an Register-ArgumentCompleter (am Dateiende).

    Ohne Argument (`pe` + Enter) erscheint eine interaktive Auswahlliste der Repo-Commands.
    Ohne echte Konsole (Input umgeleitet) wird stattdessen die statische Übersicht ausgegeben.

.PARAMETER Command
    Der Sub-Command (Token), z. B. build, clean, vsix, install, incbuild.
#>
function Invoke-PeCommand {
    param(
        [string] $Command
    )

    if (-not $Command) {
        $repo = @(Get-PeCommandInfo | Where-Object { $_.Kind -eq 'Repo' } | Sort-Object Token)
        $tokenWidth = ($repo.Token | Measure-Object -Maximum -Property Length).Maximum
        $pick = Show-PeSelectionMenu -Items $repo -Header 'Command wählen  (↑/↓ · Enter · Esc)' `
            -Label { "{0,-$tokenWidth} {1}" -f $_.Token, $_.Synopsis }
        if ($null -eq $pick) {
            if ([Console]::IsInputRedirected) { Show-PeCommands }
            return
        }
        & $pick.Name
        return
    }

    $info = Get-PeCommandInfo |
        Where-Object { $_.Kind -eq 'Repo' -and $_.Token -eq $Command } |
        Select-Object -First 1
    if (-not $info) {
        Write-Host "Unbekannter Command '$Command'." -ForegroundColor Yellow
        Show-PeCommands
        return
    }

    & $info.Name @args
}

Set-Alias pe Invoke-PeCommand

Register-ArgumentCompleter -CommandName Invoke-PeCommand -ParameterName Command -ScriptBlock {
    param($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameters)
    Get-PeCommandInfo |
        Where-Object { $_.Kind -eq 'Repo' -and $_.Token -like "$wordToComplete*" } |
        Sort-Object Token |
        ForEach-Object {
            [System.Management.Automation.CompletionResult]::new($_.Token, $_.Token, 'ParameterValue', $_.Synopsis)
        }
}
