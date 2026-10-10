// C# 5-compatible on purpose: Windows PowerShell 5.1's Add-Type uses the legacy CodeDom compiler.
using System;
using System.Management.Automation;
using System.Threading.Tasks;

namespace CmdletRepro
{
    [Cmdlet("Test", "VerboseParam")]
    public class TestVerboseParam : PSCmdlet
    {
        [Parameter] public SwitchParameter Verbose { get; set; }
        protected override void ProcessRecord() { WriteObject("ran"); }
    }

    [Cmdlet("Test", "AliasEa")]
    public class TestAliasEa : PSCmdlet
    {
        [Parameter] [Alias("ea")] public string Thing { get; set; }
        protected override void ProcessRecord() { WriteObject("ran"); }
    }

    [Cmdlet("Test", "ProgressActionParam")]
    public class TestProgressActionParam : PSCmdlet
    {
        [Parameter] public string ProgressAction { get; set; }
        protected override void ProcessRecord() { WriteObject("ran"); }
    }

    [Cmdlet("Test", "NoSupport")]
    public class TestNoSupport : PSCmdlet
    {
        protected override void ProcessRecord() { WriteObject("ShouldProcess returned " + ShouldProcess("target")); }
    }

    [Cmdlet("Remove", "ReproThing", SupportsShouldProcess = true)]
    public class RemoveReproThing : PSCmdlet
    {
        protected override void ProcessRecord() { WriteObject("deleted for real"); }
    }

    [Cmdlet("Test", "NoDefaultSet")]
    public class TestNoDefaultSet : PSCmdlet
    {
        [Parameter(ParameterSetName = "A")] public string A { get; set; }
        [Parameter(ParameterSetName = "B")] public string B { get; set; }
        protected override void ProcessRecord() { WriteObject("set=" + ParameterSetName); }
    }

    [Cmdlet("Test", "SetTypo", DefaultParameterSetName = "ByName")]
    public class TestSetTypo : PSCmdlet
    {
        [Parameter(ParameterSetName = "ByName", Mandatory = true)] public string Name { get; set; }
        [Parameter(ParameterSetName = "ByNmae")] public SwitchParameter Force { get; set; }
        protected override void ProcessRecord() { WriteObject("set=" + ParameterSetName); }
    }

    [Cmdlet("Test", "AliasBoundKey")]
    public class TestAliasBoundKey : PSCmdlet
    {
        [Parameter] [Alias("cn")] public string ComputerName { get; set; }
        protected override void ProcessRecord()
        {
            WriteObject("ContainsKey(cn)=" + MyInvocation.BoundParameters.ContainsKey("cn")
                + " ContainsKey(ComputerName)=" + MyInvocation.BoundParameters.ContainsKey("ComputerName"));
        }
    }

    [Cmdlet("Test", "WriteFromTask")]
    public class TestWriteFromTask : PSCmdlet
    {
        protected override void ProcessRecord() { Task.Run(() => WriteObject("x")).Wait(); }
    }

    [Cmdlet("Test", "WriteAfterAwait")]
    public class TestWriteAfterAwait : PSCmdlet
    {
        private async Task WorkAsync()
        {
            await Task.Delay(10);
            WriteObject("x");
        }
        protected override void ProcessRecord() { WorkAsync().GetAwaiter().GetResult(); }
    }

    [Cmdlet("Test", "DupPosition")]
    public class TestDupPosition : PSCmdlet
    {
        [Parameter(Position = 0)] public string First { get; set; }
        [Parameter(Position = 0)] public string Second { get; set; }
        protected override void ProcessRecord() { WriteObject("First=" + First + " Second=" + Second); }
    }

    [Cmdlet("Test", "RangeOnString")]
    public class TestRangeOnString : PSCmdlet
    {
        [Parameter] [ValidateRange(1, 10)] public string S { get; set; }
        protected override void ProcessRecord() { WriteObject("S=" + S); }
    }

    [Cmdlet("Test", "GetOnlyParam")]
    public class TestGetOnlyParam : PSCmdlet
    {
        [Parameter] public string Ro { get { return "fixed"; } }
        protected override void ProcessRecord() { WriteObject("Ro=" + Ro); }
    }

    [Cmdlet("Test", "PerRecord")]
    public class TestPerRecord : PSCmdlet
    {
        [Parameter(ValueFromPipelineByPropertyName = true)] public string Name { get; set; }
        [Parameter(ValueFromPipelineByPropertyName = true)] public string Extra { get; set; }
        protected override void ProcessRecord()
        {
            WriteObject("Keys=" + string.Join(",", MyInvocation.BoundParameters.Keys) + " Extra=" + (Extra ?? "<null>"));
        }
    }

    [Cmdlet("Test", "DefaultMissing", DefaultParameterSetName = "Bogus")]
    public class TestDefaultMissing : PSCmdlet
    {
        [Parameter(ParameterSetName = "A")] public string A { get; set; }
        [Parameter(ParameterSetName = "B")] public string B { get; set; }
        protected override void ProcessRecord() { WriteObject("set=" + ParameterSetName); }
    }

    [Cmdlet("Fetch", "ReproThing")]
    public class FetchReproThing : PSCmdlet
    {
        protected override void ProcessRecord() { WriteObject("fetched"); }
    }
}
