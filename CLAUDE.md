# CLAUDE.md

This file provides guidance to Claude Code when working in this repository.

PoshDotNet is a family of .NET tools for authors of binary PowerShell modules. Its first package, `PoshDotNet.Cmdlets.Analyzers`, will hold Roslyn analyzers, code fixes, and later a source generator that report cmdlet mistakes at compile time. These are mistakes that PowerShell otherwise reveals only at import, at bind time, in the middle of a pipeline, or never. The modules it checks target Windows PowerShell 5.1, PowerShell 7, or both.

As of 2026-10-11 the project is in milestone M1, and nothing has been released. The analyzer project holds the shared cmdlet model and the first rule, PDN1005. Its test project compiles each case against every engine's reference assemblies, and `tests/repro/` holds the first repro pass and PDN1005's Pester cases.

## The plan

**[PoshDotNet.Cmdlets.Analyzers — Draft Plan](https://claude.ai/code/artifact/fd9de08f-37fe-4c90-bcdc-cb1e1c532d45)** is the source of truth for this project's scope and decisions.

- Read it before you plan, design, or build anything here, and before you answer a question about what the project will do. It holds the problem statement, the analyzer catalog (with the runtime behavior behind each rule, reproduced on 2026-10-10), the diagnostic ID blocks, what the generator will and won't own, the milestones M0 to M6 with their gates, and the open questions.
- It's a Claude Doc, not a web page. Read and edit it with the Claude Docs tools, loading the docs skill and the `claude-docs-api` skill first. Never fetch it with WebFetch or curl.
- It lives in Mike's claude.ai account and is private. Don't link it from `README.md` or from anything else that ships.
- Record decisions in the doc, in their section or under Open questions, in the same session that makes them. Don't copy the catalog, the ID table, or the milestones into this file, so there's only one copy to keep current.
- When a milestone or a rule ships, update the doc to match in the same change.

Two identifiers are settled and safe to use without reading the doc: the package ID `PoshDotNet.Cmdlets.Analyzers` (a separately shipped generator would be `PoshDotNet.Cmdlets.Generators`), and diagnostic IDs of `PDN` plus four digits, whose first digit names the category.

## Repository layout

Of the folders below, `src/PoshDotNet.Cmdlets.Analyzers/`, `tests/PoshDotNet.Cmdlets.Analyzers.Tests/`, and `tests/repro/` exist. Create each of the others with its first project, not before.

```text
PoshDotNet/
├─ PoshDotNet.slnx                              one solution for every product, a solution folder per area
├─ src/
│  ├─ PoshDotNet.Cmdlets.Analyzers/             analyzers (netstandard2.0)
│  ├─ PoshDotNet.Cmdlets.Analyzers.CodeFixes/   code fixes, split out because they need Workspaces (netstandard2.0)
│  └─ PoshDotNet.Cmdlets.Generators/            incremental source generator (netstandard2.0, milestone M5)
├─ tests/
│  ├─ PoshDotNet.Cmdlets.Analyzers.Tests/       analyzer and code-fix tests
│  └─ repro/                                    PowerShell repro corpus: proof of each rule's runtime failure
├─ .ideas/                                      multi-session plans, if a plan moves into the repo
├─ .claude/                                     rules and skills (indexed below)
└─ .github/copilot-instructions.md              the XML documentation guide, for GitHub Copilot
```

Later products follow the same pattern: `src/PoshDotNet.<Area>.<Kind>/`, with its tests in `tests/PoshDotNet.<Area>.<Kind>.Tests/`, in the same solution.

## When the first projects are created

These conventions come from Mike's ListFunctions and Realta repositories:

- Central package management: package versions live only in a root `Directory.Packages.props`.
- A root `Directory.Build.props` holds the shared metadata (`Authors` Mike Garvey, `Company` Yevrag35, LLC., the copyright line, the repository URL) and the shared compiler settings (`LangVersion`, `Nullable`, `AllowUnsafeBlocks`). Each csproj keeps only what differs.
- `Version` lives in `Directory.Build.props`, and `AssemblyVersion` and `FileVersion` reuse it so they stay three-part.
- Roslyn projects follow `.claude/rules/roslyn-components.md`.
- The `Microsoft.CodeAnalysis.CSharp` version the analyzers reference is the oldest compiler a consumer can build with. Choose it deliberately for a public package. Realta pins 5.9.0, which works only because nothing but Realta consumes its analyzers.
- Test projects use xUnit.net v3 on Microsoft.Testing.Platform, with a root `global.json` of `{ "test": { "runner": "Microsoft.Testing.Platform" } }`. Without it, `dotnet test` uses VSTest, which a Microsoft.Testing.Platform project rejects on the .NET 10 SDK. Don't add `Microsoft.NET.Test.Sdk` or `xunit.runner.visualstudio`.
- Never pass `--nologo` to `dotnet test`. The platform hands options it doesn't recognize to the test app, which stops with "Zero tests ran". The flag is still right for `dotnet build`.

## Rules and skills

| Name | Kind | Use |
|---|---|---|
| `.claude/rules/roslyn-components.md` | path-scoped rule | Loads for files under `src/*.Analyzers/`, `src/*.CodeFixes/`, and `src/*.Generators/` |
| `.claude/rules/repro-corpus.md` | path-scoped rule | Loads for files under `tests/repro/` |
| `pdn-pwsh-internals` | project skill | Measured PowerShell engine behavior; see PowerShell behavior below |
| `csharp-xml-docs` | personal skill | Writing or editing XML documentation comments |
| `multi-session-plans` | personal skill | Drafting or updating a `.ideas/` plan |
| `claude-docs-api` | personal skill | Reading and editing Claude Docs, including a map of the plan doc's blocks |

Personal skills live in `~/.claude/skills/` on Mike's machine, not in this repository.

## PowerShell behavior

Every rule encodes a fact about the PowerShell engine, and most of those facts are undocumented. Load the `pdn-pwsh-internals` skill before you write or change a rule, a code fix, or generated code that depends on engine behavior, decide a rule's default severity, answer a question about how PowerShell behaves, or measure it. Trust its measurements over recollection, measure in both editions, and record each new measurement there with the versions and the date.

## Tests

- `tests/repro/` proves each rule's runtime failure in Windows PowerShell 5.1 and PowerShell 7. `.claude/rules/repro-corpus.md` covers writing and running it.
- Each analyzer rule ships with its tests and its repro in the same change. That overrides the "tests go in a final phase" rule in the `multi-session-plans` skill: a rule's tests are its specification, written against the diagnostic's contract rather than against code that later phases reshape.
- Use stable PowerShell releases only: Windows PowerShell 5.1, and PowerShell 7.6 or a later stable release. Name the versions in every result you report.

## Code style

`.editorconfig` is the authority. Code you write or change must use:

- Tabs for indentation (tab width 4), LF line endings, and UTF-8. `.gitattributes` enforces LF with `* text=auto eol=lf`.
- File-scoped namespaces, with `using` directives outside the namespace.
- `this.` qualification on methods, properties, and events, and never on fields.
- Regular constructors, not primary constructors.
- Block-bodied methods, constructors, and operators. Expression bodies are fine for single-line properties, indexers, and accessors.
- The modifier order `public, private, protected, internal, file, static, extern, new, virtual, abstract, sealed, override, readonly, unsafe, volatile, async`.
- Names: `_camelCase` for private instance fields and private static properties, `s_camelCase` for private static readonly fields, `ALL_UPPER` for private constants, and PascalCase for types, members, and non-private constants.

Ask before reformatting whole files that you aren't otherwise changing.

XML documentation follows the `csharp-xml-docs` skill. `.github/copilot-instructions.md` gives GitHub Copilot the same guide.

Commit titles use the third-person singular simple present tense, such as "Adds ..." or "Updates ...".

## Working notes

`.ideas/` holds plans, and `.temp/` (git-ignored) is scratch space. Treat what you read there as working notes: they may be outdated, speculative, half-implemented, or abandoned. Verify a claim from them against the current code, or `git log` and `git blame`, before relying on it or restating it as fact. When you're asked to implement what such a file describes, treat it as authoritative for that task.

## Words to avoid

Mike finds these annoying, so don't use them: load-bearing, blast radius (or blast-radius), footgun (or foot-gun), gotcha.
