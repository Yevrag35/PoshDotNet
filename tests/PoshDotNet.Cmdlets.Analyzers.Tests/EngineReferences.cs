using System;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis.Testing;

namespace PoshDotNet.Cmdlets.Analyzers.Tests;

/// <summary>
/// Provides the reference assemblies that tests compile cmdlet code against, one set for each
/// <see cref="PowerShellEngine"/>.
/// </summary>
/// <remarks>
/// <para>
/// Each set pairs a framework's reference assemblies with an engine's <c>System.Management.Automation</c> package.
/// Microsoft.CodeAnalysis.Testing downloads the packages the first time a test resolves a set.
/// </para>
/// <para>
/// The engine packages' versions come from <c>Directory.Packages.props</c>, through assembly metadata that the test
/// project's build adds.
/// </para>
/// </remarks>
public static class EngineReferences
{
	/// <summary>
	/// Gets the reference assemblies of Windows PowerShell 5.1 on .NET Framework 4.8.
	/// </summary>
	/// <value>
	/// The .NET Framework 4.8 reference assemblies with Microsoft.PowerShell.5.1.ReferenceAssemblies. A compilation
	/// against them has <c>mscorlib</c> as its core library.
	/// </value>
	public static ReferenceAssemblies WindowsPowerShell { get; } = ReferenceAssemblies.NetFramework.Net48.Default
		.AddPackages(Package("Microsoft.PowerShell.5.1.ReferenceAssemblies"));

	/// <summary>
	/// Gets the reference assemblies of PowerShellStandard.Library on .NET Standard 2.0.
	/// </summary>
	/// <value>
	/// The .NET Standard 2.0 reference assemblies with PowerShellStandard.Library. A compilation against them has
	/// <c>netstandard</c> as its core library, and its <c>System.Management.Automation</c> reports the same version as
	/// Windows PowerShell 5.1's.
	/// </value>
	public static ReferenceAssemblies PowerShellStandard { get; } = ReferenceAssemblies.NetStandard.NetStandard20
		.AddPackages(Package("PowerShellStandard.Library"));

	/// <summary>
	/// Gets the reference assemblies of PowerShell 7 on .NET 10.
	/// </summary>
	/// <value>
	/// The .NET 10 reference assemblies with the System.Management.Automation package. A compilation against them has
	/// <c>System.Runtime</c> as its core library.
	/// </value>
	public static ReferenceAssemblies PowerShell7 { get; } = ReferenceAssemblies.Net.Net100
		.AddPackages(Package("System.Management.Automation"));

	/// <summary>
	/// Gets the reference assemblies for the specified engine.
	/// </summary>
	/// <param name="engine">The engine whose reference assemblies a test compiles against.</param>
	/// <returns>The reference assemblies of <paramref name="engine"/>.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="engine"/> isn't a defined engine.</exception>
	public static ReferenceAssemblies For(PowerShellEngine engine)
	{
		return engine switch
		{
			PowerShellEngine.WindowsPowerShell => WindowsPowerShell,
			PowerShellEngine.PowerShellStandard => PowerShellStandard,
			PowerShellEngine.PowerShell7 => PowerShell7,
			_ => throw new ArgumentOutOfRangeException(nameof(engine), engine, "The engine isn't one that the tests define."),
		};
	}

	/// <summary>
	/// Gets the identity of the specified package, at the version that <c>Directory.Packages.props</c> sets for it.
	/// </summary>
	/// <param name="id">The package ID.</param>
	/// <returns>A one-element array that holds the package's identity.</returns>
	/// <exception cref="InvalidOperationException">Thrown when the test assembly carries no version for <paramref name="id"/>.</exception>
	private static ImmutableArray<PackageIdentity> Package(string id)
	{
		foreach (AssemblyMetadataAttribute metadata in typeof(EngineReferences).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
		{
			if (metadata.Key == id && !string.IsNullOrEmpty(metadata.Value))
			{
				return [new PackageIdentity(id, metadata.Value)];
			}
		}

		throw new InvalidOperationException($"The test assembly carries no version for {id}. Add a PackageVersion for it to Directory.Packages.props, and name it in the test project's AddEnginePackageVersions target.");
	}
}
