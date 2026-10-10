# Assembly loading

> Copied on 2026-10-10 from Mike's ListFunctions repository. Names such as `New-HashSet` and `ListFunctions.Engine` are ListFunctions' own, given where a measurement was taken; the facts are general engine behavior. The last section says what they mean for PoshDotNet.

## Resolving dependencies in Windows PowerShell 5.1

Measured 2026-10-05 on Windows PowerShell 5.1.26100.9444 with the Debug `net48` build.

- `ResolveEventArgs.RequestingAssembly` is `null` for a static reference from a module's assembly, including one that `Import-Module` loaded for another module. The only non-null requester seen was `Anonymously Hosted DynamicMethods Assembly`, a dynamic assembly, for `[Reflection.Assembly]::Load()` called from script. Satellite `.resources` lookups have a `null` requester too. So a requester check can't tell one module's binds from another's.
- The runtime rejects a handler's result whose public key token is wrong (`FileLoadException`, 0x80131040), but accepts any version, including a downgrade.
- The runtime reuses each answer: once a handler returns an assembly for an identity, binding that identity again raises no new event.
- `Assembly.LoadFile` and `Assembly.LoadFrom` on the same path return the same `Assembly` object, in either order. No duplicate copies appeared in the session.
- The runtime calls `AssemblyResolve` handlers in registration order and stops at the first non-null result. PowerShell registers two handlers of its own, from `LanguagePrimitives` and `ExecutionContext`, ahead of any module's. To see whether a handler answered an event, register a logging handler before it and another after it.
- PowerShell calls `IModuleAssemblyInitializer.OnImport` on every import, including a re-import after `Remove-Module` and an `Import-Module -Force`, so `OnImport` has to guard against registering a handler twice.

## Type names and lazy loading

PowerShell resolves a type name only against assemblies that are already loaded, and the runtime loads an assembly the first time code needs it. Importing `ListFunctions.Next.dll` or `ListFunctions.NETFramework.dll` doesn't load `ListFunctions.Engine.dll`, so until a cmdlet runs, `'ListFunctions.Modern.Exceptions.HashCodeScriptException' -as [type]` is `$null` and a type literal fails with "Unable to find type", in both editions (seen 2026-10-07). Running a cmdlet that uses the dependency loads it.

## Loading a build's DLLs in a script

To inspect a build, such as counting the public types it exports with `Assembly.GetExportedTypes()`, load each DLL with `[Reflection.Assembly]::LoadFrom` in a new process, so the build output stays unlocked.

- Load two builds of the same assembly in separate PowerShell 7 processes. ListFunctions' two Engine builds share the identity `ListFunctions.Engine, Version=4.0.0.0`, so a second `LoadFrom` in the same process throws "Assembly with same name is already loaded". In a `foreach` loop that error doesn't stop the script, the variable keeps the first assembly, and the second path prints the first build's results under its own name (found 2026-10-07). DLLs whose assembly names differ can share a process.
- In Windows PowerShell 5.1, `GetExportedTypes()` on a `netstandard2.0` build threw `FileNotFoundException` for `System.Collections.Immutable, Version=10.0.0.0`, because nothing ran the module's resolver (found 2026-10-07). PowerShell 7 loaded the same build without help.

## The PowerShell 7 version

Measured 2026-10-10 on PowerShell 7.6.6 with the Release build.

- PowerShell 7.6.6 runs `System.Management.Automation` 7.6.0.500. The `net10.0` Engine and `ListFunctions.Next.dll`, compiled against the 7.6.6 package, reference 7.6.0.500 too, so the reference doesn't name the package's patch release.
- `$PSVersionTable.PSVersion` is a `SemanticVersion`, and `-ge [version]'7.6'` compares it with 7.6.0. It's `True` for 7.6.0, 7.6.6, 7.7.0-preview.1, and 8.0.0, and `False` for 6.2.7, 7.4.13, 7.5.4, 7.6.0-preview.5, and 7.6.0-rc.1.
- A child scope can't shadow `$PSVersionTable`: assigning to it fails with "Cannot overwrite variable PSVersionTable because it is read-only or constant." To try a version check with other versions, run its text with the variable renamed.

## In PoshDotNet

- None of this touches the analyzer package itself. Roslyn components load in the compiler, not in PowerShell.
- It's why nothing that PoshDotNet generates may add a runtime dependency. A module can't use binding redirects the way an application can, so in Windows PowerShell 5.1 every dependency needs a resolver like ListFunctions' `ModuleInitializer`, and PowerShell 7 can't load a second assembly with the same name into one process.
- `System.Management.Automation` reports `Version=3.0.0.0` in Windows PowerShell 5.1.26100 and `Version=7.6.0.500` in PowerShell 7.6.6 (checked 2026-10-10). An edition-aware rule can tell the editions apart by the version a compilation references; confirm what each reference-assembly package reports before relying on it.
