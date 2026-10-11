---
description: Constraints and patterns for PoshDotNet's Roslyn analyzer, code-fix, and source-generator projects
paths:
  - "src/*.Analyzers/**"
  - "src/*.CodeFixes/**"
  - "src/*.Generators/**"
---

# Roslyn components

Adapted on 2026-10-10 from the conventions of Realta's analyzer and generator projects. `src/PoshDotNet.Cmdlets.Analyzers/` follows it; follow it in every Roslyn project you add.

## Target netstandard2.0

Analyzers, code fixes, and generators load into the compiler and the IDE as .NET Standard 2.0 components. APIs that later .NET versions added, such as `string.Create`, `Index` and `Range`, and newer span helpers, aren't available, and records and `init` setters need the `IsExternalInit` polyfill. Check every API against `netstandard2.0`, and polyfill language features with PolySharp, as ListFunctions does, or use the older API.

## Project settings

- Every Roslyn project sets `IsRoslynComponent` and `EnforceExtendedAnalyzerRules`.
- The analyzer assembly references `Microsoft.CodeAnalysis.CSharp` only. Code fixes need `Microsoft.CodeAnalysis.CSharp.Workspaces`, so they live in a separate `*.CodeFixes` assembly: the compiler loads analyzers with no Workspaces in their load context, and only the IDE loads code fixes.
- Reference those packages with `PrivateAssets="all"`, so they don't flow to consumers.
- A project in this repository that runs a generator or analyzer references it as `<ProjectReference ... OutputItemType="Analyzer" ReferenceOutputAssembly="false" />`. It runs at compile time and is never a runtime dependency.
- The NuGet package puts the Roslyn assemblies under `analyzers/dotnet/cs` and nothing under `lib/`. Nothing PoshDotNet ships may add a DLL to a consumer's PowerShell module.

## Diagnostic IDs and release tracking

- IDs are `PDN` plus four digits, and the first digit names the category. The plan doc linked from `CLAUDE.md` maps the blocks in its Diagnostic IDs table. Take the next free number in the rule's block, and never reuse a retired ID.
- Each block maps to one `DiagnosticDescriptor.Category` string. Make those strings unique to PoshDotNet: an `.editorconfig` line such as `dotnet_analyzer_diagnostic.category-Usage.severity` changes every analyzer's rules in a shared category, not just ours. `DiagnosticCategories` holds them, each `PoshDotNet.` plus the block's name, such as `PoshDotNet.Declarations`, and `Descriptors` holds the descriptors.
- With `EnforceExtendedAnalyzerRules` on, RS2008 fires as soon as a project declares a `DiagnosticDescriptor` without release tracking. Add `AnalyzerReleases.Shipped.md` and `AnalyzerReleases.Unshipped.md` as `AdditionalFiles`, list each new ID in Unshipped, and move it to Shipped in the release that ships it.
- A rule defaults to Error only when the repro corpus shows a guaranteed runtime failure. Anything heuristic is a Warning.

## Reading cmdlets

- Look up engine types by metadata name once per compilation, in a `RegisterCompilationStartAction` callback, such as `compilation.GetTypeByMetadataName("System.Management.Automation.Cmdlet")`. Register nothing when the compilation doesn't reference PowerShell.
- Never reference `System.Management.Automation` from a Roslyn project. The analyzers see the consumer's references only as symbols.
- A consumer's target frameworks compile separately, so judge each compilation against the engine it references. `System.Management.Automation` reports `Version=3.0.0.0` in Windows PowerShell 5.1.26100 and `Version=7.6.0.500` in PowerShell 7.6.6 (checked 2026-10-10). Confirm the identity that each reference-assembly package reports before a rule keys on it. The `pdn-pwsh-internals` skill lists the engine APIs that each edition lacks.
- In `Initialize`, call `context.EnableConcurrentExecution()` and `context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)`. Prefer symbol and operation actions to walking syntax trees.
- Read a cmdlet's declarations through the shared model in `Model/`, never from symbols in a rule. `CmdletModelReader` reads a class once into value-equatable records, following the engine's measured rules for which members are parameters. `CmdletDeclarationAnalyzer` builds the model and passes it to each declaration rule, a static class in `Rules/` that reports through a callback. A new declaration rule adds its descriptor to `Descriptors` and a call to `CmdletDeclarationAnalyzer`.

## Generators

- Implement `IIncrementalGenerator`, find marked symbols with `SyntaxProvider.ForAttributeWithMetadataName`, and inject marker attributes with `RegisterPostInitializationOutput`.
- Keep pipeline models value-equatable: records of strings, numbers, and equatable arrays. Never hold an `ISymbol`, a `SyntaxNode`, a `Compilation`, or a `Location` in a model, because each one defeats caching. Carry a small location record (file path, text span, line span), and rebuild the `Location` when reporting a diagnostic.
- Generated code must compile against both engines' reference assemblies, because a consumer's `net48` build compiles it against Windows PowerShell 5.1. Choose an engine-specific mechanism per compilation; for example, `IValidateSetValuesGenerator` exists only in PowerShell 7.
- Start each generated file with `// <auto-generated/>` and `#nullable enable`.

## Tests

- Test with the Microsoft.CodeAnalysis.Testing packages: `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing`, `.CodeFix.Testing`, and `.SourceGenerators.Testing`. Version 1.1.4 was current on 2026-10-10, and their development has moved into the dotnet/roslyn repository.
- Compile test sources against the reference assemblies of each engine a rule cares about, so a rule that behaves differently per edition is tested in both. `EngineReferences` in the test project gives the three sets: `WindowsPowerShell` (net48), `PowerShellStandard` (netstandard2.0) and `PowerShell7` (net10.0). The engine packages' versions live in `Directory.Packages.props`.
- Give every rule positive and negative cases, and ship them with the rule. `AnalyzerVerifier<TAnalyzer>` runs a case against one engine, and `[MemberData(nameof(EngineData.All), MemberType = typeof(EngineData))]` runs it against all three.
- The harness applies an `.editorconfig` line for a diagnostic ID, but not a `dotnet_analyzer_diagnostic.category-*` line, which only a build applies (checked 2026-10-11 with Microsoft.CodeAnalysis.Testing 1.1.4). Check a category line with a real build of a project that references the analyzer.
- Test generator caching: run the driver twice with step tracking on, and assert that unchanged inputs come back `Cached` or `Unchanged`.
