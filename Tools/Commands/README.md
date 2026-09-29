# ProjectExplorer-Commands

PowerShell-Befehle für wiederkehrende Aufgaben im Repo: Bauen, VSIX erzeugen, Version erhöhen,
installieren. Sie laufen über einen Dispatcher mit dem Alias **`pe`**. Die Sub-Commands leitet er
aus den Dateien in `Functions/` ab. Jede Funktion mit einer `.FUNCTIONALITY <token>`-Help wird
automatisch zu `pe <token>`, inklusive Tab-Completion, Auswahlmenü und Übersicht.

## Setup (einmalig)

Voraussetzungen: Visual Studio 2026 mit dem Workload „Visual Studio-Extensionentwicklung“
(MSBuild und VSIXInstaller werden per vswhere gefunden) und PowerShell 7. Für `pe fixenc`
zusätzlich das .NET 10 SDK.

Der Loader muss per Dot-Sourcing geladen werden, damit die Funktionen und der Alias `pe` in der
Session bleiben. Für eine einzelne Session reicht:

```powershell
. "C:\ws\git\ProjectExplorer\Tools\Commands\Import-PeCommands.ps1"
```

### Dauerhaft über das Profil

Die Repos `ProjectExplorer`, `Nav.Language.Extensions` und `Mfv-Peissenberg.Website` folgen
derselben Konvention: `<Repo>\Tools\Commands\Import-*Commands.ps1`. Statt jedes Repo einzeln
einzutragen, lädt ein generischer Loader in `$PROFILE` alle Toolsets unter `C:\ws\git`, auch
künftige:

```powershell
# Toolsets aller Repos laden (Konvention: <Repo>\Tools\Commands\Import-*Commands.ps1),
# z. B. w (Website), nav (Nav.Language.Extensions), pe (ProjectExplorer).
# Nur Haupt-Repos (.git ist ein Ordner), keine Worktrees (.git ist dort eine Datei):
# die Commands kommen damit immer aus der master-Kopie, nicht aus einem Feature-Branch.
# foreach statt ForEach-Object, damit das Dot-Sourcing im globalen Scope landet.
foreach ($repo in Get-ChildItem C:\ws\git -Directory) {
    if (-not (Test-Path (Join-Path $repo.FullName '.git') -PathType Container)) { continue }
    foreach ($loader in Get-ChildItem (Join-Path $repo.FullName 'Tools\Commands\Import-*Commands.ps1') -ErrorAction SilentlyContinue) {
        . $loader.FullName
    }
}
Remove-Variable repo, loader -ErrorAction SilentlyContinue
```

Die Commands stammen aus dem Branch, der im Haupt-Repo gerade ausgecheckt ist. Neue oder
geänderte Commands auf einem Feature-Branch wirken also erst nach dem Merge, außer man lädt sie
im Worktree per Hand nach.

Die Commands lösen ihren Repo-/Worktree-Root zur Aufruf-Zeit auf (`git rev-parse --show-toplevel`).
Sie funktionieren aus jedem Unterordner und treffen bei mehreren Worktrees den, in dem man steht.

Alle Funktionen tragen das Präfix `Pe` (`Invoke-PeBuild`, `Resolve-PeRoot` …). So kommen sie sich
nicht mit den gleichartigen Commands anderer Repos in die Quere, wenn mehrere Loader im selben
Profil stehen.

## Benutzung

```powershell
pe                      # interaktives Menü (↑/↓ · Enter · Esc)
pe help                 # statische Übersicht aller Commands
pe <TAB>                # Tab-Completion der Tokens
pe build -Configuration Release -Clean   # benannte Parameter/Switches werden durchgereicht
```

## Commands

| Token       | Funktion               | Zweck                                                                    |
|-------------|------------------------|--------------------------------------------------------------------------|
| `build`     | Invoke-PeBuild         | Restore + Build per MSBuild (vswhere), danach Versionsprüfung im VSIX. `-Configuration`, `-Clean`. |
| `clean`     | Clear-PeBuildOutput    | `bin`, `obj` aller Projekte und `deploy\` löschen. Quellcode bleibt unangetastet. |
| `vsix`      | Invoke-PePublish       | `clean` + Release-Build + VSIX nach `deploy\ProjectExplorer-<Version>.vsix`. |
| `install`   | Install-PeExtension    | VSIX aus `deploy\` per VSIXInstaller installieren.                       |
| `version`   | Show-PeVersion         | Version in Version.props, Quell-Manifest und allen gebauten VSIX anzeigen. |
| `incbuild`  | Invoke-PeIncreaseBuild | `X.Y.Z` → `X.Y.Z+1`, danach `vsix` (`-NoBuild` unterdrückt den Build).   |
| `incminor`  | Invoke-PeIncreaseMinor | `X.Y.Z` → `X.Y+1.0`, danach `vsix`.                                      |
| `incmajor`  | Invoke-PeIncreaseMajor | `X.Y.Z` → `X+1.0.0`, danach `vsix`.                                      |
| `undo`      | Invoke-PeUndo          | Alle lokalen Änderungen verwerfen + `git clean` (mit Rückfrage).         |
| `newbranch` | New-PeBranch           | Branch + danebenliegenden Worktree anlegen und hineinwechseln.           |
| `rmbranch`  | Remove-PeBranch        | Branch + Worktree + Remote-Branch löschen (mit Schutzmechanismen).       |
| `fixenc`    | Repair-PeEncoding      | Quelldateien auf UTF-8 mit BOM bringen (braucht .NET 10 SDK).            |
| `help`      | Show-PeCommands        | Übersicht aller Commands.                                                |

Navigation: `pe:` wechselt in den Root eines Worktrees (Pfeiltasten-Menü, optionaler Branch-Filter).

Die Tabelle ist nur eine Lese-Hilfe. Quelle der Wahrheit sind die `.FUNCTIONALITY`-Tokens in
`Functions/`. Detail-Hilfe je Command via `Get-Help <Funktion> -Full`.

## Versionierung

`Version.props` (`<ProductVersion>`) ist die einzige Quelle der Wahrheit. Beim Build schreibt
`UpdateProductVersion.targets` die Version in `ThisAssembly.generated.cs` (Assembly- und
Dateiversion) und per XmlPoke in `source.extension.vsixmanifest` (VSIX-Version).

Früher kam eine Versionserhöhung manchmal nicht im VSIX an. Drei Maßnahmen verhindern das jetzt:

1. `incbuild`/`incminor`/`incmajor` schreiben die neue Version in Version.props **und** direkt ins
   Quell-Manifest und bauen danach per `vsix` von Grund auf (Clean + Release-Build).
2. `build` liest nach jedem Build die Version aus dem fertigen VSIX und bricht ab, wenn sie von
   Version.props abweicht.
3. `Directory.Build.props` schaltet den Fast-Up-To-Date-Check von Visual Studio ab. Der kennt
   Version.props nicht als Eingabe und hat nach einer Versionserhöhung den Build übersprungen.

`pe version` zeigt, wo welche Version steht. Abweichungen sind gelb markiert.

## Eigene Commands hinzufügen

1. Neue Datei `Functions/Verb-PeNoun.ps1` mit einer Funktion `Verb-PeNoun`.
2. Comment-based Help mit `.SYNOPSIS` (erscheint in der Übersicht) und `.FUNCTIONALITY <token>`
   (macht sie als `pe <token>` aufrufbar).
3. Root via `Resolve-PeRoot` auflösen. Übersicht und Tab-Completion ziehen automatisch nach.

Funktionen **ohne** `.FUNCTIONALITY` mit Bindestrich-Namen gelten als interne Helper
(`Resolve-PeRoot`, `Resolve-PeMsBuild`, `Get-PeWorktree`, `Get-PeProductVersion`,
`Get-PeVsixVersion`, `Step-PeProductVersion`) und tauchen nicht als Command auf.
