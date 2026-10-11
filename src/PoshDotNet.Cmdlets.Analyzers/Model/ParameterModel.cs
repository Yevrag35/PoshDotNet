namespace PoshDotNet.Cmdlets.Analyzers.Model;

/// <summary>
/// Represents a parameter of a cmdlet: a public instance property or field that carries a <c>[Parameter]</c> attribute.
/// </summary>
/// <remarks>
/// Two parameters are equal when every value is equal, locations included.
/// </remarks>
/// <param name="Name">The parameter's name, which is the member's name.</param>
/// <param name="TypeName">The parameter's type, fully qualified.</param>
/// <param name="IsField">
/// A value that indicates whether a field declares the parameter. When it's <see langword="false"/>, a property does.
/// </param>
/// <param name="DeclaringTypeName">The name of the class that declares the member.</param>
/// <param name="Aliases">The names from the member's <c>[Alias]</c> attributes, in the order they're written.</param>
/// <param name="Sets">One entry for each of the member's <c>[Parameter]</c> attributes, in the order they're written.</param>
/// <param name="Location">The position of the member's name in source, or <see langword="null"/> when it isn't in source.</param>
internal sealed record ParameterModel(
	string Name,
	string TypeName,
	bool IsField,
	string DeclaringTypeName,
	EquatableArray<string> Aliases,
	EquatableArray<ParameterSetEntry> Sets,
	LocationInfo? Location);
