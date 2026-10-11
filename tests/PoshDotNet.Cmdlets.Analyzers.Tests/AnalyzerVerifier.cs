using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace PoshDotNet.Cmdlets.Analyzers.Tests;

/// <summary>
/// Provides the methods that run an analyzer over cmdlet code compiled against one engine's reference assemblies.
/// </summary>
/// <typeparam name="TAnalyzer">The analyzer under test.</typeparam>
public static class AnalyzerVerifier<TAnalyzer>
	where TAnalyzer : DiagnosticAnalyzer, new()
{
	/// <summary>
	/// Creates the expected result of a diagnostic with the specified descriptor.
	/// </summary>
	/// <param name="descriptor">The descriptor of the expected diagnostic.</param>
	/// <returns>A result that expects a diagnostic of <paramref name="descriptor"/>, to which a test adds locations and arguments.</returns>
	public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
	{
		return new DiagnosticResult(descriptor);
	}

	/// <summary>
	/// Compiles the specified source against an engine's reference assemblies, runs the analyzer over it, and verifies that
	/// it reports exactly the expected diagnostics.
	/// </summary>
	/// <param name="engine">The engine whose reference assemblies the source compiles against.</param>
	/// <param name="source">The source, with markup for the expected locations.</param>
	/// <param name="expected">The diagnostics the analyzer must report.</param>
	/// <returns>A task that represents the verification.</returns>
	public static Task VerifyAsync(PowerShellEngine engine, string source, params DiagnosticResult[] expected)
	{
		CSharpAnalyzerTest<TAnalyzer, DefaultVerifier> test = new()
		{
			ReferenceAssemblies = EngineReferences.For(engine),
			TestCode = source,
		};

		test.ExpectedDiagnostics.AddRange(expected);
		return test.RunAsync(TestContext.Current.CancellationToken);
	}
}
