namespace PoshDotNet.Cmdlets.Analyzers.Tests;

/// <summary>
/// Names the PowerShell engines whose reference assemblies a test can compile cmdlet code against.
/// </summary>
/// <remarks>
/// A rule that depends on the engine judges each compilation by its core library, so each engine also stands for the
/// runtimes that a build can load into.
/// </remarks>
public enum PowerShellEngine
{
	/// <summary>Windows PowerShell 5.1 on .NET Framework 4.8, whose builds load only into Windows PowerShell 5.1.</summary>
	WindowsPowerShell,

	/// <summary>PowerShellStandard.Library on .NET Standard 2.0, whose builds load into both editions.</summary>
	PowerShellStandard,

	/// <summary>PowerShell 7 on .NET 10, whose builds load only into PowerShell 7.</summary>
	PowerShell7,
}
