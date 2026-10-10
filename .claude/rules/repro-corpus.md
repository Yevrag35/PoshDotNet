---
description: How to write and run the PowerShell repro corpus that proves each analyzer rule's runtime failure
paths:
  - "tests/repro/**"
---

# Repro corpus

The corpus proves the runtime failure that each analyzer rule reports, in both editions. A rule doesn't ship without one (milestone M0 in the plan doc). `first-pass-2026-10-10/` holds the first pass, which reproduced 15 of the catalog's behaviors and ruled one out.

Adapted on 2026-10-10 from the repro and measurement guidance in ListFunctions' testing and engine-internals skills.

## Running a case

- Run each case in a new process for each edition: `pwsh -NoProfile -File ...` and `powershell.exe -NoProfile -ExecutionPolicy Bypass -File ...`. The types that `Add-Type` compiles stay loaded until the process exits, and a hung or crashed case can't spoil the next one.
- Use stable releases only: Windows PowerShell 5.1, and PowerShell 7.6 or a later stable release. The `pwsh` on PATH can be a preview. Stable PowerShell 7 installs to `$env:ProgramFiles\PowerShell\7`, and previews to `$env:ProgramFiles\PowerShell\7-preview`. Print `$PSVersionTable.PSVersion` in every run.
- To pass an array to a script, use `-Command "& '<script>' -Path a, b"`. Under `-File`, `-Path a,b` arrives as the single string `'a,b'`.

## Writing a case

- `Add-Type` in Windows PowerShell 5.1 compiles C# 5, so a case's C# can't use string interpolation, `?.`, `nameof`, pattern matching, expression-bodied members, or auto-property initializers. Read a non-public member through reflection, by the name it has in each edition.
- The scripts run in Windows PowerShell 5.1 too, so they can't use `??`, the ternary operator, or `&&` and `||`. Keep them ASCII, because 5.1 reads a UTF-8 file without a BOM as ANSI.
- Report the engine's own exception. Unwrap `CmdletInvocationException`, `AggregateException`, and `MethodInvocationException` to the innermost exception, and print its type name and the first line of its message, as `first-pass-2026-10-10/Run-Repro.ps1` does.
- When a failure needs a particular argument combination, the case has to use it, as catalog row 8 does with `-Name x -Force`.
- When the editions behave differently, report what each one did; don't merge them into one result.
- Pester runs each test inside a `try` block, where an error that ends a statement and one that ends the script both jump to `catch`. Run a case that depends on that difference in its own process or runspace.

## Recording results

- Record each result a rule depends on in the `pdn-pwsh-internals` skill, in `references/cmdlet-metadata.md` or the reference it belongs to, with both versions and the date.
- When a later PowerShell release changes a behavior, keep the old measurement, add the new one with its version, and revisit the rule's default severity.

## Where the corpus is heading

The first pass runs many cases from one source file. Milestone M0 calls for one minimal cmdlet per rule, with a Pester test that asserts the failure and its message in both editions, so that a PowerShell release that changes a behavior fails the corpus. Convert the first pass when the first rules are written.
