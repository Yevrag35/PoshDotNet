namespace PoshDotNet.Cmdlets.Analyzers.Model;

/// <summary>
/// Represents a cmdlet class as PowerShell builds its command metadata: the settings of its <c>[Cmdlet]</c> attribute
/// and the parameters that it and its base classes declare.
/// </summary>
/// <remarks>
/// <para>
/// Every rule reads a cmdlet through this model, so the engine's binding semantics live in one place. The model holds
/// only strings, numbers, and other value-equatable records, never a symbol, a syntax node, or a compilation. Two reads of
/// an unchanged class are equal, which lets an incremental generator cache the model.
/// </para>
/// <para>
/// Two models are equal when every value is equal, locations included.
/// </para>
/// </remarks>
/// <param name="TypeName">The name of the cmdlet class.</param>
/// <param name="Verb">The verb from the <c>[Cmdlet]</c> attribute, or an empty string when the attribute has none.</param>
/// <param name="Noun">The noun from the <c>[Cmdlet]</c> attribute, or an empty string when the attribute has none.</param>
/// <param name="DefaultParameterSetName">
/// The <c>DefaultParameterSetName</c> from the <c>[Cmdlet]</c> attribute, or <see langword="null"/> when it sets none.
/// </param>
/// <param name="SupportsShouldProcess">
/// A value that indicates whether the <c>[Cmdlet]</c> attribute sets <c>SupportsShouldProcess</c>.
/// </param>
/// <param name="Parameters">
/// The parameters, those of base classes first, and each class's in the order it declares them.
/// </param>
/// <param name="Location">The position of the class's name in source, or <see langword="null"/> when it isn't in source.</param>
internal sealed record CmdletModel(
	string TypeName,
	string Verb,
	string Noun,
	string? DefaultParameterSetName,
	bool SupportsShouldProcess,
	EquatableArray<ParameterModel> Parameters,
	LocationInfo? Location);
