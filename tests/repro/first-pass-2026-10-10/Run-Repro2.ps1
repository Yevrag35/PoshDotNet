param([Parameter(Mandatory)][string]$Source, [Parameter(Mandatory)][ValidateSet('PerRecord', 'AsyncVoid')][string]$Case)
$ErrorActionPreference = 'Stop'
$types = Add-Type -TypeDefinition (Get-Content -Raw -LiteralPath $Source) -PassThru
Import-Module -Assembly $types[0].Assembly
"== $($PSVersionTable.PSEdition) $($PSVersionTable.PSVersion) [$Case]"
if ($Case -eq 'PerRecord') {
    [pscustomobject]@{ Name = 'r1' },
    [pscustomobject]@{ Name = 'r2'; Extra = 'b' },
    [pscustomobject]@{ Name = 'r3' } | Test-PerRecord2
}
else {
    Test-AsyncVoid
    Start-Sleep -Seconds 2
    'process survived'
}
