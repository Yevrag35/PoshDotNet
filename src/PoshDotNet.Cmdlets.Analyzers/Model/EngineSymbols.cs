using Microsoft.CodeAnalysis;

namespace PoshDotNet.Cmdlets.Analyzers.Model;

/// <summary>
/// Provides the engine types that one compilation references, looked up once for that compilation.
/// </summary>
/// <remarks>
/// The symbols belong to the compilation they came from, so an instance serves that compilation's analysis only.
/// </remarks>
internal sealed class EngineSymbols
{
	/// <summary>The metadata name of the base class of every cmdlet.</summary>
	private const string CMDLET = "System.Management.Automation.Cmdlet";

	/// <summary>The metadata name of the attribute that names a cmdlet class's verb and noun.</summary>
	private const string CMDLET_ATTRIBUTE = "System.Management.Automation.CmdletAttribute";

	/// <summary>The metadata name of the attribute that makes a member a parameter.</summary>
	private const string PARAMETER_ATTRIBUTE = "System.Management.Automation.ParameterAttribute";

	/// <summary>The metadata name of the attribute that gives a parameter its aliases.</summary>
	private const string ALIAS_ATTRIBUTE = "System.Management.Automation.AliasAttribute";

	/// <summary>
	/// Initializes a new <see cref="EngineSymbols"/> instance with the specified engine types.
	/// </summary>
	/// <param name="cmdlet">The <c>Cmdlet</c> class.</param>
	/// <param name="cmdletAttribute">The <c>CmdletAttribute</c> class.</param>
	/// <param name="parameterAttribute">The <c>ParameterAttribute</c> class.</param>
	/// <param name="aliasAttribute">The <c>AliasAttribute</c> class.</param>
	private EngineSymbols(
		INamedTypeSymbol cmdlet,
		INamedTypeSymbol cmdletAttribute,
		INamedTypeSymbol parameterAttribute,
		INamedTypeSymbol aliasAttribute)
	{
		this.Cmdlet = cmdlet;
		this.CmdletAttribute = cmdletAttribute;
		this.ParameterAttribute = parameterAttribute;
		this.AliasAttribute = aliasAttribute;
	}

	/// <summary>
	/// Gets the <c>System.Management.Automation.Cmdlet</c> class.
	/// </summary>
	/// <value>
	/// The base class of every cmdlet.
	/// </value>
	public INamedTypeSymbol Cmdlet { get; }

	/// <summary>
	/// Gets the <c>System.Management.Automation.CmdletAttribute</c> class.
	/// </summary>
	/// <value>
	/// The attribute that names a cmdlet class's verb and noun.
	/// </value>
	public INamedTypeSymbol CmdletAttribute { get; }

	/// <summary>
	/// Gets the <c>System.Management.Automation.ParameterAttribute</c> class.
	/// </summary>
	/// <value>
	/// The attribute that makes a member a parameter.
	/// </value>
	public INamedTypeSymbol ParameterAttribute { get; }

	/// <summary>
	/// Gets the <c>System.Management.Automation.AliasAttribute</c> class.
	/// </summary>
	/// <value>
	/// The attribute that gives a parameter its aliases.
	/// </value>
	public INamedTypeSymbol AliasAttribute { get; }

	/// <summary>
	/// Looks up the engine types in the specified compilation.
	/// </summary>
	/// <param name="compilation">The compilation to search.</param>
	/// <returns>
	/// The engine types, or <see langword="null"/> when the compilation doesn't reference PowerShell or a type is missing
	/// or ambiguous.
	/// </returns>
	public static EngineSymbols? Create(Compilation compilation)
	{
		INamedTypeSymbol? cmdlet = compilation.GetTypeByMetadataName(CMDLET);
		INamedTypeSymbol? cmdletAttribute = compilation.GetTypeByMetadataName(CMDLET_ATTRIBUTE);
		INamedTypeSymbol? parameterAttribute = compilation.GetTypeByMetadataName(PARAMETER_ATTRIBUTE);
		INamedTypeSymbol? aliasAttribute = compilation.GetTypeByMetadataName(ALIAS_ATTRIBUTE);

		return cmdlet is null || cmdletAttribute is null || parameterAttribute is null || aliasAttribute is null
			? null
			: new EngineSymbols(cmdlet, cmdletAttribute, parameterAttribute, aliasAttribute);
	}
}
