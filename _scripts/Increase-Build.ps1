
$versioningScripts=Join-Path $PSScriptRoot Versioning.ps1
. $versioningScripts

$targetFiles | %{IncreaseBuild $_ -verbose}