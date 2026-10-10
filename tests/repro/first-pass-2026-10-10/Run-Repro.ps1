param([Parameter(Mandatory)][string]$Source)
# Runs each repro cmdlet and prints one line per case. Must stay Windows PowerShell 5.1-compatible.
$ErrorActionPreference = 'Stop'
$types = Add-Type -TypeDefinition (Get-Content -Raw -LiteralPath $Source) -PassThru
$warn = $null
Import-Module -Assembly $types[0].Assembly -WarningVariable warn -WarningAction SilentlyContinue

function Show([string]$Name, [scriptblock]$Block) {
    try {
        $out = & $Block | ForEach-Object { "$_".Trim() }
        "[$Name] OK: " + (($out | Select-Object -First 4) -join ' | ')
    }
    catch {
        $e = $_.Exception
        while ($e.InnerException -and (
                $e -is [System.Management.Automation.CmdletInvocationException] -or
                $e -is [System.AggregateException] -or
                $e -is [System.Management.Automation.MethodInvocationException])) { $e = $e.InnerException }
        "[$Name] THROWS $($e.GetType().Name): " + ($e.Message -split "`r?`n")[0]
    }
}

"== $($PSVersionTable.PSEdition) $($PSVersionTable.PSVersion)"
"[Import warning] " + (($warn | ForEach-Object { "$_" }) -join ' | ')
Show 'VerboseParam' { Test-VerboseParam }
Show 'AliasEa' { Test-AliasEa -Thing x }
Show 'ProgressActionParam' { Test-ProgressActionParam -ProgressAction x }
Show 'NoSupport (plain)' { Test-NoSupport }
Show 'NoSupport -WhatIf' { Test-NoSupport -WhatIf }
Show 'SupportsButNoCall -WhatIf' { Remove-ReproThing -WhatIf }
Show 'NoDefaultSet (no args)' { Test-NoDefaultSet }
Show 'SetTypo sets' { (Get-Command Test-SetTypo).ParameterSets | ForEach-Object Name }
Show 'SetTypo -Name x -Force' { Test-SetTypo -Name x -Force }
Show 'AliasBoundKey -cn' { Test-AliasBoundKey -cn srv1 }
Show 'WriteFromTask' { Test-WriteFromTask }
Show 'WriteAfterAwait' { Test-WriteAfterAwait }
Show 'DupPosition syntax' { Get-Command Test-DupPosition -Syntax }
Show 'DupPosition positional' { Test-DupPosition one }
Show 'RangeOnString' { Test-RangeOnString -S 5 }
Show 'GetOnlyParam' { Test-GetOnlyParam -Ro x }
Show 'PerRecord' { [pscustomobject]@{ Name = 'a'; Extra = 'b' }, [pscustomobject]@{ Name = 'c' } | Test-PerRecord }
Show 'DefaultMissing (no args)' { Test-DefaultMissing }
