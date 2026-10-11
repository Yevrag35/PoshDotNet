using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using PoshDotNet.Cmdlets.Analyzers.Model;
using PoshDotNet.Cmdlets.Analyzers.Rules;

namespace PoshDotNet.Cmdlets.Analyzers;

/// <summary>
/// Reports mistakes in a cmdlet class's declarations that PowerShell reveals only when it builds the command's metadata
/// or binds its parameters.
/// </summary>
/// <remarks>
/// The analyzer reads each cmdlet class once into a model of its parameters and runs every declaration rule over that
/// model. It does nothing in a compilation that doesn't reference PowerShell.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CmdletDeclarationAnalyzer : DiagnosticAnalyzer
{
	/// <summary>
	/// Gets the descriptors of the diagnostics that this analyzer reports.
	/// </summary>
	/// <value>
	/// The descriptors of the declaration rules.
	/// </value>
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Descriptors.DuplicatePosition];

	/// <summary>
	/// Registers the analyzer's actions.
	/// </summary>
	/// <param name="context">The context to register the actions with.</param>
	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterCompilationStartAction(OnCompilationStart);
	}

	/// <summary>
	/// Looks up the engine types once for a compilation, and registers the class analysis when the compilation references
	/// PowerShell.
	/// </summary>
	/// <param name="context">The compilation's start context.</param>
	private static void OnCompilationStart(CompilationStartAnalysisContext context)
	{
		if (EngineSymbols.Create(context.Compilation) is not EngineSymbols engine)
		{
			return;
		}

		context.RegisterSymbolAction(symbolContext => AnalyzeType(symbolContext, engine), SymbolKind.NamedType);
	}

	/// <summary>
	/// Reads a class into a cmdlet model and runs the declaration rules over it.
	/// </summary>
	/// <param name="context">The class's analysis context.</param>
	/// <param name="engine">The engine types of the compilation that holds the class.</param>
	private static void AnalyzeType(SymbolAnalysisContext context, EngineSymbols engine)
	{
		if (context.Symbol is not INamedTypeSymbol type
			|| CmdletModelReader.Read(type, engine, context.CancellationToken) is not CmdletModel cmdlet)
		{
			return;
		}

		DuplicatePositionRule.Analyze(cmdlet, context.Compilation, context.ReportDiagnostic);
	}
}
