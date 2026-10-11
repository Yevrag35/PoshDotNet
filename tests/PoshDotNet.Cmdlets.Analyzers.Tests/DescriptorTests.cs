using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PoshDotNet.Cmdlets.Analyzers.Tests;

/// <summary>
/// Provides the tests of the IDs and categories that every PoshDotNet diagnostic keeps.
/// </summary>
/// <remarks>
/// Users write a rule's ID and its category in <c>.editorconfig</c> files and suppressions, so both are public. A build
/// applies a line such as <c>dotnet_analyzer_diagnostic.category-PoshDotNet.Declarations.severity = none</c>, which was
/// checked with a real build on 2026-10-11, but the test harness doesn't apply category lines, so these tests check the
/// strings themselves.
/// </remarks>
public sealed class DescriptorTests
{
	/// <summary>
	/// The category of each ID block, keyed by the block's digit.
	/// </summary>
	private static readonly Dictionary<char, string> s_categories = new()
	{
		['1'] = "PoshDotNet.Declarations",
	};

	/// <summary>
	/// The analyzers in the assembly.
	/// </summary>
	private static readonly DiagnosticAnalyzer[] s_analyzers =
	[
		new CmdletDeclarationAnalyzer(),
	];

	/// <summary>
	/// Verifies that each descriptor's ID is <c>PDN</c> plus four digits, that no two descriptors share an ID, and that each
	/// one's category is its block's.
	/// </summary>
	[Fact]
	public void EachDescriptorUsesItsBlocksCategory()
	{
		DiagnosticDescriptor[] descriptors = [.. s_analyzers.SelectMany(static analyzer => analyzer.SupportedDiagnostics)];

		Assert.Equal(descriptors.Length, descriptors.Select(static descriptor => descriptor.Id).Distinct().Count());
		Assert.All(descriptors, static descriptor =>
		{
			Assert.Matches("^PDN[1-9][0-9]{3}$", descriptor.Id);
			Assert.True(s_categories.TryGetValue(descriptor.Id[3], out string? category), $"{descriptor.Id} is in a block with no category.");
			Assert.Equal(category, descriptor.Category);
		});
	}
}
