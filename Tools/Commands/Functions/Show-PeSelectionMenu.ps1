<#
.SYNOPSIS
    Generisches interaktives Pfeiltasten-Menü über eine beliebige Item-Liste.

.DESCRIPTION
    ↑/↓ bewegen (mit Wrap-around), Enter wählt, Esc bricht still ab. Tippen springt
    per Type-ahead auf den ersten Treffer (Prefix bevorzugt, sonst Teilstring;
    case-insensitiv) — nur Selektion, kein automatisches Enter. Backspace nimmt das
    letzte Zeichen zurück, eine Tipp-Pause beginnt die Suche neu. Liefert das gewählte
    Item oder $null. Ohne echte Konsole (z. B. Pipeline, umgeleiteter Input) gibt es
    $null zurück, statt zu scheitern.

    Der Anzeigetext je Item wird über das -Label-Scriptblock gerendert ($_ = aktuelles
    Item). Gemeinsame Basis für Worktree- und Command-Auswahl (DRY).

.PARAMETER Items
    Die zur Auswahl stehenden Objekte.

.PARAMETER Label
    Scriptblock, der je Item ($_) den anzuzeigenden Text liefert. Default: "$_".

.PARAMETER Header
    Überschrift über dem Menü.
#>
function Show-PeSelectionMenu {
    param(
        [object[]] $Items,
        [scriptblock] $Label = { "$_" },
        [string] $Header = 'Auswählen  (↑/↓ · Enter · Esc)'
    )

    if (-not $Items) { return $null }
    if ([Console]::IsInputRedirected) { return $null }

    $labels = @($Items | ForEach-Object $Label)

    # Type-ahead: erster Prefix-Treffer, sonst erster Teilstring-Treffer (case-insensitiv);
    # -1, wenn nichts passt.
    function Find-SearchMatch([string] $needle) {
        for ($n = 0; $n -lt $labels.Count; $n++) {
            if (([string]$labels[$n]).StartsWith($needle, [StringComparison]::OrdinalIgnoreCase)) { return $n }
        }
        for ($n = 0; $n -lt $labels.Count; $n++) {
            if (([string]$labels[$n]).IndexOf($needle, [StringComparison]::OrdinalIgnoreCase) -ge 0) { return $n }
        }
        return -1
    }

    $selected = 0
    $search = ''
    $lastInput = [DateTime]::UtcNow
    $searchTimeoutMs = 1000
    $width = [Math]::Max(20, [Console]::WindowWidth - 1)
    Write-Host $Header -ForegroundColor DarkGray
    [Console]::CursorVisible = $false
    $result = $null
    try {
        $firstDraw = $true
        while ($true) {
            if (-not $firstDraw) {
                [Console]::SetCursorPosition(0, [Console]::CursorTop - $Items.Count)
            }
            $firstDraw = $false
            for ($i = 0; $i -lt $Items.Count; $i++) {
                if ($i -eq $selected) {
                    Write-Host (('> ' + $labels[$i]).PadRight($width)) -ForegroundColor Black -BackgroundColor Cyan
                }
                else {
                    Write-Host (('  ' + $labels[$i]).PadRight($width))
                }
            }
            $key = [Console]::ReadKey($true)
            $now = [DateTime]::UtcNow
            $done = $false
            switch ($key.Key) {
                'UpArrow'   { $selected = ($selected - 1 + $Items.Count) % $Items.Count; $search = '' }
                'DownArrow' { $selected = ($selected + 1) % $Items.Count; $search = '' }
                'Enter'     { $result = $Items[$selected]; $done = $true }
                'Escape'    { $result = $null; $done = $true }
                'Backspace' {
                    if ($search.Length -gt 0) {
                        $search = $search.Substring(0, $search.Length - 1)
                        $lastInput = $now
                        if ($search) {
                            $hit = Find-SearchMatch $search
                            if ($hit -ge 0) { $selected = $hit }
                        }
                    }
                }
                default {
                    $ch = $key.KeyChar
                    if (-not [char]::IsControl($ch)) {
                        # Nach einer Tipp-Pause die Suche neu beginnen, sonst anhängen.
                        if (($now - $lastInput).TotalMilliseconds -gt $searchTimeoutMs) { $search = '' }
                        $search += $ch
                        $lastInput = $now
                        $hit = Find-SearchMatch $search
                        if ($hit -ge 0) { $selected = $hit }
                    }
                }
            }
            if ($done) { break }
        }

        # Menü (Header + Items) wieder vom Bildschirm entfernen
        [Console]::SetCursorPosition(0, [Console]::CursorTop - $Items.Count - 1)
        for ($i = 0; $i -lt ($Items.Count + 1); $i++) {
            Write-Host (' ' * $width)
        }
        [Console]::SetCursorPosition(0, [Console]::CursorTop - $Items.Count - 1)
    }
    finally {
        [Console]::CursorVisible = $true
    }

    return $result
}
