using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.CodeAnalysis;
using PoshDotNet.Cmdlets.Analyzers.Model;

namespace PoshDotNet.Cmdlets.Analyzers.Rules;

/// <summary>
/// Provides <c>PDN1005</c>, which reports two parameters with the same position in one parameter set.
/// </summary>
/// <remarks>
/// <para>
/// While two unbound parameters share a position in a set that a call could still bind, PowerShell throws a
/// <c>ParameterBindingException</c> for any positional argument: "Cannot bind positional parameters because no names
/// were given." That holds in Windows PowerShell 5.1.26100 and PowerShell 7.6.6, even when the default set could take the
/// argument.
/// </para>
/// <para>
/// Positions clash between two parameters in the same named set, compared case-sensitively, or between two parameters in
/// every set. A parameter in every set doesn't clash with one in a named set: PowerShell binds the argument to both. A
/// <c>ValueFromRemainingArguments</c> parameter doesn't clash, because PowerShell skips it when it binds positions.
/// </para>
/// </remarks>
internal static class DuplicatePositionRule
{
	/// <summary>
	/// Reports each parameter whose position another parameter already has in the same set.
	/// </summary>
	/// <remarks>
	/// Parameters are compared in the model's order, so the first parameter at a position is the one that has it, and
	/// each later one gets a diagnostic. The diagnostic goes on the later parameter's <c>Position</c> argument when the
	/// cmdlet class declares that parameter, and on the class's name when it inherits it.
	/// </remarks>
	/// <param name="cmdlet">The cmdlet to check.</param>
	/// <param name="compilation">The compilation that holds the cmdlet, which locations are rebuilt in.</param>
	/// <param name="report">The callback that reports a diagnostic.</param>
	public static void Analyze(CmdletModel cmdlet, Compilation compilation, Action<Diagnostic> report)
	{
		// The parameters at each position of each set, in the model's order. A null set name stands for every set.
		Dictionary<(string? SetName, int Position), List<Holder>> slots = [];
		foreach (ParameterModel parameter in cmdlet.Parameters)
		{
			foreach (ParameterSetEntry entry in parameter.Sets)
			{
				if (entry.Position is not int position || entry.ValueFromRemainingArguments)
				{
					continue;
				}

				(string? SetName, int Position) key = (entry.SetName, position);
				if (!slots.TryGetValue(key, out List<Holder>? holders))
				{
					holders = [];
					slots.Add(key, holders);
				}

				if (!Contains(holders, parameter))
				{
					holders.Add(new Holder(parameter, entry));
				}
			}
		}

		foreach (KeyValuePair<(string? SetName, int Position), List<Holder>> slot in slots)
		{
			List<Holder> holders = slot.Value;
			for (int i = 1; i < holders.Count; i++)
			{
				report(CreateDiagnostic(cmdlet, holders[0], holders[i], slot.Key.SetName, slot.Key.Position, compilation));
			}
		}
	}

	/// <summary>
	/// Determines whether the specified parameter already holds a position.
	/// </summary>
	/// <param name="holders">The parameters that hold the position.</param>
	/// <param name="parameter">The parameter to look for.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="holders"/> holds <paramref name="parameter"/>; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	private static bool Contains(List<Holder> holders, ParameterModel parameter)
	{
		foreach (Holder holder in holders)
		{
			if (ReferenceEquals(holder.Parameter, parameter))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Creates the diagnostic for a parameter that repeats another parameter's position.
	/// </summary>
	/// <param name="cmdlet">The cmdlet that declares or inherits both parameters.</param>
	/// <param name="first">The parameter that has the position first.</param>
	/// <param name="duplicate">The parameter that repeats it.</param>
	/// <param name="setName">The set's name, or <see langword="null"/> for every set.</param>
	/// <param name="position">The position.</param>
	/// <param name="compilation">The compilation that holds the cmdlet.</param>
	/// <returns>The diagnostic to report.</returns>
	private static Diagnostic CreateDiagnostic(
		CmdletModel cmdlet,
		Holder first,
		Holder duplicate,
		string? setName,
		int position,
		Compilation compilation)
	{
		LocationInfo? firstLocation = first.GetLocation();
		LocationInfo? duplicateLocation = duplicate.GetLocation();

		// A clash between two inherited parameters is a fault of each class that inherits both, so it goes on the
		// class's name, and the parameters become additional locations.
		bool declaredHere = string.Equals(duplicate.Parameter.DeclaringTypeName, cmdlet.TypeName, StringComparison.Ordinal);
		LocationInfo? primary = declaredHere ? duplicateLocation ?? cmdlet.Location : cmdlet.Location;

		List<Location> additionalLocations = [];
		if (firstLocation is not null)
		{
			additionalLocations.Add(firstLocation.ToLocation(compilation));
		}

		if (!declaredHere && duplicateLocation is not null)
		{
			additionalLocations.Add(duplicateLocation.ToLocation(compilation));
		}

		string set = setName is null ? "every parameter set" : $"parameter set '{setName}'";
		return Diagnostic.Create(
			Descriptors.DuplicatePosition,
			primary?.ToLocation(compilation) ?? Location.None,
			additionalLocations,
			duplicate.Parameter.Name,
			position.ToString(CultureInfo.InvariantCulture),
			first.Parameter.Name,
			set);
	}

	/// <summary>
	/// Represents a parameter that holds a position in a set, with the attribute that puts it there.
	/// </summary>
	/// <param name="Parameter">The parameter.</param>
	/// <param name="Entry">The parameter's entry for the set.</param>
	private readonly record struct Holder(ParameterModel Parameter, ParameterSetEntry Entry)
	{
		/// <summary>
		/// Gets the most precise source position of the entry: its <c>Position</c> argument, its attribute, or the member.
		/// </summary>
		/// <returns>The position, or <see langword="null"/> when the parameter isn't in source.</returns>
		public LocationInfo? GetLocation()
		{
			return this.Entry.PositionLocation ?? this.Entry.Location ?? this.Parameter.Location;
		}
	}
}
