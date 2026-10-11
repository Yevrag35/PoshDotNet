using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Verifier = PoshDotNet.Cmdlets.Analyzers.Tests.AnalyzerVerifier<PoshDotNet.Cmdlets.Analyzers.CmdletDeclarationAnalyzer>;

namespace PoshDotNet.Cmdlets.Analyzers.Tests;

/// <summary>
/// Provides the tests of <c>PDN1005</c>, which reports two parameters with the same position in one parameter set.
/// </summary>
/// <remarks>
/// The cases follow the measurements that <c>tests/repro/PDN1005</c> keeps: which parameters clash in Windows PowerShell
/// 5.1 and PowerShell 7, and which bind.
/// </remarks>
public sealed class DuplicatePositionTests
{
	/// <summary>
	/// Verifies that two parameters without a set name at one position are reported in every parameter set.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[MemberData(nameof(EngineData.All), MemberType = typeof(EngineData))]
	public async Task ReportsTwoParametersWithoutASetName(PowerShellEngine engine)
	{
		const string source = """
			using System.Management.Automation;

			[Cmdlet(VerbsDiagnostic.Test, "Thing")]
			public sealed class TestThingCommand : PSCmdlet
			{
				[Parameter({|#1:Position = 0|})]
				public string First { get; set; }

				[Parameter({|#0:Position = 0|})]
				public string Second { get; set; }
			}
			""";

		await Verifier.VerifyAsync(
			engine,
			source,
			Verifier.Diagnostic(Descriptors.DuplicatePosition)
				.WithLocation(0)
				.WithLocation(1)
				.WithArguments("Second", "0", "First", "every parameter set"));
	}

	/// <summary>
	/// Verifies that two parameters at one position in the same named set are reported in that set.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[MemberData(nameof(EngineData.All), MemberType = typeof(EngineData))]
	public async Task ReportsTwoParametersInOneNamedSet(PowerShellEngine engine)
	{
		const string source = """
			using System.Management.Automation;

			[Cmdlet(VerbsDiagnostic.Test, "Thing")]
			public sealed class TestThingCommand : PSCmdlet
			{
				[Parameter(ParameterSetName = "ByName", {|#1:Position = 0|})]
				public string First { get; set; }

				[Parameter(ParameterSetName = "ByName", {|#0:Position = 0|})]
				public string Second { get; set; }
			}
			""";

		await Verifier.VerifyAsync(
			engine,
			source,
			Verifier.Diagnostic(Descriptors.DuplicatePosition)
				.WithLocation(0)
				.WithLocation(1)
				.WithArguments("Second", "0", "First", "parameter set 'ByName'"));
	}

	/// <summary>
	/// Verifies that an empty set name, and <c>__AllParameterSets</c> in any letter case, put a parameter in every set.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[MemberData(nameof(EngineData.All), MemberType = typeof(EngineData))]
	public async Task ReportsEverySpellingOfEverySet(PowerShellEngine engine)
	{
		const string source = """
			using System.Management.Automation;

			[Cmdlet(VerbsDiagnostic.Test, "Thing")]
			public sealed class TestThingCommand : PSCmdlet
			{
				[Parameter({|#1:Position = 0|})]
				public string First { get; set; }

				[Parameter(ParameterSetName = "__allparametersets", {|#0:Position = 0|})]
				public string Second { get; set; }

				[Parameter(ParameterSetName = "", {|#2:Position = 0|})]
				public string Third { get; set; }
			}
			""";

		await Verifier.VerifyAsync(
			engine,
			source,
			Verifier.Diagnostic(Descriptors.DuplicatePosition)
				.WithLocation(0)
				.WithLocation(1)
				.WithArguments("Second", "0", "First", "every parameter set"),
			Verifier.Diagnostic(Descriptors.DuplicatePosition)
				.WithLocation(2)
				.WithLocation(1)
				.WithArguments("Third", "0", "First", "every parameter set"));
	}

	/// <summary>
	/// Verifies that a public field counts as a parameter.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[MemberData(nameof(EngineData.All), MemberType = typeof(EngineData))]
	public async Task ReportsAFieldAndAProperty(PowerShellEngine engine)
	{
		const string source = """
			using System.Management.Automation;

			[Cmdlet(VerbsDiagnostic.Test, "Thing")]
			public sealed class TestThingCommand : PSCmdlet
			{
				[Parameter({|#1:Position = 0|})]
				public string First;

				[Parameter({|#0:Position = 0|})]
				public string Second { get; set; }
			}
			""";

		await Verifier.VerifyAsync(
			engine,
			source,
			Verifier.Diagnostic(Descriptors.DuplicatePosition)
				.WithLocation(0)
				.WithLocation(1)
				.WithArguments("Second", "0", "First", "every parameter set"));
	}

	/// <summary>
	/// Verifies that each later parameter at a position is reported against the first one.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[MemberData(nameof(EngineData.All), MemberType = typeof(EngineData))]
	public async Task ReportsEachLaterParameterAtAPosition(PowerShellEngine engine)
	{
		const string source = """
			using System.Management.Automation;

			[Cmdlet(VerbsDiagnostic.Test, "Thing")]
			public sealed class TestThingCommand : PSCmdlet
			{
				[Parameter({|#1:Position = 1|})]
				public string First { get; set; }

				[Parameter({|#0:Position = 1|})]
				public string Second { get; set; }

				[Parameter({|#2:Position = 1|})]
				public string Third { get; set; }
			}
			""";

		await Verifier.VerifyAsync(
			engine,
			source,
			Verifier.Diagnostic(Descriptors.DuplicatePosition)
				.WithLocation(0)
				.WithLocation(1)
				.WithArguments("Second", "1", "First", "every parameter set"),
			Verifier.Diagnostic(Descriptors.DuplicatePosition)
				.WithLocation(2)
				.WithLocation(1)
				.WithArguments("Third", "1", "First", "every parameter set"));
	}

	/// <summary>
	/// Verifies that two parameters that share a position in two sets are reported once for each set.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[MemberData(nameof(EngineData.All), MemberType = typeof(EngineData))]
	public async Task ReportsEachSetWhereTwoParametersShareAPosition(PowerShellEngine engine)
	{
		const string source = """
			using System.Management.Automation;

			[Cmdlet(VerbsDiagnostic.Test, "Thing")]
			public sealed class TestThingCommand : PSCmdlet
			{
				[Parameter(ParameterSetName = "A", {|#1:Position = 0|})]
				[Parameter(ParameterSetName = "B", {|#3:Position = 0|})]
				public string First { get; set; }

				[Parameter(ParameterSetName = "A", {|#0:Position = 0|})]
				[Parameter(ParameterSetName = "B", {|#2:Position = 0|})]
				public string Second { get; set; }
			}
			""";

		await Verifier.VerifyAsync(
			engine,
			source,
			Verifier.Diagnostic(Descriptors.DuplicatePosition)
				.WithLocation(0)
				.WithLocation(1)
				.WithArguments("Second", "0", "First", "parameter set 'A'"),
			Verifier.Diagnostic(Descriptors.DuplicatePosition)
				.WithLocation(2)
				.WithLocation(3)
				.WithArguments("Second", "0", "First", "parameter set 'B'"));
	}

	/// <summary>
	/// Verifies that a clash in a set other than the default set is reported, because it fails a positional call that the
	/// default set could bind.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[MemberData(nameof(EngineData.All), MemberType = typeof(EngineData))]
	public async Task ReportsAClashOutsideTheDefaultSet(PowerShellEngine engine)
	{
		const string source = """
			using System.Management.Automation;

			[Cmdlet(VerbsDiagnostic.Test, "Thing", DefaultParameterSetName = "B")]
			public sealed class TestThingCommand : PSCmdlet
			{
				[Parameter(ParameterSetName = "A", {|#1:Position = 0|})]
				public string A1 { get; set; }

				[Parameter(ParameterSetName = "A", {|#0:Position = 0|})]
				public string A2 { get; set; }

				[Parameter(ParameterSetName = "B", Position = 0)]
				public string B1 { get; set; }
			}
			""";

		await Verifier.VerifyAsync(
			engine,
			source,
			Verifier.Diagnostic(Descriptors.DuplicatePosition)
				.WithLocation(0)
				.WithLocation(1)
				.WithArguments("A2", "0", "A1", "parameter set 'A'"));
	}

	/// <summary>
	/// Verifies that a negative position counts like any other.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[MemberData(nameof(EngineData.All), MemberType = typeof(EngineData))]
	public async Task ReportsANegativePosition(PowerShellEngine engine)
	{
		const string source = """
			using System.Management.Automation;

			[Cmdlet(VerbsDiagnostic.Test, "Thing")]
			public sealed class TestThingCommand : PSCmdlet
			{
				[Parameter({|#1:Position = -1|})]
				public string First { get; set; }

				[Parameter({|#0:Position = -1|})]
				public string Second { get; set; }
			}
			""";

		await Verifier.VerifyAsync(
			engine,
			source,
			Verifier.Diagnostic(Descriptors.DuplicatePosition)
				.WithLocation(0)
				.WithLocation(1)
				.WithArguments("Second", "-1", "First", "every parameter set"));
	}

	/// <summary>
	/// Verifies that a cmdlet's parameter that repeats an inherited parameter's position is reported on its own attribute.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[MemberData(nameof(EngineData.All), MemberType = typeof(EngineData))]
	public async Task ReportsAParameterThatRepeatsAnInheritedPosition(PowerShellEngine engine)
	{
		const string source = """
			using System.Management.Automation;

			public abstract class ThingCommandBase : PSCmdlet
			{
				[Parameter({|#1:Position = 0|})]
				public string First { get; set; }
			}

			[Cmdlet(VerbsDiagnostic.Test, "Thing")]
			public sealed class TestThingCommand : ThingCommandBase
			{
				[Parameter({|#0:Position = 0|})]
				public string Second { get; set; }
			}
			""";

		await Verifier.VerifyAsync(
			engine,
			source,
			Verifier.Diagnostic(Descriptors.DuplicatePosition)
				.WithLocation(0)
				.WithLocation(1)
				.WithArguments("Second", "0", "First", "every parameter set"));
	}

	/// <summary>
	/// Verifies that two inherited parameters that clash are reported on the name of the cmdlet that inherits them.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[MemberData(nameof(EngineData.All), MemberType = typeof(EngineData))]
	public async Task ReportsTwoInheritedParametersOnTheCmdlet(PowerShellEngine engine)
	{
		const string source = """
			using System.Management.Automation;

			public abstract class ThingCommandBase : PSCmdlet
			{
				[Parameter({|#1:Position = 0|})]
				public string First { get; set; }

				[Parameter({|#2:Position = 0|})]
				public string Second { get; set; }
			}

			[Cmdlet(VerbsDiagnostic.Test, "Thing")]
			public sealed class {|#0:TestThingCommand|} : ThingCommandBase
			{
			}
			""";

		await Verifier.VerifyAsync(
			engine,
			source,
			Verifier.Diagnostic(Descriptors.DuplicatePosition)
				.WithLocation(0)
				.WithLocation(1)
				.WithLocation(2)
				.WithArguments("Second", "0", "First", "every parameter set"));
	}

	/// <summary>
	/// Verifies that a derived property of another type leaves the base class's parameter in place, so its position still
	/// clashes.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[MemberData(nameof(EngineData.All), MemberType = typeof(EngineData))]
	public async Task ReportsAParameterThatAPropertyOfAnotherTypeDoesNotHide(PowerShellEngine engine)
	{
		const string source = """
			using System.Management.Automation;

			public abstract class ThingCommandBase : PSCmdlet
			{
				[Parameter({|#1:Position = 0|})]
				public string First { get; set; }
			}

			[Cmdlet(VerbsDiagnostic.Test, "Thing")]
			public sealed class TestThingCommand : ThingCommandBase
			{
				public new int First { get; set; }

				[Parameter({|#0:Position = 0|})]
				public string Second { get; set; }
			}
			""";

		await Verifier.VerifyAsync(
			engine,
			source,
			Verifier.Diagnostic(Descriptors.DuplicatePosition)
				.WithLocation(0)
				.WithLocation(1)
				.WithArguments("Second", "0", "First", "every parameter set"));
	}

	/// <summary>
	/// Verifies that a derived field leaves the base class's field parameter in place, so its position still clashes.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[MemberData(nameof(EngineData.All), MemberType = typeof(EngineData))]
	public async Task ReportsAFieldParameterThatADerivedFieldDoesNotHide(PowerShellEngine engine)
	{
		const string source = """
			using System.Management.Automation;

			public abstract class ThingCommandBase : PSCmdlet
			{
				[Parameter({|#1:Position = 0|})]
				public string First;
			}

			[Cmdlet(VerbsDiagnostic.Test, "Thing")]
			public sealed class TestThingCommand : ThingCommandBase
			{
				public new string First;

				[Parameter({|#0:Position = 0|})]
				public string Second { get; set; }
			}
			""";

		await Verifier.VerifyAsync(
			engine,
			source,
			Verifier.Diagnostic(Descriptors.DuplicatePosition)
				.WithLocation(0)
				.WithLocation(1)
				.WithArguments("Second", "0", "First", "every parameter set"));
	}

	/// <summary>
	/// Verifies that a parameter in every set and one in a named set don't clash, since PowerShell binds the argument to
	/// both.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[MemberData(nameof(EngineData.All), MemberType = typeof(EngineData))]
	public async Task IgnoresAParameterInEverySetBesideOneInANamedSet(PowerShellEngine engine)
	{
		const string source = """
			using System.Management.Automation;

			[Cmdlet(VerbsDiagnostic.Test, "Thing")]
			public sealed class TestThingCommand : PSCmdlet
			{
				[Parameter(Position = 0)]
				public string First { get; set; }

				[Parameter(ParameterSetName = "A", Position = 0)]
				public string Second { get; set; }
			}
			""";

		await Verifier.VerifyAsync(engine, source);
	}

	/// <summary>
	/// Verifies that a <c>ValueFromRemainingArguments</c> parameter doesn't clash, since PowerShell skips it when it binds
	/// positions.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[MemberData(nameof(EngineData.All), MemberType = typeof(EngineData))]
	public async Task IgnoresAValueFromRemainingArgumentsParameter(PowerShellEngine engine)
	{
		const string source = """
			using System.Management.Automation;

			[Cmdlet(VerbsDiagnostic.Test, "Thing")]
			public sealed class TestThingCommand : PSCmdlet
			{
				[Parameter(Position = 0)]
				public string First { get; set; }

				[Parameter(Position = 0, ValueFromRemainingArguments = true)]
				public string[] Rest { get; set; }
			}
			""";

		await Verifier.VerifyAsync(engine, source);
	}

	/// <summary>
	/// Verifies that set names that differ only in letter case are two sets.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[MemberData(nameof(EngineData.All), MemberType = typeof(EngineData))]
	public async Task IgnoresSetNamesThatDifferInCase(PowerShellEngine engine)
	{
		const string source = """
			using System.Management.Automation;

			[Cmdlet(VerbsDiagnostic.Test, "Thing", DefaultParameterSetName = "ByName")]
			public sealed class TestThingCommand : PSCmdlet
			{
				[Parameter(ParameterSetName = "ByName", Position = 0)]
				public string First { get; set; }

				[Parameter(ParameterSetName = "byname", Position = 0)]
				public string Second { get; set; }
			}
			""";

		await Verifier.VerifyAsync(engine, source);
	}

	/// <summary>
	/// Verifies that parameters at one position in different sets, and one parameter at one position in two sets, don't
	/// clash.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[MemberData(nameof(EngineData.All), MemberType = typeof(EngineData))]
	public async Task IgnoresPositionsInDifferentSets(PowerShellEngine engine)
	{
		const string source = """
			using System.Management.Automation;

			[Cmdlet(VerbsDiagnostic.Test, "Thing", DefaultParameterSetName = "ByName")]
			public sealed class TestThingCommand : PSCmdlet
			{
				[Parameter(ParameterSetName = "ByName", Position = 0, Mandatory = true)]
				public string Name { get; set; }

				[Parameter(ParameterSetName = "ById", Position = 0, Mandatory = true)]
				public int Id { get; set; }

				[Parameter(ParameterSetName = "ByName", Position = 1)]
				[Parameter(ParameterSetName = "ById", Position = 1)]
				public string Path { get; set; }
			}
			""";

		await Verifier.VerifyAsync(engine, source);
	}

	/// <summary>
	/// Verifies that members PowerShell doesn't read as parameters don't clash: non-public, static, and unattributed ones.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[MemberData(nameof(EngineData.All), MemberType = typeof(EngineData))]
	public async Task IgnoresMembersThatAreNotParameters(PowerShellEngine engine)
	{
		const string source = """
			using System.Management.Automation;

			[Cmdlet(VerbsDiagnostic.Test, "Thing")]
			public sealed class TestThingCommand : PSCmdlet
			{
				[Parameter(Position = 0)]
				public string Visible { get; set; }

				[Parameter(Position = 0)]
				internal string Internal { get; set; }

				[Parameter(Position = 0)]
				public static string Shared { get; set; }

				[Parameter(Position = 0)]
				private string _hidden;

				public string Plain { get; set; }
			}
			""";

		await Verifier.VerifyAsync(engine, source);
	}

	/// <summary>
	/// Verifies that a position of <see cref="int.MinValue"/> is no position.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[MemberData(nameof(EngineData.All), MemberType = typeof(EngineData))]
	public async Task IgnoresPositionMinValue(PowerShellEngine engine)
	{
		const string source = """
			using System.Management.Automation;

			[Cmdlet(VerbsDiagnostic.Test, "Thing")]
			public sealed class TestThingCommand : PSCmdlet
			{
				[Parameter(Position = int.MinValue)]
				public string First { get; set; }

				[Parameter(Position = int.MinValue)]
				public string Second { get; set; }
			}
			""";

		await Verifier.VerifyAsync(engine, source);
	}

	/// <summary>
	/// Verifies that a base class's parameter that a derived property hides doesn't clash: an override without
	/// <c>[Parameter]</c>, and a property of the same type.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[MemberData(nameof(EngineData.All), MemberType = typeof(EngineData))]
	public async Task IgnoresHiddenParameters(PowerShellEngine engine)
	{
		const string source = """
			using System.Management.Automation;

			public abstract class ThingCommandBase : PSCmdlet
			{
				[Parameter(Position = 0)]
				public virtual string Overridden { get; set; }

				[Parameter(Position = 0)]
				public string Replaced { get; set; }
			}

			[Cmdlet(VerbsDiagnostic.Test, "Thing")]
			public sealed class TestThingCommand : ThingCommandBase
			{
				public override string Overridden { get; set; }

				public new string Replaced { get; set; }

				[Parameter(Position = 0)]
				public string Second { get; set; }
			}
			""";

		await Verifier.VerifyAsync(engine, source);
	}

	/// <summary>
	/// Verifies that classes PowerShell doesn't register as cmdlets aren't checked: one without its own <c>[Cmdlet]</c>,
	/// an abstract one, a generic one, and one that doesn't derive from <c>Cmdlet</c>.
	/// </summary>
	/// <param name="engine">The engine to compile against.</param>
	/// <returns>A task that represents the test.</returns>
	[Theory]
	[MemberData(nameof(EngineData.All), MemberType = typeof(EngineData))]
	public async Task IgnoresClassesThatAreNotCmdlets(PowerShellEngine engine)
	{
		const string source = """
			using System.Management.Automation;

			public class NoAttributeCommand : PSCmdlet
			{
				[Parameter(Position = 0)]
				public string First { get; set; }

				[Parameter(Position = 0)]
				public string Second { get; set; }
			}

			[Cmdlet(VerbsDiagnostic.Test, "Abstract")]
			public abstract class TestAbstractCommand : PSCmdlet
			{
				[Parameter(Position = 0)]
				public string First { get; set; }

				[Parameter(Position = 0)]
				public string Second { get; set; }
			}

			[Cmdlet(VerbsDiagnostic.Test, "Generic")]
			public sealed class TestGenericCommand<T> : PSCmdlet
			{
				[Parameter(Position = 0)]
				public string First { get; set; }

				[Parameter(Position = 0)]
				public string Second { get; set; }
			}

			[Cmdlet(VerbsDiagnostic.Test, "NotCmdlet")]
			public sealed class TestNotCmdletCommand
			{
				[Parameter(Position = 0)]
				public string First { get; set; }

				[Parameter(Position = 0)]
				public string Second { get; set; }
			}
			""";

		await Verifier.VerifyAsync(engine, source);
	}

	/// <summary>
	/// Verifies that an <c>.editorconfig</c> line for the rule's ID turns it off.
	/// </summary>
	/// <remarks>
	/// The test harness applies ID lines but not <c>dotnet_analyzer_diagnostic.category-*</c> lines, which only a build
	/// applies, so <see cref="DescriptorTests"/> checks the category string instead.
	/// </remarks>
	/// <returns>A task that represents the test.</returns>
	[Fact]
	public async Task EditorConfigSeverityTurnsTheRuleOff()
	{
		const string source = """
			using System.Management.Automation;

			[Cmdlet(VerbsDiagnostic.Test, "Thing")]
			public sealed class TestThingCommand : PSCmdlet
			{
				[Parameter(Position = 0)]
				public string First { get; set; }

				[Parameter(Position = 0)]
				public string Second { get; set; }
			}
			""";

		const string editorConfig = """
			root = true

			[*]
			dotnet_diagnostic.PDN1005.severity = none
			""";

		CSharpAnalyzerTest<CmdletDeclarationAnalyzer, DefaultVerifier> test = new()
		{
			ReferenceAssemblies = EngineReferences.PowerShell7,
			TestState =
			{
				Sources = { source },
				AnalyzerConfigFiles = { ("/.editorconfig", editorConfig) },
			},
		};

		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	/// <summary>
	/// Verifies that the analyzer does nothing in a compilation that doesn't reference PowerShell.
	/// </summary>
	/// <returns>A task that represents the test.</returns>
	[Fact]
	public async Task DoesNothingWithoutPowerShell()
	{
		const string source = """
			public sealed class Thing
			{
				public string Name { get; set; }
			}
			""";

		CSharpAnalyzerTest<CmdletDeclarationAnalyzer, DefaultVerifier> test = new()
		{
			ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
			TestCode = source,
		};

		await test.RunAsync(TestContext.Current.CancellationToken);
	}
}
