using Microsoft.CodeAnalysis;

namespace PoshDotNet.Cmdlets.Analyzers;

/// <summary>
/// Provides the descriptors of PoshDotNet's diagnostics.
/// </summary>
internal static class Descriptors
{
	/// <summary>
	/// The descriptor of <c>PDN1005</c>, which reports two parameters with the same position in one parameter set.
	/// </summary>
	/// <remarks>
	/// The message's arguments are the parameter that repeats the position, the position, the parameter that has it
	/// first, and the set: <c>every parameter set</c>, or <c>parameter set 'Name'</c>.
	/// </remarks>
	public static readonly DiagnosticDescriptor DuplicatePosition = new(
		id: "PDN1005",
		title: "Two parameters have the same position in one parameter set",
		messageFormat: "Parameter '{0}' has the same position, {1}, as parameter '{2}' in {3}, so PowerShell can't bind a positional argument",
		category: DiagnosticCategories.Declarations,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description:
			"When two parameters have the same Position in one parameter set, a call that passes a positional argument "
			+ "fails with \"Cannot bind positional parameters because no names were given.\" unless it names one of the "
			+ "two, and Get-Command -Syntax shows only one of them. A parameter that names no set clashes only with "
			+ "another that names none, and a ValueFromRemainingArguments parameter never clashes.");
}
