# First repro pass (2026-10-10)

The first measurements behind the analyzer catalog in the plan doc, run on PowerShell 7.6.6 (.NET 10.0.12) and Windows PowerShell 5.1.26100.9444. The results, with the exact messages, are in `.claude/skills/pdn-pwsh-internals/references/cmdlet-metadata.md`.

| File | What it shows |
|---|---|
| `Repro.cs` and `Run-Repro.ps1` | One minimal cmdlet per catalog behavior, compiled with `Add-Type`. The script prints one line per case: `OK` with the output, or `THROWS` with the engine's exception type and the first line of its message. |
| `Repro2.cs` and `Run-Repro2.ps1` | `-Case PerRecord`: stale `BoundParameters` keys across pipeline records. `-Case AsyncVoid`: an `async void ProcessRecord` that writes after an `await`. |
| `PartialAttrs.cs` | Attributes on both halves of a C# partial property land on the compiled property. A .NET 10 file-based app. |

The C# in `Repro.cs` and `Repro2.cs` is C# 5 on purpose: `Add-Type` in Windows PowerShell 5.1 compiles with the legacy compiler.

Run each edition in its own process, from this folder:

```powershell
pwsh -NoProfile -File .\Run-Repro.ps1 -Source .\Repro.cs
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Run-Repro.ps1 -Source .\Repro.cs
pwsh -NoProfile -File .\Run-Repro2.ps1 -Source .\Repro2.cs -Case PerRecord
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Run-Repro2.ps1 -Source .\Repro2.cs -Case AsyncVoid
dotnet run PartialAttrs.cs
```

The `AsyncVoid` case in Windows PowerShell 5.1 printed "process survived" and then exited with code 2. That exit code is the observed result, not a failure of the script.

This pass runs many cases from one source file. Milestone M0 replaces it with one minimal cmdlet per rule and a Pester test that asserts each failure in both editions; `.claude/rules/repro-corpus.md` covers how.
