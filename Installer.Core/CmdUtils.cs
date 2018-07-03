using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Installer.Core
{
    public static class CmdUtils
    {
        public static string Run(string command, string arguments)
        {

            var process = new Process
            {
                StartInfo =
                {
                    FileName = command,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                }
            };

            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            Console.WriteLine(output);
            string err = process.StandardError.ReadToEnd();
            Console.WriteLine(err);
            process.WaitForExit();

            return output;
        }


        public static ObservableProcess RunProcessAsync(string fileName, string args)
        {
            using (var process = new Process
            {
                StartInfo =
                {
                    FileName = fileName, Arguments = args,
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardError = true
                },
                EnableRaisingEvents = true
            })
            {
                return RunProcessAsync(process);
            }
        }

        private static ObservableProcess RunProcessAsync(Process process)
        {
            var tcs = new TaskCompletionSource<int>();

            var stdObs = Observable.FromEventPattern<DataReceivedEventHandler, DataReceivedEventArgs>(x => process.OutputDataReceived += x, x => process.OutputDataReceived -= x);
            var errObs = Observable.FromEventPattern<DataReceivedEventHandler, DataReceivedEventArgs>(x => process.ErrorDataReceived += x, x => process.ErrorDataReceived -= x);
            var exitObs = Observable.FromEventPattern<EventHandler, EventArgs>(x => process.Exited += x, x => process.Exited -= x);


            bool started = process.Start();
            if (!started)
            {
                //you may allow for the process to be re-used (started = false) 
                //but I'm not sure about the guarantees of the Exited event in such a case
                throw new InvalidOperationException("Could not start process: " + process);
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return new ObservableProcess(exitObs, stdObs, errObs);
        }
    }

    public class ObservableProcess
    {
        public IObservable<EventPattern<EventArgs>> ExitObs { get; }
        public IObservable<EventPattern<DataReceivedEventArgs>> StdObs { get; }
        public IObservable<EventPattern<DataReceivedEventArgs>> ErrObs { get; }

        public ObservableProcess(IObservable<EventPattern<EventArgs>> exitObs, IObservable<EventPattern<DataReceivedEventArgs>> stdObs, IObservable<EventPattern<DataReceivedEventArgs>> errObs)
        {
            ExitObs = exitObs;
            StdObs = stdObs;
            ErrObs = errObs;
        }
    }
}