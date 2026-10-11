using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PoshDotNet.Cmdlets.Analyzers.Model;

/// <summary>
/// Provides the method that reads a cmdlet class into a <see cref="CmdletModel"/>.
/// </summary>
/// <remarks>
/// The reader follows what PowerShell reads through reflection. The behaviors it relies on were measured in Windows
/// PowerShell 5.1.26100 and PowerShell 7.6.6 on 2026-10-11, and the repro corpus in <c>tests/repro/</c> keeps them.
/// </remarks>
internal static class CmdletModelReader
{
	/// <summary>The set name that a <c>[Parameter]</c> attribute has when it names no set.</summary>
	private const string ALL_PARAMETER_SETS = "__AllParameterSets";

	/// <summary>
	/// Reads the specified class into a <see cref="CmdletModel"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A class is read when it carries its own <c>[Cmdlet]</c> attribute, derives from <c>Cmdlet</c>, and is neither
	/// abstract nor generic, which are the classes PowerShell registers as cmdlets. Its accessibility isn't checked,
	/// because a host can add a non-public cmdlet class to a runspace.
	/// </para>
	/// <para>
	/// The parameters are the public instance properties and fields that carry their own <c>[Parameter]</c> attribute,
	/// including those of base classes. A base class's property is hidden by a derived property with the same name and
	/// type, such as an override, and a base class's field isn't hidden by a derived field. A base member that shares a
	/// name with a derived member of the other kind is treated as hidden.
	/// </para>
	/// </remarks>
	/// <param name="type">The class to read.</param>
	/// <param name="engine">The engine types of the compilation that holds <paramref name="type"/>.</param>
	/// <param name="cancellationToken">The token that cancels the read.</param>
	/// <returns>The model of <paramref name="type"/>, or <see langword="null"/> when it isn't a cmdlet class.</returns>
	/// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is canceled.</exception>
	public static CmdletModel? Read(INamedTypeSymbol type, EngineSymbols engine, CancellationToken cancellationToken)
	{
		if (!IsCmdletClass(type, engine) || FindAttribute(type, engine.CmdletAttribute) is not AttributeData attribute)
		{
			return null;
		}

		string? defaultParameterSetName = null;
		bool supportsShouldProcess = false;
		foreach (KeyValuePair<string, TypedConstant> argument in attribute.NamedArguments)
		{
			switch (argument.Key)
			{
				case "DefaultParameterSetName":
					defaultParameterSetName = argument.Value.Value as string;
					break;

				case "SupportsShouldProcess":
					supportsShouldProcess = argument.Value.Value is true;
					break;
			}
		}

		return new CmdletModel(
			TypeName: type.ToDisplayString(),
			Verb: GetConstructorString(attribute, 0),
			Noun: GetConstructorString(attribute, 1),
			DefaultParameterSetName: defaultParameterSetName,
			SupportsShouldProcess: supportsShouldProcess,
			Parameters: ReadParameters(type, engine, cancellationToken),
			Location: LocationInfo.From(GetSourceLocation(type)));
	}

	/// <summary>
	/// Determines whether PowerShell can register the specified class as a cmdlet, apart from its attribute.
	/// </summary>
	/// <param name="type">The class to check.</param>
	/// <param name="engine">The engine types of the compilation that holds <paramref name="type"/>.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="type"/> is a concrete, non-generic class that derives from <c>Cmdlet</c>;
	/// otherwise, <see langword="false"/>.
	/// </returns>
	private static bool IsCmdletClass(INamedTypeSymbol type, EngineSymbols engine)
	{
		if (type.TypeKind != TypeKind.Class || type.IsAbstract || type.IsStatic)
		{
			return false;
		}

		// A class nested in a generic class is generic too.
		for (INamedTypeSymbol? container = type; container is not null; container = container.ContainingType)
		{
			if (container.TypeParameters.Length > 0)
			{
				return false;
			}
		}

		for (INamedTypeSymbol? current = type.BaseType; current is not null; current = current.BaseType)
		{
			if (SymbolEqualityComparer.Default.Equals(current, engine.Cmdlet))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Reads the parameters of a cmdlet class and its base classes.
	/// </summary>
	/// <param name="type">The cmdlet class.</param>
	/// <param name="engine">The engine types of the compilation that holds <paramref name="type"/>.</param>
	/// <param name="cancellationToken">The token that cancels the read.</param>
	/// <returns>The parameters, those of base classes first, and each class's in the order it declares them.</returns>
	/// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is canceled.</exception>
	private static EquatableArray<ParameterModel> ReadParameters(INamedTypeSymbol type, EngineSymbols engine, CancellationToken cancellationToken)
	{
		// The cmdlet class and its base classes down to the engine's own, most derived first. The engine's classes
		// declare no parameters.
		List<INamedTypeSymbol> classes = [];
		for (INamedTypeSymbol? current = type; current is not null && !IsEngineType(current, engine); current = current.BaseType)
		{
			classes.Add(current);
		}

		// Walking from the most derived class sees a hiding member before the base member it hides.
		List<ISymbol> derivedMembers = [];
		List<ParameterModel>[] declared = new List<ParameterModel>[classes.Count];
		for (int i = 0; i < classes.Count; i++)
		{
			cancellationToken.ThrowIfCancellationRequested();

			ImmutableArray<ISymbol> members = classes[i].GetMembers();
			List<ParameterModel> parameters = [];
			foreach (ISymbol member in members)
			{
				if (IsParameterMember(member, engine) && !IsHidden(member, derivedMembers))
				{
					parameters.Add(ReadParameter(member, engine, cancellationToken));
				}
			}

			declared[i] = parameters;
			foreach (ISymbol member in members)
			{
				if (member is IPropertySymbol or IFieldSymbol && !member.IsImplicitlyDeclared)
				{
					derivedMembers.Add(member);
				}
			}
		}

		List<ParameterModel> ordered = [];
		for (int i = declared.Length - 1; i >= 0; i--)
		{
			ordered.AddRange(declared[i]);
		}

		return new EquatableArray<ParameterModel>(ordered);
	}

	/// <summary>
	/// Determines whether PowerShell reads the specified member as a parameter.
	/// </summary>
	/// <param name="member">The member to check.</param>
	/// <param name="engine">The engine types of the compilation that holds <paramref name="member"/>.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="member"/> is a public instance property or field, other than an indexer,
	/// that carries its own <c>[Parameter]</c> attribute; otherwise, <see langword="false"/>.
	/// </returns>
	private static bool IsParameterMember(ISymbol member, EngineSymbols engine)
	{
		bool isCandidate = member switch
		{
			IPropertySymbol property => !property.IsIndexer,
			IFieldSymbol field => !field.IsImplicitlyDeclared,
			_ => false,
		};

		return isCandidate
			&& !member.IsStatic
			&& member.DeclaredAccessibility == Accessibility.Public
			&& FindAttribute(member, engine.ParameterAttribute) is not null;
	}

	/// <summary>
	/// Determines whether a member of a more derived class hides the specified base class member from reflection.
	/// </summary>
	/// <remarks>
	/// A derived property with the same name and type hides a base property, and one of another type doesn't. A derived
	/// field never hides a base field. Those three were measured. A base member that shares a name with a derived member
	/// of the other kind is treated as hidden, which can only drop a parameter, never add a diagnostic.
	/// </remarks>
	/// <param name="member">The base class member.</param>
	/// <param name="derivedMembers">The properties and fields that the more derived classes declare.</param>
	/// <returns>
	/// <see langword="true"/> if a member of <paramref name="derivedMembers"/> hides <paramref name="member"/>; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	private static bool IsHidden(ISymbol member, List<ISymbol> derivedMembers)
	{
		foreach (ISymbol derived in derivedMembers)
		{
			if (!string.Equals(derived.Name, member.Name, StringComparison.Ordinal))
			{
				continue;
			}

			switch (member, derived)
			{
				case (IPropertySymbol baseProperty, IPropertySymbol derivedProperty):
					if (SymbolEqualityComparer.Default.Equals(baseProperty.Type, derivedProperty.Type))
					{
						return true;
					}

					break;

				case (IFieldSymbol, IFieldSymbol):
					break;

				default:
					return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Reads a parameter member into a <see cref="ParameterModel"/>.
	/// </summary>
	/// <param name="member">The property or field that declares the parameter.</param>
	/// <param name="engine">The engine types of the compilation that holds <paramref name="member"/>.</param>
	/// <param name="cancellationToken">The token that cancels the read.</param>
	/// <returns>The model of the parameter.</returns>
	private static ParameterModel ReadParameter(ISymbol member, EngineSymbols engine, CancellationToken cancellationToken)
	{
		ITypeSymbol type = member is IPropertySymbol property ? property.Type : ((IFieldSymbol)member).Type;
		List<string> aliases = [];
		List<ParameterSetEntry> sets = [];
		foreach (AttributeData attribute in member.GetAttributes())
		{
			if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, engine.ParameterAttribute))
			{
				sets.Add(ReadSetEntry(attribute, cancellationToken));
			}
			else if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, engine.AliasAttribute))
			{
				ReadAliases(attribute, aliases);
			}
		}

		return new ParameterModel(
			Name: member.Name,
			TypeName: type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
			IsField: member is IFieldSymbol,
			DeclaringTypeName: member.ContainingType.ToDisplayString(),
			Aliases: new EquatableArray<string>(aliases),
			Sets: new EquatableArray<ParameterSetEntry>(sets),
			Location: LocationInfo.From(GetSourceLocation(member)));
	}

	/// <summary>
	/// Reads a <c>[Parameter]</c> attribute into a <see cref="ParameterSetEntry"/>.
	/// </summary>
	/// <param name="attribute">The attribute to read.</param>
	/// <param name="cancellationToken">The token that cancels the read.</param>
	/// <returns>The parameter's entry for the set that the attribute names.</returns>
	private static ParameterSetEntry ReadSetEntry(AttributeData attribute, CancellationToken cancellationToken)
	{
		string? setName = null;
		int? position = null;
		bool mandatory = false;
		bool valueFromPipeline = false;
		bool valueFromPipelineByPropertyName = false;
		bool valueFromRemainingArguments = false;
		foreach (KeyValuePair<string, TypedConstant> argument in attribute.NamedArguments)
		{
			object? value = argument.Value.Value;
			switch (argument.Key)
			{
				case "ParameterSetName":
					setName = value as string;
					break;

				case "Position":
					position = value is int number && number != int.MinValue ? number : null;
					break;

				case "Mandatory":
					mandatory = value is true;
					break;

				case "ValueFromPipeline":
					valueFromPipeline = value is true;
					break;

				case "ValueFromPipelineByPropertyName":
					valueFromPipelineByPropertyName = value is true;
					break;

				case "ValueFromRemainingArguments":
					valueFromRemainingArguments = value is true;
					break;
			}
		}

		// ParameterAttribute stores a null or empty set name as __AllParameterSets, and the engine compares that name
		// ignoring case.
		if (string.IsNullOrEmpty(setName) || string.Equals(setName, ALL_PARAMETER_SETS, StringComparison.OrdinalIgnoreCase))
		{
			setName = null;
		}

		AttributeSyntax? syntax = attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken) as AttributeSyntax;
		return new ParameterSetEntry(
			SetName: setName,
			Position: position,
			Mandatory: mandatory,
			ValueFromPipeline: valueFromPipeline,
			ValueFromPipelineByPropertyName: valueFromPipelineByPropertyName,
			ValueFromRemainingArguments: valueFromRemainingArguments,
			Location: LocationInfo.From(syntax),
			PositionLocation: LocationInfo.From(FindNamedArgument(syntax, "Position")));
	}

	/// <summary>
	/// Adds the names that an <c>[Alias]</c> attribute gives to the specified list.
	/// </summary>
	/// <param name="attribute">The attribute to read.</param>
	/// <param name="aliases">The list to add the names to, in the order the attribute writes them.</param>
	private static void ReadAliases(AttributeData attribute, List<string> aliases)
	{
		foreach (TypedConstant argument in attribute.ConstructorArguments)
		{
			if (argument.Kind != TypedConstantKind.Array)
			{
				if (argument.Value is string alias)
				{
					aliases.Add(alias);
				}

				continue;
			}

			if (argument.IsNull)
			{
				continue;
			}

			foreach (TypedConstant element in argument.Values)
			{
				if (element.Value is string alias)
				{
					aliases.Add(alias);
				}
			}
		}
	}

	/// <summary>
	/// Finds the named argument with the specified name in an attribute's syntax.
	/// </summary>
	/// <param name="syntax">The attribute's syntax, or <see langword="null"/> when it isn't in source.</param>
	/// <param name="name">The argument's name.</param>
	/// <returns>The argument, or <see langword="null"/> when the attribute doesn't set it.</returns>
	private static AttributeArgumentSyntax? FindNamedArgument(AttributeSyntax? syntax, string name)
	{
		if (syntax?.ArgumentList is not AttributeArgumentListSyntax argumentList)
		{
			return null;
		}

		foreach (AttributeArgumentSyntax argument in argumentList.Arguments)
		{
			if (argument.NameEquals is not null && argument.NameEquals.Name.Identifier.ValueText == name)
			{
				return argument;
			}
		}

		return null;
	}

	/// <summary>
	/// Finds the first attribute of the specified class that a symbol declares itself.
	/// </summary>
	/// <param name="symbol">The symbol whose attributes to search.</param>
	/// <param name="attributeClass">The attribute class to find.</param>
	/// <returns>The attribute, or <see langword="null"/> when <paramref name="symbol"/> doesn't carry one.</returns>
	private static AttributeData? FindAttribute(ISymbol symbol, INamedTypeSymbol attributeClass)
	{
		foreach (AttributeData attribute in symbol.GetAttributes())
		{
			if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeClass))
			{
				return attribute;
			}
		}

		return null;
	}

	/// <summary>
	/// Gets the string that an attribute passes as the specified constructor argument.
	/// </summary>
	/// <param name="attribute">The attribute to read.</param>
	/// <param name="index">The zero-based index of the constructor argument.</param>
	/// <returns>The argument's string, or an empty string when the attribute passes no string there.</returns>
	private static string GetConstructorString(AttributeData attribute, int index)
	{
		return index < attribute.ConstructorArguments.Length && attribute.ConstructorArguments[index].Value is string value
			? value
			: string.Empty;
	}

	/// <summary>
	/// Determines whether the engine's own assembly declares the specified class.
	/// </summary>
	/// <param name="type">The class to check.</param>
	/// <param name="engine">The engine types of the compilation that references the engine.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="type"/> is in the same assembly as <c>Cmdlet</c>; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	private static bool IsEngineType(INamedTypeSymbol type, EngineSymbols engine)
	{
		return SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, engine.Cmdlet.ContainingAssembly);
	}

	/// <summary>
	/// Gets the first source location of the specified symbol.
	/// </summary>
	/// <param name="symbol">The symbol whose location to get.</param>
	/// <returns>The symbol's first location in source, or <see langword="null"/> when it has none.</returns>
	private static Location? GetSourceLocation(ISymbol symbol)
	{
		foreach (Location location in symbol.Locations)
		{
			if (location.IsInSource)
			{
				return location;
			}
		}

		return null;
	}
}
