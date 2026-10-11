# PoshDotNet

.NET tooling for authors of binary PowerShell modules.

The first package, `PoshDotNet.Cmdlets.Analyzers`, will be a set of Roslyn analyzers, code fixes, and later a source generator that report cmdlet mistakes at compile time. These are mistakes that PowerShell otherwise reveals only at import, at bind time, in the middle of a pipeline, or never: a parameter named like a common parameter, a `-WhatIf` that still performs the action, a typo in a parameter set name, a `WriteObject` from the wrong thread. It targets modules for Windows PowerShell 5.1, PowerShell 7, or both, and adds nothing to a module at run time.

**Status:** in development, and not yet released. The first rule, PDN1005, reports two parameters with the same position in one parameter set, where PowerShell can't bind a positional argument. `tests/repro/` holds the measurements of the runtime behavior the analyzers check, as Pester tests that run in both editions.
