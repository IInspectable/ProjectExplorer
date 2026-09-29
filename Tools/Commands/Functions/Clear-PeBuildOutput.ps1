<#
.SYNOPSIS
    Löscht alle Build-Artefakte (bin, obj, deploy) — Quellcode bleibt unangetastet.

.DESCRIPTION
    Entfernt die `bin`- und `obj`-Ordner aller Projektverzeichnisse im Repo-Root (auch Reste
    entfernter Projekte, z. B. ProjectExplorer.Extension2022) sowie den Ordner `deploy\`. Damit
    baut der nächste Build garantiert von Grund auf — ohne veraltete Zwischenstände im obj-Ordner,
    die sonst z. B. eine alte VSIX-Version durchreichen können.

    Anders als `pe undo` fasst der Befehl weder getrackte noch untracked Quelldateien an.
    Gelockte Dateien (z. B. weil die Experimental-Instanz von Visual Studio läuft) führen zu
    einem Abbruch mit Hinweis.

.FUNCTIONALITY
    clean
#>
function Clear-PeBuildOutput {
    [CmdletBinding()]
    param()

    $root = Resolve-PeRoot
    if (-not $root) { return }

    $targets = @(
        Get-ChildItem -Path $root -Directory |
            Where-Object { $_.Name -notin '.git', '.vs', 'Tools' } |
            ForEach-Object { Join-Path $_.FullName 'bin'; Join-Path $_.FullName 'obj' }
        Join-Path $root 'deploy'
    ) | Where-Object { Test-Path $_ }

    if (-not $targets) {
        Write-Host "  Nichts zu löschen." -ForegroundColor DarkGray
        return
    }

    foreach ($dir in $targets) {
        Write-Host "  Lösche $($dir.Substring($root.Length + 1))" -ForegroundColor DarkGray
        try {
            Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction Stop
        }
        catch {
            throw "Konnte '$dir' nicht löschen ($($_.Exception.Message)). Läuft noch Visual Studio bzw. die Experimental-Instanz?"
        }
    }

    Write-Host "  Build-Artefakte gelöscht." -ForegroundColor Green
}
