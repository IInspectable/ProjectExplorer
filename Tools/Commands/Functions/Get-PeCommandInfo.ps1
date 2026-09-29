<#
.SYNOPSIS
    Ermittelt die pe-Commands generisch aus den Functions-Dateien.

.DESCRIPTION
    Parst alle *.ps1 im Functions-Ordner per AST und liefert je Top-Level-Funktion ein
    Objekt { Name; Token; Synopsis; Kind }:

      * .FUNCTIONALITY gesetzt        → Kind 'Repo' (Token = .FUNCTIONALITY, Aufruf: pe <Token>)
      * kein Token, kein Bindestrich  → Kind 'Nav'  (Navigation, z. B. pe:)
      * kein Token, mit Bindestrich   → interner Helper (Verb-Noun) → wird ausgelassen

    Einzige Quelle der Wahrheit für die Befehlsübersicht (Show-PeCommands) und den
    pe-Dispatcher (Invoke-PeCommand).
#>
function Get-PeCommandInfo {
    [CmdletBinding()]
    param()

    foreach ($file in Get-ChildItem -Path $PSScriptRoot -Filter *.ps1) {
        $ast = [System.Management.Automation.Language.Parser]::ParseFile($file.FullName, [ref]$null, [ref]$null)

        # Nur Top-Level-Funktionen (searchNestedScriptBlocks = $false).
        $functions = $ast.FindAll(
            { param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] },
            $false)

        foreach ($fn in $functions) {
            $name     = $fn.Name
            $help     = $fn.GetHelpContent()
            $token    = if ($help -and $help.Functionality) { $help.Functionality.Trim() } else { '' }
            # Mehrzeilige .SYNOPSIS auf eine Zeile kollabieren (Tabellen-Layout).
            $synopsis = if ($help -and $help.Synopsis) { ($help.Synopsis -replace '\s+', ' ').Trim() } else { '' }

            if ($token) {
                [pscustomobject]@{ Name = $name; Token = $token; Synopsis = $synopsis; Kind = 'Repo' }
            }
            elseif ($name -notmatch '-') {
                [pscustomobject]@{ Name = $name; Token = ''; Synopsis = $synopsis; Kind = 'Nav' }
            }
            # sonst: interner Helper (Verb-Noun ohne Token) → nicht ausgeben
        }
    }
}
