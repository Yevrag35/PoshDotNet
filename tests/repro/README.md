# Repro corpus

Each analyzer rule reports a mistake that PowerShell reveals only at import, at bind time, in the middle of a pipeline, or never. This folder proves each one: it runs minimal cmdlets in Windows PowerShell 5.1 and PowerShell 7 and asserts what the engine does with them. When a PowerShell release changes a behavior, a test here fails and the rule gets revisited.

| Path | What it holds |
|---|---|
| `Invoke-ReproCorpus.ps1` | Runs every `*.Tests.ps1` here in both editions, each in a new process |
| `PDN1005/` | Two parameters with the same `Position` in one parameter set: the cases PDN1005 reports, and the near misses it leaves alone |
| `first-pass-2026-10-10/` | The first measurements behind the analyzer catalog. Each case moves to its rule's folder when the rule is written. |

Each rule's folder holds its cmdlets in one C# file and a Pester test that compiles them with `Add-Type`. The C# is C# 5, because `Add-Type` in Windows PowerShell 5.1 compiles with the legacy compiler.

## Running it

The corpus needs Windows PowerShell 5.1, a stable PowerShell 7 in `$env:ProgramFiles\PowerShell\7`, and Pester 6.2.0 or later where both editions can find it.

```powershell
.\tests\repro\Invoke-ReproCorpus.ps1
.\tests\repro\Invoke-ReproCorpus.ps1 -Path .\tests\repro\PDN1005 -Edition Core
```

The script prints each edition's version and Pester's, and exits with the number of editions whose run failed. On 2026-10-11, PDN1005's 21 tests passed in Windows PowerShell 5.1.26100.9444 and PowerShell 7.6.6 with Pester 6.2.0.
