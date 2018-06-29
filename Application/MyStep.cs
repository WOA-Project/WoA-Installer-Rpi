using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;
using ReactiveUI;

namespace Install
{
    public class MyStep : ReactiveObject, IStep
    {
        public MyStep()
        {
            RunCommand = ReactiveCommand.CreateFromTask(Execute);
        }

        public string Name => "Basic task";
        public ReactiveCommand RunCommand { get; set; }

        public Task Execute()
        {
            var powerShell = PowerShell.Create();
            powerShell.AddScript("get-process | foreach { $_.Name }");
            var result = powerShell.Invoke();

            var str = result.Select(o => o.ToString());

            return Task.CompletedTask;
        }
    }
}