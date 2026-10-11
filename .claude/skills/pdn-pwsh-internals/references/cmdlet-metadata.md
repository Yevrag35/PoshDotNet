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

- **Two parameters with the same `Position` in one set** (row 5): `Get-Command -Syntax` silently drops one, showing `Test-DupPosition [[-Second] <string>] [<CommonParameters>]`, and a positional call throws `ParameterBindingException`: "Cannot bind positional parameters because no names were given." The error ID is `AmbiguousPositionalParameterNoName`.
- **Which positions clash** (measured 2026-10-11, same versions, kept in `tests/repro/PDN1005/`):
  - A call that names both parameters binds, and so does a positional argument once the call names one of the two: `-First a b` binds `b` to `Second`.
  - Two parameters in the same named set clash. A clash in a set other than the default fails a positional call that the default set could take, unless a named argument has already ruled that set out.
  - An empty `ParameterSetName`, and `__AllParameterSets` in any letter case, mean every set, so such a parameter clashes with one that names no set. `ByName` and `byname` are two sets, and their parameters don't clash.
  - A parameter that names no set and one in a named set, at the same position, don't clash. One positional argument binds to both, and the call runs in the named set. A second positional argument then fails with "A positional parameter cannot be found that accepts argument 'two'." `Get-Command -Syntax` shows only one of the two.
  - A `ValueFromRemainingArguments` parameter at the same position doesn't clash: the other parameter takes the first argument, and it takes the rest.
  - One parameter at the same position in two sets doesn't clash.
  - Two parameters at `Position = -1` clash. `Get-Command -Syntax` throws `ArgumentOutOfRangeException` for a cmdlet with even one negative position, though a positional call to that cmdlet binds. `Position = int.MinValue` means no position.
- **`[Parameter]` on a get-only property** (row 6): the parameter appears in the syntax, and passing it throws `ParameterBindingException`: "Cannot bind parameter 'Ro' to the target. Exception setting "Ro": "Expression must be writeable ..."". Nothing fails until a caller passes it.

## Which members are parameters

Measured 2026-10-11, same versions, with the cases in `tests/repro/PDN1005/`.

- Public instance properties and fields with their own `[Parameter]` are parameters, including those a base class declares. An `internal` or `static` property with `[Parameter]` isn't one.
- A derived property with the same name and type hides the base class's parameter, whether it's `new` or an `override`. An override without its own `[Parameter]` isn't a parameter, and one with `[Parameter]` takes that attribute's settings.
- A derived property of another type, such as `new int First` over `string First`, doesn't hide the base parameter, and neither does a derived field over a base field. Both base parameters stay.
- PowerShell 7.4's source reads a class's `[Cmdlet]` with `inherit: false` and skips a class without a public parameterless constructor. Neither was measured.

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
- Row 5's rule, PDN1005, groups positions by set, compares set names case-sensitively, and treats "every set" as a set of its own. So two parameters clash only in the same named set, or when neither names a set. It skips `ValueFromRemainingArguments` parameters and `Position = int.MinValue`.
- A parameter that names no set beside one in a named set, at the same position, binds one argument to both without an error. That isn't a guaranteed failure, so it can't be an Error.
- The cmdlet model reads parameters by the rules in "Which members are parameters". Where a pairing wasn't measured, such as a derived field over a base property, it treats the base member as hidden, which can drop a parameter but never adds a diagnostic.
- Row 3 differs between the editions, so its rule has to judge each compilation against the engine it references: a `net48` build against Windows PowerShell 5.1 is fine, and a PowerShell 7 build isn't.
- Row 11 means a generated "was this parameter bound for this record" check can't read `BoundParameters` for pipeline parameters. The engine resets those properties between records, so tracking has to tell a reset from a real bind.
- Rows 12 and 13 together mean a rule can tell, from the attribute and the calls alone, when `-WhatIf` would silently do nothing or not exist.
- Parameter names and aliases compare ignoring case, and all four pairings clash: name with name, name with alias, alias with name, and alias with alias. So rows 2 and 4 compare ignoring case and check every pairing. `WhatIf` and `Confirm` join the check only when the cmdlet sets `SupportsShouldProcess`.
- `BoundParameters` keys compare ignoring case, so row 15 flags an alias used as a key but not a parameter's name in other letter case.
- Set names compare case-sensitively, so rows 8, 14 and 16 treat a case-only difference as a different set: two parameters in `ByName` and `byname` are a typo in row 8's sense, a default of `byname` is a phantom set, and an ordinal comparison of `ParameterSetName` with `"byname"` is never true when only `ByName` is declared.
