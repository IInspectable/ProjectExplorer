
$versioningScripts=Join-Path $PSScriptRoot Versioning.ps1
. $versioningScripts

$targetFiles | %{IncreaseMinor $_ -verbose}
