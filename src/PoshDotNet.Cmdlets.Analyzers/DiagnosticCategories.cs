namespace PoshDotNet.Cmdlets.Analyzers;

/// <summary>
/// Provides the category strings of PoshDotNet's diagnostic ID blocks.
/// </summary>
/// <remarks>
/// Each block of IDs maps to one category, and each category string starts with <c>PoshDotNet.</c>, so an
/// <c>.editorconfig</c> line such as <c>dotnet_analyzer_diagnostic.category-PoshDotNet.Declarations.severity</c> changes
/// PoshDotNet's rules only.
/// </remarks>
internal static class DiagnosticCategories
{
	/// <summary>The category of the <c>PDN1xxx</c> rules: verbs, names, aliases, positions, and get-only parameters.</summary>
	public const string Declarations = "PoshDotNet.Declarations";
}
