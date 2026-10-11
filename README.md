# PoshDotNet

.NET tooling for authors of binary PowerShell modules.

The first package, `PoshDotNet.Cmdlets.Analyzers`, will be a set of Roslyn analyzers, code fixes, and later a source generator that report cmdlet mistakes at compile time. These are mistakes that PowerShell otherwise reveals only at import, at bind time, in the middle of a pipeline, or never: a parameter named like a common parameter, a `-WhatIf` that still performs the action, a typo in a parameter set name, a `WriteObject` from the wrong thread. It targets modules for Windows PowerShell 5.1, PowerShell 7, or both, and adds nothing to a module at run time.

**Status:** planning. The solution builds, but it has no rules yet. `tests/repro/` holds the first measurements of the runtime behavior the analyzers will check.
