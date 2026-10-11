<#
.SYNOPSIS
	Runs the repro corpus's Pester tests in Windows PowerShell 5.1 and in PowerShell 7, each in a new process.

.DESCRIPTION
	Each edition gets its own process, because the types that Add-Type compiles stay loaded until the process exits.
	PowerShell 7 runs from its stable install folder, since the pwsh on PATH can be a preview. Each run prints the
	edition's version and Pester's, and the script exits with the number of editions whose run failed.

	Keep this script Windows PowerShell 5.1-compatible and ASCII.

.PARAMETER Path
	The test files or folders to run. The default is the whole corpus.

.PARAMETER Edition
	The editions to run: Desktop for Windows PowerShell 5.1, and Core for PowerShell 7. The default is both.

.PARAMETER PesterVersion
	The oldest Pester version to import. The corpus was written against Pester 6.2.0.

.EXAMPLE
	.\tests\repro\Invoke-ReproCorpus.ps1

.EXAMPLE
	.\tests\repro\Invoke-ReproCorpus.ps1 -Path .\tests\repro\PDN1005 -Edition Core
#>
[CmdletBinding()]
param(
	[string[]]$Path,

	[ValidateSet('Desktop', 'Core')]
	[string[]]$Edition = @('Desktop', 'Core'),

	[version]$PesterVersion = '6.2.0'
)

$ErrorActionPreference = 'Stop'

# Windows PowerShell 5.1 sets $PSScriptRoot only after it binds the parameters, so the default lives here.
if (-not $Path) {
	$Path = @($PSScriptRoot)
}

$executables = @{
	Desktop = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
	Core    = Join-Path $env:ProgramFiles 'PowerShell\7\pwsh.exe'
}

# The child's command holds no double quotes, because Windows PowerShell 5.1 doesn't escape them when it passes an
# argument to a native command, and the paths go in as single-quoted literals. -EncodedCommand would avoid the quoting,
# but it makes the child write host output as CLIXML when its output is redirected.
$literals = @($Path | ForEach-Object { "'" + ((Resolve-Path -LiteralPath $_).ProviderPath -replace "'", "''") + "'" }) -join ', '
$command = @(
	"`$ErrorActionPreference = 'Stop'"
	"Import-Module Pester -MinimumVersion '$PesterVersion'"
	"'== ' + `$PSVersionTable.PSEdition + ' ' + `$PSVersionTable.PSVersion + ', Pester ' + (Get-Module Pester).Version"
	"`$configuration = New-PesterConfiguration"
	"`$configuration.Run.Path = @($literals)"
	"`$configuration.Run.Exit = `$true"
	"`$configuration.Output.Verbosity = 'Detailed'"
	"Invoke-Pester -Configuration `$configuration"
) -join '; '

$failed = 0
foreach ($name in $Edition) {
	$executable = $executables[$name]
	if (-not (Test-Path -LiteralPath $executable)) {
		throw "Can't find $executable to run the $name edition."
	}

	& $executable -NoProfile -NonInteractive -ExecutionPolicy Bypass -Command $command
	if ($LASTEXITCODE -ne 0) {
		$failed++
	}
}

exit $failed
