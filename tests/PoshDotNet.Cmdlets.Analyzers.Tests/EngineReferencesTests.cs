using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace PoshDotNet.Cmdlets.Analyzers.Tests;

/// <summary>
/// Provides smoke tests that compile a minimal cmdlet against each engine's reference assemblies.
/// </summary>
public sealed class EngineReferencesTests
{
	/// <summary>
	/// A minimal cmdlet that compiles against every engine.
	/// </summary>
	private const string MINIMAL_CMDLET = """
		using System.Management.Automation;

		[Cmdlet(VerbsDiagnostic.Test, "Engine")]
		public sealed class TestEngineCommand : PSCmdlet
		{
			[Parameter(Position = 0)]
			public string Name { get; set; }

			protected override void ProcessRecord()
			{
				this.WriteObject(this.Name);
			}
		}
		""";

	/// <summary>
	/// The output of the running test.
	/// </summary>
	private readonly ITestOutputHelper _output;

	/// <summary>
	/// Initializes a new <see cref="EngineReferencesTests"/> instance with the specified test output.
	/// </summary>
	/// <param name="output">The output of the running test.</param>
	public EngineReferencesTests(ITestOutputHelper output)
	{
		_output = output;
	}

	/// <summary>
	/// Verifies that a minimal cmdlet compiles against an engine's reference assemblies, and that the compilation reports
	/// the core library and the <c>System.Management.Automation</c> version that engine-aware rules read.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <param name="coreLibrary">The name of the assembly that the compilation's <see cref="object"/> comes from.</param>
	/// <param name="automationMajorVersion">The major version of the referenced <c>System.Management.Automation</c>.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[InlineData(PowerShellEngine.WindowsPowerShell, "mscorlib", 3)]
	[InlineData(PowerShellEngine.PowerShellStandard, "netstandard", 3)]
	[InlineData(PowerShellEngine.PowerShell7, "System.Runtime", 7)]
	public async Task MinimalCmdletCompilesAgainstEngine(PowerShellEngine engine, string coreLibrary, int automationMajorVersion)
	{
		CancellationToken cancellationToken = TestContext.Current.CancellationToken;
		ReferenceAssemblies references = EngineReferences.For(engine);
		ImmutableArray<MetadataReference> metadata = await references.ResolveAsync(LanguageNames.CSharp, cancellationToken);
		CSharpCompilation compilation = CSharpCompilation.Create(
			"EngineSmokeTest",
			[CSharpSyntaxTree.ParseText(MINIMAL_CMDLET, cancellationToken: cancellationToken)],
			metadata,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		Diagnostic[] errors = [.. compilation.GetDiagnostics(cancellationToken).Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)];
		Assert.Empty(errors);

		IAssemblySymbol coreAssembly = compilation.ObjectType.ContainingAssembly;
		INamedTypeSymbol? cmdlet = compilation.GetTypeByMetadataName("System.Management.Automation.PSCmdlet");
		Assert.NotNull(cmdlet);

		string packages = string.Join(", ", references.Packages.Select(static package => $"{package.Id} {package.Version}"));
		_output.WriteLine($"{engine}: {references.ReferenceAssemblyPackage?.Id} {references.ReferenceAssemblyPackage?.Version}; {packages}");
		_output.WriteLine($"{engine}: core library {coreAssembly.Identity}; {cmdlet.ContainingAssembly.Identity}");

		Assert.Equal(coreLibrary, coreAssembly.Name);
		Assert.Equal(automationMajorVersion, cmdlet.ContainingAssembly.Identity.Version.Major);
	}

	/// <summary>
	/// Verifies that the analyzer test harness compiles a minimal cmdlet against an engine's reference assemblies without
	/// compiler errors.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[InlineData(PowerShellEngine.WindowsPowerShell)]
	[InlineData(PowerShellEngine.PowerShellStandard)]
	[InlineData(PowerShellEngine.PowerShell7)]
	public async Task AnalyzerTestCompilesMinimalCmdlet(PowerShellEngine engine)
	{
		CSharpAnalyzerTest<EmptyDiagnosticAnalyzer, DefaultVerifier> test = new()
		{
			ReferenceAssemblies = EngineReferences.For(engine),
			TestCode = MINIMAL_CMDLET,
		};

		await test.RunAsync(TestContext.Current.CancellationToken);
	}
}
