// The cmdlets behind PDN1005: two parameters with the same Position in one parameter set.
// C# 5-compatible on purpose: Windows PowerShell 5.1's Add-Type uses the legacy compiler.
using System.Management.Automation;

namespace PoshDotNet.Repro.Pdn1005
{
	// PDN1005 reports these: a positional argument fails to bind.

	[Cmdlet("Test", "Pdn1005NoSetPair")]
	public class TestNoSetPair : PSCmdlet
	{
		[Parameter(Position = 0)] public string First { get; set; }
		[Parameter(Position = 0)] public string Second { get; set; }
		protected override void ProcessRecord() { WriteObject("set=" + ParameterSetName + " First=" + First + " Second=" + Second); }
	}

	[Cmdlet("Test", "Pdn1005NamedSetPair")]
	public class TestNamedSetPair : PSCmdlet
	{
		[Parameter(ParameterSetName = "ByName", Position = 0)] public string First { get; set; }
		[Parameter(ParameterSetName = "ByName", Position = 0)] public string Second { get; set; }
		protected override void ProcessRecord() { WriteObject("set=" + ParameterSetName + " First=" + First + " Second=" + Second); }
	}

	[Cmdlet("Test", "Pdn1005OtherSetPair", DefaultParameterSetName = "B")]
	public class TestOtherSetPair : PSCmdlet
	{
		[Parameter(ParameterSetName = "A", Position = 0)] public string A1 { get; set; }
		[Parameter(ParameterSetName = "A", Position = 0)] public string A2 { get; set; }
		[Parameter(ParameterSetName = "B", Position = 0)] public string B1 { get; set; }
		protected override void ProcessRecord() { WriteObject("set=" + ParameterSetName + " A1=" + A1 + " A2=" + A2 + " B1=" + B1); }
	}

	[Cmdlet("Test", "Pdn1005AllSetsSpelling")]
	public class TestAllSetsSpelling : PSCmdlet
	{
		[Parameter(Position = 0)] public string First { get; set; }
		[Parameter(ParameterSetName = "__allparametersets", Position = 0)] public string Second { get; set; }
		protected override void ProcessRecord() { WriteObject("set=" + ParameterSetName + " First=" + First + " Second=" + Second); }
	}

	[Cmdlet("Test", "Pdn1005EmptySetName")]
	public class TestEmptySetName : PSCmdlet
	{
		[Parameter(ParameterSetName = "", Position = 0)] public string First { get; set; }
		[Parameter(Position = 0)] public string Second { get; set; }
		protected override void ProcessRecord() { WriteObject("set=" + ParameterSetName + " First=" + First + " Second=" + Second); }
	}

	public abstract class InheritedPairBase : PSCmdlet
	{
		[Parameter(Position = 0)] public string First { get; set; }
	}

	[Cmdlet("Test", "Pdn1005InheritedPair")]
	public class TestInheritedPair : InheritedPairBase
	{
		[Parameter(Position = 0)] public string Second { get; set; }
		protected override void ProcessRecord() { WriteObject("set=" + ParameterSetName + " First=" + First + " Second=" + Second); }
	}

	[Cmdlet("Test", "Pdn1005FieldPair")]
	public class TestFieldPair : PSCmdlet
	{
		[Parameter(Position = 0)] public string First;
		[Parameter(Position = 0)] public string Second { get; set; }
		protected override void ProcessRecord() { WriteObject("set=" + ParameterSetName + " First=" + First + " Second=" + Second); }
	}

	[Cmdlet("Test", "Pdn1005NegativePair")]
	public class TestNegativePair : PSCmdlet
	{
		[Parameter(Position = -1)] public string First { get; set; }
		[Parameter(Position = -1)] public string Second { get; set; }
		protected override void ProcessRecord() { WriteObject("set=" + ParameterSetName + " First=" + First + " Second=" + Second); }
	}

	public abstract class OtherTypeBase : PSCmdlet
	{
		[Parameter(Position = 0)] public string First { get; set; }
	}

	// A derived property of another type doesn't hide the base class's parameter.
	[Cmdlet("Test", "Pdn1005OtherTypeKeepsBase")]
	public class TestOtherTypeKeepsBase : OtherTypeBase
	{
		public new int First { get; set; }
		[Parameter(Position = 0)] public string Second { get; set; }
		protected override void ProcessRecord() { WriteObject("set=" + ParameterSetName + " Second=" + Second); }
	}

	public abstract class FieldBase : PSCmdlet
	{
		[Parameter(Position = 0)] public string First;
	}

	// A derived field doesn't hide the base class's field parameter.
	[Cmdlet("Test", "Pdn1005FieldKeepsBase")]
	public class TestFieldKeepsBase : FieldBase
	{
		public new string First;
		[Parameter(Position = 0)] public string Second { get; set; }
		protected override void ProcessRecord() { WriteObject("set=" + ParameterSetName + " Second=" + Second); }
	}

	// PDN1005 leaves these alone: a positional argument binds.

	[Cmdlet("Test", "Pdn1005NoSetAndNamedSet")]
	public class TestNoSetAndNamedSet : PSCmdlet
	{
		[Parameter(Position = 0)] public string First { get; set; }
		[Parameter(ParameterSetName = "A", Position = 0)] public string Second { get; set; }
		protected override void ProcessRecord() { WriteObject("set=" + ParameterSetName + " First=" + First + " Second=" + Second); }
	}

	[Cmdlet("Test", "Pdn1005RemainingArguments")]
	public class TestRemainingArguments : PSCmdlet
	{
		[Parameter(Position = 0)] public string First { get; set; }
		[Parameter(Position = 0, ValueFromRemainingArguments = true)] public string[] Rest { get; set; }
		protected override void ProcessRecord()
		{
			WriteObject("set=" + ParameterSetName + " First=" + First + " Rest=" + (Rest == null ? "<null>" : string.Join(",", Rest)));
		}
	}

	[Cmdlet("Test", "Pdn1005SetNameCase", DefaultParameterSetName = "ByName")]
	public class TestSetNameCase : PSCmdlet
	{
		[Parameter(ParameterSetName = "ByName", Position = 0)] public string First { get; set; }
		[Parameter(ParameterSetName = "byname", Position = 0)] public string Second { get; set; }
		protected override void ProcessRecord() { WriteObject("set=" + ParameterSetName + " First=" + First + " Second=" + Second); }
	}

	[Cmdlet("Test", "Pdn1005OneParameterTwoSets", DefaultParameterSetName = "A")]
	public class TestOneParameterTwoSets : PSCmdlet
	{
		[Parameter(ParameterSetName = "A", Position = 0)]
		[Parameter(ParameterSetName = "B", Position = 0)]
		public string Name { get; set; }
		[Parameter(ParameterSetName = "B")] public SwitchParameter Force { get; set; }
		protected override void ProcessRecord() { WriteObject("set=" + ParameterSetName + " Name=" + Name); }
	}

	[Cmdlet("Test", "Pdn1005NotParameters")]
	public class TestNotParameters : PSCmdlet
	{
		[Parameter(Position = 0)] public string Visible { get; set; }
		[Parameter(Position = 0)] internal string Internal { get; set; }
		[Parameter(Position = 0)] public static string Shared { get; set; }
		protected override void ProcessRecord() { WriteObject("set=" + ParameterSetName + " Visible=" + Visible); }
	}

	public abstract class SameTypeBase : PSCmdlet
	{
		[Parameter(Position = 0)] public string First { get; set; }
	}

	// A derived property of the same type hides the base class's parameter.
	[Cmdlet("Test", "Pdn1005SameTypeHidesBase")]
	public class TestSameTypeHidesBase : SameTypeBase
	{
		public new string First { get; set; }
		[Parameter(Position = 0)] public string Second { get; set; }
		protected override void ProcessRecord() { WriteObject("set=" + ParameterSetName + " Second=" + Second); }
	}

	public abstract class VirtualBase : PSCmdlet
	{
		[Parameter(Position = 0)] public virtual string First { get; set; }
	}

	// An override without [Parameter] isn't a parameter, and hides the base declaration's.
	[Cmdlet("Test", "Pdn1005OverrideHidesBase")]
	public class TestOverrideHidesBase : VirtualBase
	{
		public override string First { get; set; }
		[Parameter(Position = 0)] public string Second { get; set; }
		protected override void ProcessRecord() { WriteObject("set=" + ParameterSetName + " Second=" + Second); }
	}

	[Cmdlet("Test", "Pdn1005MinValuePair")]
	public class TestMinValuePair : PSCmdlet
	{
		[Parameter(Position = int.MinValue)] public string First { get; set; }
		[Parameter(Position = int.MinValue)] public string Second { get; set; }
		protected override void ProcessRecord() { WriteObject("set=" + ParameterSetName + " First=" + First + " Second=" + Second); }
	}
}
