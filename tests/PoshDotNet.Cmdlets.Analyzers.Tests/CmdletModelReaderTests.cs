using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using PoshDotNet.Cmdlets.Analyzers.Model;

namespace PoshDotNet.Cmdlets.Analyzers.Tests;

/// <summary>
/// Provides the tests of the reader that turns a cmdlet class into the model every rule shares.
/// </summary>
public sealed class CmdletModelReaderTests
{
	/// <summary>
	/// A cmdlet with a parameter in two sets, a field parameter, and an alias, which several tests read.
	/// </summary>
	private const string THING_COMMAND = """
		using System.Management.Automation;

		namespace Contoso
		{
			[Cmdlet(VerbsCommon.Get, "Thing", DefaultParameterSetName = "ByName", SupportsShouldProcess = true)]
			public sealed class GetThingCommand : PSCmdlet
			{
				[Parameter(ParameterSetName = "ByName", Position = 0, Mandatory = true)]
				[Parameter(ParameterSetName = "ByInput", ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
				[Alias("n", "nm")]
				public string Name { get; set; }

				[Parameter(Position = 1, ValueFromRemainingArguments = true)]
				public string[] Rest;
			}
		}
		""";

	/// <summary>
	/// Verifies that the model holds the <c>[Cmdlet]</c> attribute's settings and the class's name and position.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[MemberData(nameof(EngineData.All), MemberType = typeof(EngineData))]
	public async Task ReadsTheCmdletAttribute(PowerShellEngine engine)
	{
		CmdletModel? cmdlet = await ReadAsync(engine, THING_COMMAND, "Contoso.GetThingCommand");

		Assert.NotNull(cmdlet);
		Assert.Equal("Contoso.GetThingCommand", cmdlet.TypeName);
		Assert.Equal("Get", cmdlet.Verb);
		Assert.Equal("Thing", cmdlet.Noun);
		Assert.Equal("ByName", cmdlet.DefaultParameterSetName);
		Assert.True(cmdlet.SupportsShouldProcess);
		Assert.Equal("GetThingCommand", TextAt(THING_COMMAND, cmdlet.Location));
	}

	/// <summary>
	/// Verifies that each <c>[Parameter]</c> attribute becomes a set entry with its settings and positions in source, and
	/// that aliases keep their order.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[MemberData(nameof(EngineData.All), MemberType = typeof(EngineData))]
	public async Task ReadsEachParameterAttribute(PowerShellEngine engine)
	{
		CmdletModel? cmdlet = await ReadAsync(engine, THING_COMMAND, "Contoso.GetThingCommand");

		Assert.NotNull(cmdlet);
		Assert.Equal(["Name", "Rest"], cmdlet.Parameters.Select(static parameter => parameter.Name));

		ParameterModel name = cmdlet.Parameters[0];
		Assert.Equal("string", name.TypeName);
		Assert.False(name.IsField);
		Assert.Equal("Contoso.GetThingCommand", name.DeclaringTypeName);
		Assert.Equal(["n", "nm"], name.Aliases);
		Assert.Equal("Name", TextAt(THING_COMMAND, name.Location));
		Assert.Equal(2, name.Sets.Count);

		ParameterSetEntry byName = name.Sets[0];
		Assert.Equal("ByName", byName.SetName);
		Assert.Equal(0, byName.Position);
		Assert.True(byName.Mandatory);
		Assert.False(byName.ValueFromPipeline);
		Assert.False(byName.ValueFromPipelineByPropertyName);
		Assert.False(byName.ValueFromRemainingArguments);
		Assert.Equal("""Parameter(ParameterSetName = "ByName", Position = 0, Mandatory = true)""", TextAt(THING_COMMAND, byName.Location));
		Assert.Equal("Position = 0", TextAt(THING_COMMAND, byName.PositionLocation));

		ParameterSetEntry byInput = name.Sets[1];
		Assert.Equal("ByInput", byInput.SetName);
		Assert.Null(byInput.Position);
		Assert.Null(byInput.PositionLocation);
		Assert.False(byInput.Mandatory);
		Assert.True(byInput.ValueFromPipeline);
		Assert.True(byInput.ValueFromPipelineByPropertyName);

		ParameterModel rest = cmdlet.Parameters[1];
		Assert.Equal("string[]", rest.TypeName);
		Assert.True(rest.IsField);
		Assert.Empty(rest.Aliases);
		ParameterSetEntry remaining = Assert.Single(rest.Sets);
		Assert.True(remaining.IsInAllSets);
		Assert.Equal(1, remaining.Position);
		Assert.True(remaining.ValueFromRemainingArguments);
	}

	/// <summary>
	/// Verifies that every spelling that means every set reads as no set name, and that other names keep their case.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[MemberData(nameof(EngineData.All), MemberType = typeof(EngineData))]
	public async Task ReadsEverySetSpellingAsNoName(PowerShellEngine engine)
	{
		const string source = """
			using System.Management.Automation;

			[Cmdlet(VerbsDiagnostic.Test, "Thing")]
			public sealed class TestThingCommand : PSCmdlet
			{
				[Parameter] public string Omitted { get; set; }
				[Parameter(ParameterSetName = "")] public string Empty { get; set; }
				[Parameter(ParameterSetName = null)] public string Null { get; set; }
				[Parameter(ParameterSetName = "__AllParameterSets")] public string Spelled { get; set; }
				[Parameter(ParameterSetName = "__ALLPARAMETERSETS")] public string Upper { get; set; }
				[Parameter(ParameterSetName = "ByName")] public string Named { get; set; }
				[Parameter(ParameterSetName = "byname")] public string Lower { get; set; }
			}
			""";

		CmdletModel? cmdlet = await ReadAsync(engine, source, "TestThingCommand");

		Assert.NotNull(cmdlet);
		Assert.Equal(
			[null, null, null, null, null, "ByName", "byname"],
			cmdlet.Parameters.Select(static parameter => Assert.Single(parameter.Sets).SetName));
	}

	/// <summary>
	/// Verifies that a position of <see cref="int.MinValue"/> reads as no position, and that a negative one is kept.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[MemberData(nameof(EngineData.All), MemberType = typeof(EngineData))]
	public async Task ReadsPositionMinValueAsNoPosition(PowerShellEngine engine)
	{
		const string source = """
			using System.Management.Automation;

			[Cmdlet(VerbsDiagnostic.Test, "Thing")]
			public sealed class TestThingCommand : PSCmdlet
			{
				[Parameter(Position = int.MinValue)] public string MinValue { get; set; }
				[Parameter(Position = -1)] public string Negative { get; set; }
			}
			""";

		CmdletModel? cmdlet = await ReadAsync(engine, source, "TestThingCommand");

		Assert.NotNull(cmdlet);
		Assert.Equal([null, -1], cmdlet.Parameters.Select(static parameter => Assert.Single(parameter.Sets).Position));
	}

	/// <summary>
	/// Verifies that only public instance properties and fields with their own <c>[Parameter]</c> become parameters.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[MemberData(nameof(EngineData.All), MemberType = typeof(EngineData))]
	public async Task ReadsOnlyPublicInstanceMembers(PowerShellEngine engine)
	{
		const string source = """
			using System.Management.Automation;

			[Cmdlet(VerbsDiagnostic.Test, "Thing")]
			public sealed class TestThingCommand : PSCmdlet
			{
				[Parameter] public string Property { get; set; }
				[Parameter] public string Field;
				[Parameter] internal string Internal { get; set; }
				[Parameter] public static string Shared { get; set; }
				[Parameter] private string _private;
				[Parameter] public string this[int index] { get { return null; } set { } }
				public string Plain { get; set; }
			}
			""";

		CmdletModel? cmdlet = await ReadAsync(engine, source, "TestThingCommand");

		Assert.NotNull(cmdlet);
		Assert.Equal(["Property", "Field"], cmdlet.Parameters.Select(static parameter => parameter.Name));
	}

	/// <summary>
	/// Verifies that base classes' parameters come first, each class's in declaration order, with the class that declares
	/// each one.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[MemberData(nameof(EngineData.All), MemberType = typeof(EngineData))]
	public async Task ReadsBaseClassParametersFirst(PowerShellEngine engine)
	{
		const string source = """
			using System.Management.Automation;

			public abstract class RootCommand : PSCmdlet
			{
				[Parameter] public string Root1 { get; set; }
				[Parameter] public string Root2 { get; set; }
			}

			public abstract class MiddleCommand : RootCommand
			{
				[Parameter] public string Middle { get; set; }
			}

			[Cmdlet(VerbsDiagnostic.Test, "Thing")]
			public sealed class TestThingCommand : MiddleCommand
			{
				[Parameter] public string Own { get; set; }
			}
			""";

		CmdletModel? cmdlet = await ReadAsync(engine, source, "TestThingCommand");

		Assert.NotNull(cmdlet);
		Assert.Equal(
			["Root1 in RootCommand", "Root2 in RootCommand", "Middle in MiddleCommand", "Own in TestThingCommand"],
			cmdlet.Parameters.Select(static parameter => parameter.Name + " in " + parameter.DeclaringTypeName));
	}

	/// <summary>
	/// Verifies that the reader hides base class members as reflection does: a derived property with the same name and
	/// type hides a property, and neither a property of another type nor a derived field hides anything.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[MemberData(nameof(EngineData.All), MemberType = typeof(EngineData))]
	public async Task HidesBaseMembersAsReflectionDoes(PowerShellEngine engine)
	{
		const string source = """
			using System.Management.Automation;

			public abstract class ThingCommandBase : PSCmdlet
			{
				[Parameter] public virtual string Overridden { get; set; }
				[Parameter] public string SameType { get; set; }
				[Parameter] public string OtherType { get; set; }
				[Parameter] public string Field;
			}

			[Cmdlet(VerbsDiagnostic.Test, "Thing")]
			public sealed class TestThingCommand : ThingCommandBase
			{
				public override string Overridden { get; set; }
				public new string SameType { get; set; }
				public new int OtherType { get; set; }
				public new string Field;
			}
			""";

		CmdletModel? cmdlet = await ReadAsync(engine, source, "TestThingCommand");

		Assert.NotNull(cmdlet);
		Assert.Equal(
			["OtherType in ThingCommandBase", "Field in ThingCommandBase"],
			cmdlet.Parameters.Select(static parameter => parameter.Name + " in " + parameter.DeclaringTypeName));
	}

	/// <summary>
	/// Verifies that an override that carries its own <c>[Parameter]</c> replaces the base declaration's settings.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[MemberData(nameof(EngineData.All), MemberType = typeof(EngineData))]
	public async Task ReadsAnOverridesOwnAttribute(PowerShellEngine engine)
	{
		const string source = """
			using System.Management.Automation;

			public abstract class ThingCommandBase : PSCmdlet
			{
				[Parameter(Position = 0)] public virtual string Name { get; set; }
			}

			[Cmdlet(VerbsDiagnostic.Test, "Thing")]
			public sealed class TestThingCommand : ThingCommandBase
			{
				[Parameter(Position = 1)] public override string Name { get; set; }
			}
			""";

		CmdletModel? cmdlet = await ReadAsync(engine, source, "TestThingCommand");

		Assert.NotNull(cmdlet);
		ParameterModel name = Assert.Single(cmdlet.Parameters);
		Assert.Equal("TestThingCommand", name.DeclaringTypeName);
		Assert.Equal(1, Assert.Single(name.Sets).Position);
	}

	/// <summary>
	/// Verifies that the reader skips classes that PowerShell doesn't register as cmdlets.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[MemberData(nameof(EngineData.All), MemberType = typeof(EngineData))]
	public async Task SkipsClassesThatAreNotCmdlets(PowerShellEngine engine)
	{
		const string source = """
			using System.Management.Automation;

			public class NoAttributeCommand : PSCmdlet
			{
			}

			[Cmdlet(VerbsDiagnostic.Test, "Base")]
			public class TestBaseCommand : PSCmdlet
			{
			}

			public class InheritedAttributeCommand : TestBaseCommand
			{
			}

			[Cmdlet(VerbsDiagnostic.Test, "Abstract")]
			public abstract class TestAbstractCommand : PSCmdlet
			{
			}

			[Cmdlet(VerbsDiagnostic.Test, "Generic")]
			public sealed class TestGenericCommand<T> : PSCmdlet
			{
			}

			public sealed class Outer<T>
			{
				[Cmdlet(VerbsDiagnostic.Test, "Nested")]
				public sealed class TestNestedCommand : PSCmdlet
				{
				}
			}

			[Cmdlet(VerbsDiagnostic.Test, "NotCmdlet")]
			public sealed class TestNotCmdletCommand
			{
			}
			""";

		(Compilation compilation, EngineSymbols engineSymbols) = await CompileAsync(engine, source);
		CancellationToken cancellationToken = TestContext.Current.CancellationToken;

		Assert.NotNull(CmdletModelReader.Read(GetClass(compilation, "TestBaseCommand"), engineSymbols, cancellationToken));
		Assert.All(
			["NoAttributeCommand", "InheritedAttributeCommand", "TestAbstractCommand", "TestGenericCommand`1", "Outer`1+TestNestedCommand", "TestNotCmdletCommand"],
			metadataName => Assert.Null(CmdletModelReader.Read(GetClass(compilation, metadataName), engineSymbols, cancellationToken)));
	}

	/// <summary>
	/// Verifies that two reads of the same source in separate compilations are equal, and that a changed position isn't.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[MemberData(nameof(EngineData.All), MemberType = typeof(EngineData))]
	public async Task ReadsEqualModelsFromEqualSource(PowerShellEngine engine)
	{
		CmdletModel? first = await ReadAsync(engine, THING_COMMAND, "Contoso.GetThingCommand");
		CmdletModel? second = await ReadAsync(engine, THING_COMMAND, "Contoso.GetThingCommand");
		CmdletModel? changed = await ReadAsync(engine, THING_COMMAND.Replace("Position = 1", "Position = 2"), "Contoso.GetThingCommand");

		Assert.NotNull(first);
		Assert.NotSame(first, second);
		Assert.Equal(first, second);
		Assert.Equal(first.GetHashCode(), second!.GetHashCode());
		Assert.NotEqual(first, changed);
	}

	/// <summary>
	/// Compiles the specified source against an engine's reference assemblies and reads one class from it.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <param name="source">The source to compile.</param>
	/// <param name="metadataName">The metadata name of the class to read.</param>
	/// <returns>The class's model, or <see langword="null"/> when the reader skips it.</returns>
	private static async Task<CmdletModel?> ReadAsync(PowerShellEngine engine, string source, string metadataName)
	{
		(Compilation compilation, EngineSymbols engineSymbols) = await CompileAsync(engine, source);

		return CmdletModelReader.Read(GetClass(compilation, metadataName), engineSymbols, TestContext.Current.CancellationToken);
	}

	/// <summary>
	/// Compiles the specified source against an engine's reference assemblies, and verifies that it compiles without
	/// errors and that the engine types resolve.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <param name="source">The source to compile.</param>
	/// <returns>The compilation and its engine types.</returns>
	private static async Task<(Compilation Compilation, EngineSymbols Engine)> CompileAsync(PowerShellEngine engine, string source)
	{
		CancellationToken cancellationToken = TestContext.Current.CancellationToken;
		ImmutableArray<MetadataReference> references = await EngineReferences.For(engine).ResolveAsync(LanguageNames.CSharp, cancellationToken);
		CSharpCompilation compilation = CSharpCompilation.Create(
			"ModelTest",
			[CSharpSyntaxTree.ParseText(source, path: "/0/Test0.cs", cancellationToken: cancellationToken)],
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		Assert.Empty(compilation.GetDiagnostics(cancellationToken).Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

		EngineSymbols? engineSymbols = EngineSymbols.Create(compilation);
		Assert.NotNull(engineSymbols);
		return (compilation, engineSymbols);
	}

	/// <summary>
	/// Gets the class with the specified metadata name, and verifies that it exists.
	/// </summary>
	/// <param name="compilation">The compilation that declares the class.</param>
	/// <param name="metadataName">The class's metadata name.</param>
	/// <returns>The class.</returns>
	private static INamedTypeSymbol GetClass(Compilation compilation, string metadataName)
	{
		INamedTypeSymbol? type = compilation.GetTypeByMetadataName(metadataName);
		Assert.NotNull(type);
		return type;
	}

	/// <summary>
	/// Gets the source text that a captured position covers.
	/// </summary>
	/// <param name="source">The source that the position was captured from.</param>
	/// <param name="location">The position, which must not be <see langword="null"/>.</param>
	/// <returns>The text at <paramref name="location"/>.</returns>
	private static string TextAt(string source, LocationInfo? location)
	{
		Assert.NotNull(location);
		Assert.Equal("/0/Test0.cs", location.FilePath);
		return source.Substring(location.TextSpan.Start, location.TextSpan.Length);
	}
}
