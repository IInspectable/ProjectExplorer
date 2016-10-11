
$versioningScripts=Join-Path $PSScriptRoot Versioning.ps1
. $versioningScripts

$targetFiles | %{IncreaseMajor $_ -verbose}