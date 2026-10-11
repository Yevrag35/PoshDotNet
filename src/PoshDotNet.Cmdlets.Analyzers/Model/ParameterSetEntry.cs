namespace PoshDotNet.Cmdlets.Analyzers.Model;

/// <summary>
/// Represents one <c>[Parameter]</c> attribute: a parameter's membership in one parameter set, with the settings it has
/// there.
/// </summary>
/// <remarks>
/// Two entries are equal when every value is equal, locations included.
/// </remarks>
/// <param name="SetName">
/// The set's name as the attribute spells it, or <see langword="null"/> for a parameter in every set. PowerShell
/// compares set names case-sensitively, but a missing or empty name, and <c>__AllParameterSets</c> in any letter case,
/// mean every set.
/// </param>
/// <param name="Position">
/// The parameter's position in the set, or <see langword="null"/> when it isn't positional there. A <c>Position</c> of
/// <see cref="int.MinValue"/> means no position, as it does to PowerShell.
/// </param>
/// <param name="Mandatory">A value that indicates whether the parameter is mandatory in the set.</param>
/// <param name="ValueFromPipeline">A value that indicates whether the parameter takes pipeline input in the set.</param>
/// <param name="ValueFromPipelineByPropertyName">
/// A value that indicates whether the parameter takes a property of each pipeline input object in the set.
/// </param>
/// <param name="ValueFromRemainingArguments">
/// A value that indicates whether the parameter collects the arguments that no other parameter takes, in which case it
/// takes no position.
/// </param>
/// <param name="Location">The attribute's position in source, or <see langword="null"/> when it isn't in source.</param>
/// <param name="PositionLocation">
/// The position of the attribute's <c>Position</c> argument in source, or <see langword="null"/> when the attribute
/// sets none or isn't in source.
/// </param>
internal sealed record ParameterSetEntry(
	string? SetName,
	int? Position,
	bool Mandatory,
	bool ValueFromPipeline,
	bool ValueFromPipelineByPropertyName,
	bool ValueFromRemainingArguments,
	LocationInfo? Location,
	LocationInfo? PositionLocation)
{
	/// <summary>
	/// Gets a value that indicates whether the entry puts the parameter in every parameter set.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if the attribute names no set; otherwise, <see langword="false"/>.
	/// </value>
	public bool IsInAllSets => this.SetName is null;
}
