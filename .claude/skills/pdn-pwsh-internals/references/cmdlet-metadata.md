# Cmdlet metadata, binding, and ShouldProcess

Measured 2026-10-10 on PowerShell 7.6.6 (.NET 10.0.12) and Windows PowerShell 5.1.26100.9444, with one minimal `Add-Type` cmdlet per case and each edition in its own process. The sources are in `tests/repro/first-pass-2026-10-10/`. "Row" numbers refer to the analyzer catalog in the plan doc.

## Parameter metadata

The engine builds a command's parameter metadata the first time it needs it, such as on the first call. These errors surface there, not at `Import-Module`.

- **A parameter named like a common parameter** (row 2): `[Parameter] public SwitchParameter Verbose` throws `MetadataException`: "A parameter with the name 'Verbose' was defined multiple times for the command."
- **`ProgressAction`** (row 3): a parameter of that name runs on 5.1, and throws the same `MetadataException` on 7.6.6, where `ProgressAction` is a common parameter. The release that added it wasn't measured.
- **An alias that collides with a common parameter's alias** (row 4): `[Alias("ea")]` throws `MetadataException`: "The parameter alias cannot be specified because an alias with the name 'ea' was already defined multiple times for the command."
- **The other pairings and letter case** (measured 2026-10-11, same versions): each throws `MetadataException` at first use.
  - A parameter named `verbose`, in lowercase: "A parameter with the name 'Verbose' was defined multiple times for the command."
  - A parameter named `ea`: "The parameter 'ea' cannot be specified because it conflicts with the parameter alias of the same name for parameter 'ErrorAction'."
  - `[Alias("Verbose")]` on a parameter named `Thing`: "The parameter 'Verbose' cannot be specified because it conflicts with the parameter alias of the same name for parameter 'Thing'."
- **`WhatIf` as a parameter of the cmdlet** (measured 2026-10-11, same versions): with `SupportsShouldProcess = true`, it throws "A parameter with the name 'WhatIf' was defined multiple times for the command." Without it, the parameter is the cmdlet's own, and `-WhatIf` binds to it.
- `[System.Management.Automation.Cmdlet]::CommonParameters` and `OptionalCommonParameters` list the common parameters in both editions, and PowerShell 7's list includes `ProgressAction` (see `help.md`).

## Positions and setters

- **Two parameters with the same `Position` in one set** (row 5): `Get-Command -Syntax` silently drops one, showing `Test-DupPosition [[-Second] <string>] [<CommonParameters>]`, and a positional call throws `ParameterBindingException`: "Cannot bind positional parameters because no names were given." Calls by name weren't measured.
- **`[Parameter]` on a get-only property** (row 6): the parameter appears in the syntax, and passing it throws `ParameterBindingException`: "Cannot bind parameter 'Ro' to the target. Exception setting "Ro": "Expression must be writeable ..."". Nothing fails until a caller passes it.

## Parameter sets

- **Sets a call can't tell apart, with no `DefaultParameterSetName`** (row 7): with two sets that each hold one optional parameter, a call with no arguments throws `ParameterBindingException`: "Parameter set cannot be resolved using the specified named parameters." PowerShell 7.6.6 adds "One or more parameters issued cannot be used together or an insufficient number of parameters were provided."
- **A typo in a `ParameterSetName`** (row 8): `ByNmae` beside `ByName` creates a second set. `Get-Command` lists `ByName` and `ByNmae`, and `-Name x -Force`, where each parameter is in one of the two sets, throws the same "cannot be resolved" error.
- **A `DefaultParameterSetName` that no parameter declares** (row 14): accepted without an error. A call with no arguments runs with `ParameterSetName` set to the phantom name (`Bogus` in the repro).
- **Set names are case-sensitive** (measured 2026-10-11, same versions): a mandatory `-Name` in set `ByName` and a `-Force` switch in set `byname` make two sets. `Get-Command` lists both, and `-Name x -Force` throws the same "cannot be resolved" error as the `ByNmae` typo. A `DefaultParameterSetName` of `byname` beside a declared `ByName` doesn't match it either: `Get-Command` lists a third set, `byname`, with no parameters and marked as the default, and a call with no arguments runs in it.
- **The values `ParameterSetName` takes** (measured 2026-10-11, same versions): the set's name as the attribute spells it, such as `ByName`, or a phantom default's name. When no parameter names a set, `Get-Command` lists one set, `__AllParameterSets`, and `ParameterSetName` returns `__AllParameterSets` with or without arguments.

## BoundParameters

- **Keys are the parameters' own names** (row 15): after `-cn srv1`, where `cn` is an alias of `ComputerName`, `ContainsKey("cn")` is `False` and `ContainsKey("ComputerName")` is `True`.
- **Keys compare ignoring case** (measured 2026-10-11, same versions): after `-ComputerName x`, `ContainsKey("computername")` and `ContainsKey("COMPUTERNAME")` are both `True`. The dictionary's comparer is `OrdinalIgnoreCaseComparer` in PowerShell 7.6.6 and a case-insensitive `OrdinalComparer` in Windows PowerShell 5.1.
- **Keys go stale across pipeline records** (row 11): with two `ValueFromPipelineByPropertyName` parameters, piping `{Name=r1}`, `{Name=r2; Extra=b}`, `{Name=r3}` gave `Keys=Name` for r1, `Keys=Name,Extra` with `Extra=b` for r2, and `Keys=Name,Extra` with `Extra` reset to `$null` for r3. So `ContainsKey("Extra")` is `True` for a record that never supplied it.

## ShouldProcess

- **`ShouldProcess` without `SupportsShouldProcess`** (row 13): the call returns `True`, and `-WhatIf` throws `ParameterBindingException`: "A parameter cannot be found that matches parameter name 'WhatIf'."
- **`SupportsShouldProcess = true` with no `ShouldProcess` call** (row 12): `-WhatIf` is accepted and the action runs anyway. The repro printed "deleted for real".

## Writes from other threads

- **`WriteObject` from `Task.Run(...).Wait()` inside `ProcessRecord`** (row 9) throws `PSInvalidOperationException`: "The WriteObject and WriteError methods cannot be called from outside the overrides of the BeginProcessing, ProcessRecord, and EndProcessing methods, and they can only be called from within the same thread. Validate that the cmdlet makes these calls correctly, or contact Microsoft Customer Support Services."
- **`WriteObject` after `await Task.Delay(10)`** in a helper that `ProcessRecord` waits on with `.GetAwaiter().GetResult()` throws the same exception: the code after the `await` doesn't run on the pipeline thread.
- **`async void ProcessRecord()`** (row 10) with `await Task.Delay(50); WriteObject("x");`: neither edition crashed. PowerShell 7.6.6 printed `x` after the command had returned, while the script's next statement (`Start-Sleep -Seconds 2`) was still running. Windows PowerShell 5.1 never printed it, and its process exited with code 2 after the script ran to the end.

## Import

- **An unapproved verb** (row 1), such as `[Cmdlet("Fetch", "ReproThing")]`: `Import-Module` warns in both editions: "The names of some imported commands from the module '...' include unapproved verbs that might make them less discoverable. ..."

## Not a problem

- `[ValidateRange(1, 10)]` on a `string` parameter binds and validates `-S 5` in both editions. The catalog dropped this candidate.

## Engine APIs per edition

Checked 2026-10-10 by resolving each type or member in each edition.

| API | 5.1.26100 | 7.6.6 |
|---|---|---|
| `System.Management.Automation` assembly version | 3.0.0.0 | 7.6.0.500 |
| `IValidateSetValuesGenerator` | No | Yes |
| `PSCmdlet.PipelineStopToken` | No | Yes |
| `ArgumentCompletionsAttribute` | No | Yes |
| `IArgumentCompleter` | Yes | Yes |

## C# facts the generator relies on

- Attributes on both halves of a partial property land on the compiled property: reflection over a property declared `[DeclaredByAuthor] public partial string Name { get; set; }` and implemented with `[AddedByGenerator]` found both attributes (.NET SDK 10.0.401, 2026-10-10). Generated code can therefore add `[ValidateSet]` or `[ArgumentCompleter]` to a property that the author declared.

## In PoshDotNet

- Rows 2 to 6 fail every time the affected command or parameter is used, and rows 9 and 10 misbehave every time the code path runs, so those rules can default to Error. The others depend on what a caller passes or are silent, so they start as Warnings unless a later measurement shows a guaranteed failure.
- Row 3 differs between the editions, so its rule has to judge each compilation against the engine it references: a `net48` build against Windows PowerShell 5.1 is fine, and a PowerShell 7 build isn't.
- Row 11 means a generated "was this parameter bound for this record" check can't read `BoundParameters` for pipeline parameters. The engine resets those properties between records, so tracking has to tell a reset from a real bind.
- Rows 12 and 13 together mean a rule can tell, from the attribute and the calls alone, when `-WhatIf` would silently do nothing or not exist.
- Parameter names and aliases compare ignoring case, and all four pairings clash: name with name, name with alias, alias with name, and alias with alias. So rows 2 and 4 compare ignoring case and check every pairing. `WhatIf` and `Confirm` join the check only when the cmdlet sets `SupportsShouldProcess`.
- `BoundParameters` keys compare ignoring case, so row 15 flags an alias used as a key but not a parameter's name in other letter case.
- Set names compare case-sensitively, so rows 8, 14 and 16 treat a case-only difference as a different set: two parameters in `ByName` and `byname` are a typo in row 8's sense, a default of `byname` is a phantom set, and an ordinal comparison of `ParameterSetName` with `"byname"` is never true when only `ByName` is declared.
