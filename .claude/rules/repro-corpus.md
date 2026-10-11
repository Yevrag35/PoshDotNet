---
description: How to write and run the PowerShell repro corpus that proves each analyzer rule's runtime failure
paths:
  - "tests/repro/**"
---

# Repro corpus

The corpus proves the runtime failure that each analyzer rule reports, in both editions. A rule doesn't ship without one (milestone M0 in the plan doc). Each rule's cases live in a folder named for its diagnostic ID, such as `PDN1005/`, which holds minimal cmdlets in one C# 5 file and a Pester test, `<ID>.Tests.ps1`. `first-pass-2026-10-10/` holds the first pass, which reproduced 15 of the catalog's behaviors and ruled one out.

Adapted on 2026-10-10 from the repro and measurement guidance in ListFunctions' testing and engine-internals skills.

## Running the corpus

- Run `tests/repro/Invoke-ReproCorpus.ps1` from either edition. It runs every `*.Tests.ps1` under Pester 6.2.0 or later, once in Windows PowerShell 5.1 and once in PowerShell 7, each in a new process, and exits with the number of editions that failed. `-Path` narrows it to a rule's folder, and `-Edition Desktop` or `-Edition Core` to one edition.
- Each edition needs its own process because the types that `Add-Type` compiles stay loaded until the process exits, and a hung or crashed case can't spoil the next one.
- Use stable releases only: Windows PowerShell 5.1, and PowerShell 7.6 or a later stable release. The `pwsh` on PATH can be a preview, so the runner starts PowerShell 7 from `$env:ProgramFiles\PowerShell\7`. Each run prints the edition's version and Pester's.
- For a one-off experiment outside Pester, run a script in each edition with `pwsh -NoProfile -File ...` and `powershell.exe -NoProfile -ExecutionPolicy Bypass -File ...`, as `first-pass-2026-10-10/` does. To pass an array to a script, use `-Command "& '<script>' -Path a, b"`. Under `-File`, `-Path a,b` arrives as the single string `'a,b'`.

## Writing a case

- `Add-Type` in Windows PowerShell 5.1 compiles C# 5, so a case's C# can't use string interpolation, `?.`, `nameof`, pattern matching, expression-bodied members, or auto-property initializers. Read a non-public member through reflection, by the name it has in each edition.
- The scripts run in Windows PowerShell 5.1 too, so they can't use `??`, the ternary operator, or `&&` and `||`, and `$PSScriptRoot` is empty in a `param` block's default values there. Keep them ASCII, because 5.1 reads a UTF-8 file without a BOM as ANSI.
- Every rule's cmdlets load into one process, so start each noun with the rule's ID, as in `Test-Pdn1005NoSetPair`, and give each rule's C# its own namespace.
- Assert the engine's own exception: its type, its error ID, and the first line of its message. Pester 6 compares `-ErrorId` and `-ExpectedMessage` with `-like` and adds no wildcards, so match an error ID as `-ErrorId 'AmbiguousPositionalParameterNoName,*'`, since the command's name follows the comma. Outside Pester, unwrap `CmdletInvocationException`, `AggregateException`, and `MethodInvocationException` to the innermost exception, as `first-pass-2026-10-10/Run-Repro.ps1` does.
- Cover the near misses a rule leaves alone as well as the cases it reports, and assert what each one does instead of failing. Then a release that changes either side fails the corpus.
- When a failure needs a particular argument combination, the case has to use it, as catalog row 8 does with `-Name x -Force`.
- When the editions behave differently, assert what each one did; don't merge them into one result.
- Pester runs each test inside a `try` block, where an error that ends a statement and one that ends the script both jump to `catch`. Run a case that depends on that difference in its own process or runspace.

## Recording results

- Record each result a rule depends on in the `pdn-pwsh-internals` skill, in `references/cmdlet-metadata.md` or the reference it belongs to, with both versions and the date.
- When a later PowerShell release changes a behavior, keep the old measurement, add the new one with its version, and revisit the rule's default severity.

## Where the corpus is heading

The first pass runs many cases from one source file. Each rule moves its row's case into its own folder when the rule is written, as PDN1005 did with row 5 on 2026-10-11, and the first pass stays until every case has moved.
