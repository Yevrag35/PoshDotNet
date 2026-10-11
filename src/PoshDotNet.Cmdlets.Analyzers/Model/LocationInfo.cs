using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace PoshDotNet.Cmdlets.Analyzers.Model;

/// <summary>
/// Represents a source position as plain values, so a model that holds it roots no syntax tree.
/// </summary>
/// <param name="FilePath">The path of the file that holds the position.</param>
/// <param name="TextSpan">The character span within that file.</param>
/// <param name="LineSpan">The line and column span within that file.</param>
internal sealed record LocationInfo(string FilePath, TextSpan TextSpan, LinePositionSpan LineSpan)
{
	/// <summary>
	/// Rebuilds the location this position was taken from, in the syntax tree of the specified compilation that has its
	/// file path.
	/// </summary>
	/// <remarks>
	/// A location in the compilation's own syntax tree is one that suppressions and the IDE recognize. When no tree has
	/// the file path, the location points at the file by path instead.
	/// </remarks>
	/// <param name="compilation">The compilation whose syntax trees hold the position.</param>
	/// <returns>The rebuilt location.</returns>
	public Location ToLocation(Compilation compilation)
	{
		foreach (SyntaxTree tree in compilation.SyntaxTrees)
		{
			if (string.Equals(tree.FilePath, this.FilePath, StringComparison.Ordinal))
			{
				return Location.Create(tree, this.TextSpan);
			}
		}

		return Location.Create(this.FilePath, this.TextSpan, this.LineSpan);
	}

	/// <summary>
	/// Captures the position of a syntax node.
	/// </summary>
	/// <param name="node">The node to take the position of.</param>
	/// <returns>The captured position, or <see langword="null"/> when <paramref name="node"/> is <see langword="null"/>.</returns>
	public static LocationInfo? From(SyntaxNode? node)
	{
		return From(node?.GetLocation());
	}

	/// <summary>
	/// Captures a location in source.
	/// </summary>
	/// <param name="location">The location to capture.</param>
	/// <returns>
	/// The captured position, or <see langword="null"/> when <paramref name="location"/> is <see langword="null"/> or
	/// isn't in source.
	/// </returns>
	public static LocationInfo? From(Location? location)
	{
		return location?.SourceTree is null
			? null
			: new LocationInfo(location.SourceTree.FilePath, location.SourceSpan, location.GetLineSpan().Span);
	}
}
