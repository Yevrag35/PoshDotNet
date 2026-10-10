using System;
using System.Management.Automation;
using System.Threading.Tasks;

namespace CmdletRepro2
{
    [Cmdlet("Test", "PerRecord2")]
    public class TestPerRecord2 : PSCmdlet
    {
        [Parameter(ValueFromPipelineByPropertyName = true)] public string Name { get; set; }
        [Parameter(ValueFromPipelineByPropertyName = true)] public string Extra { get; set; }
        protected override void ProcessRecord()
        {
            WriteObject("Name=" + Name + " Keys=" + string.Join(",", MyInvocation.BoundParameters.Keys) + " Extra=" + (Extra ?? "<null>"));
        }
    }

    [Cmdlet("Test", "AsyncVoid")]
    public class TestAsyncVoid : PSCmdlet
    {
        protected override async void ProcessRecord()
        {
            await Task.Delay(50);
            WriteObject("x");
        }
    }
}
