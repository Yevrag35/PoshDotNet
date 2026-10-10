---
name: pdn-pwsh-internals
description: Measured PowerShell engine behavior that PoshDotNet's analyzers, code fixes, and generated code depend on, in Windows PowerShell 5.1 and PowerShell 7. Covers how the engine builds a cmdlet's parameter metadata (common-parameter and alias clashes, duplicate positions, get-only parameters), resolves parameter sets, keeps `BoundParameters` across pipeline records, handles ShouldProcess and writes from other threads, which engine APIs each edition lacks, how pipeline input and `-InputObject` bind, when it calls `GetDynamicParameters`, what an argument completer receives, where Get-Help finds a binary module's help and what it shows, PSObject and AutomationNull traps, how errors from script blocks end a statement or a script, what converting `$null` gives, how Windows PowerShell 5.1 resolves a module's assemblies, and the traps in measuring any of it. Use it before you write or change a rule, a code fix, or generated code that depends on engine behavior, before you set a rule's default severity, before you answer a question about how PowerShell behaves in these areas, and before you run an experiment to find out.
---

# PowerShell internals for PoshDotNet

PoshDotNet's rules report mistakes that the PowerShell engine reveals only at import, at bind time, in the middle of a pipeline, or never. Each rule encodes a fact about the engine, and the documentation leaves most of those facts out. This skill records them as measured in Windows PowerShell 5.1.26100 and PowerShell 7.6.6, with the date of each measurement. A fact holds in both editions unless it says otherwise.

- Trust a measurement here over recollection. When a rule depends on a fact that a newer PowerShell release could change, measure it again.
- A rule defaults to Error only when a measurement here shows a guaranteed failure.
- When you measure something that a rule or generated code could depend on, add it to the reference it belongs to, with the versions and the date, instead of saving it as a memory. Say what it means for PoshDotNet beside it.
- Don't point to working notes, such as plans or review lists, which may not stay in the repo. State the fact, the rule, or the reason itself.

`references/cmdlet-metadata.md` was measured for this project. The other references come from Mike's ListFunctions repository (copied 2026-10-10). Their measurements are general engine behavior, though some name ListFunctions' own cmdlets and types as the place they were measured, and each one ends with what it means for PoshDotNet.

## References

Read the reference for the area you're working in.

- `references/cmdlet-metadata.md`: the first repro pass behind the analyzer catalog. Common-parameter and alias clashes, `ProgressAction`, duplicate positions, get-only parameters, parameter-set resolution, phantom default sets, `BoundParameters` keys (aliases, and stale keys across pipeline records), ShouldProcess with and without `SupportsShouldProcess`, writes from other threads and from `async void`, unapproved verbs, which engine APIs each edition lacks, and how partial properties merge attributes. Read it before you write or change any rule in the catalog, or set a rule's severity.
- `references/binding.md`: how pipeline input and `-InputObject` bind to `[object]` and `[object[]]` parameters, which values `LanguagePrimitives.GetEnumerator` enumerates, when PowerShell calls `GetDynamicParameters`, and how `InvokeWithContext` binds a script block's `param()` block. Read it before you write a rule about pipeline parameters or dynamic parameters, or generate dynamic-parameter code.
- `references/completion.md`: what PowerShell passes an argument completer for a quoted, bracketed, or comma-split argument, what it does when a completer returns `null` or nothing, and how type-name completion behaves, including in Windows PowerShell 5.1, where `CompletionCompleters.CompleteType` throws. Read it before you generate or check an argument completer.
- `references/help.md`: where Get-Help looks for a binary module's help file, what it shows from the file as written, what it does without one, the help objects that a check can compare, and what `Update-Help` does with `HelpInfoURI`. Read it before you write a help check.
- `references/psobject.md`: PowerShell 7's `PSObject(int)` constructor, each edition's private `PSObject` members, how to tell a custom object from a wrapped value, and where `AutomationNull.Value` reaches C# code. Read it before a rule or generated code touches `PSObject`.
- `references/errors.md`: how a written error, a `throw`, a failed method call, and `break` in a script block end the statement or the whole script, which exception types carry them, and what wrapping them changes. Read it before a rule judges how a cmdlet reports a failure.
- `references/conversions.md`: what converting `$null` gives for each kind of element type. Read it before generated code converts a parameter value.
- `references/assembly-loading.md`: how Windows PowerShell 5.1 resolves a module's dependencies, what PowerShell does on each import, why a dependency's type names don't resolve right after an import, and the PowerShell 7 version facts. Read it before anything PoshDotNet emits could add a runtime dependency, or before you chase a `FileNotFoundException` or `FileLoadException` in Windows PowerShell 5.1.

## Measuring PowerShell behavior

- Measure in both editions, on stable releases, and record the versions and the date with each result.
- Run each experiment in a new process, with `pwsh -NoProfile -Command ...` or `powershell.exe -NoProfile -Command ...`. A process keeps what `Add-Type` compiled and the DLLs it loaded until it exits, and PowerShell 7 can't load two assemblies with the same name into one process (see `references/assembly-loading.md`).
- To pass an array to a script, use `-Command "& '<script>' -Path a, b"`. Under `-File`, `-Path a,b` arrives as the single string `'a,b'`.
- `Add-Type` in Windows PowerShell 5.1 compiles C# 5, so scratch C# that runs in both editions can't use pattern matching such as `is PSObject p`, string interpolation, `?.`, or `nameof`. Read a non-public member through reflection, by the name it has in each edition.
- In script, `$psObject.BaseObject` reads a member of the wrapped object, which is usually `$null`. Use `$psObject.psobject.BaseObject`.
- Pester runs each test inside a `try` block, where an error that ends a statement and one that ends the script both jump to `catch`. To see how an error ends a script, run it in a new runspace or process (see `references/errors.md`).
- When an experiment should show that a change makes a difference, also run it against the code from before the change, and check that the difference shows.
- The repro corpus in `tests/repro/` keeps the experiments that rules depend on as runnable cases. `.claude/rules/repro-corpus.md` covers how to write and run them.
