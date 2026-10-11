namespace PoshDotNet.Cmdlets.Analyzers.Tests;

/// <summary>
/// Provides theory data that runs a test once for each <see cref="PowerShellEngine"/>.
/// </summary>
public static class EngineData
{
	/// <summary>
	/// Gets one row for each engine.
	/// </summary>
	/// <value>
	/// A row for each engine whose reference assemblies the tests compile against.
	/// </value>
	public static TheoryData<PowerShellEngine> All { get; } =
	[
		PowerShellEngine.WindowsPowerShell,
		PowerShellEngine.PowerShellStandard,
		PowerShellEngine.PowerShell7,
	];
}
