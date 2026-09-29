<#
.SYNOPSIS
    Minor-Version erhöhen (X.Y.Z → X.Y+1.0) und das VSIX frisch bauen.

.DESCRIPTION
    Erhöht die Version in Version.props und im Quell-Manifest (Step-PeProductVersion) und
    führt anschließend `pe vsix` aus: Clean, Release-Build, Versionsprüfung im VSIX, Ablage
    unter deploy\.

.PARAMETER NoBuild
    Nur die Version erhöhen, nicht bauen.

.FUNCTIONALITY
    incminor
#>
function Invoke-PeIncreaseMinor {
    [CmdletBinding()]
    param(
        [switch] $NoBuild
    )

    $root = Resolve-PeRoot
    if (-not $root) { return }

    Step-PeProductVersion -Root $root -Part Minor | Out-Null
    if (-not $NoBuild) { Invoke-PePublish }
}
