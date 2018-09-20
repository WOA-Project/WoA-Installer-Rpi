using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Serilog;

namespace Installer.Core.Utils
{
    public static class ProcessUtils
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

            Log.Verbose("Starting process {@Process}", new { process.StartInfo.FileName, process.StartInfo.Arguments });
            process.Start();
            Log.Verbose("Process started sucessfully");

            string output = process.StandardOutput.ReadToEnd();
            Console.WriteLine(output);
            string err = process.StandardError.ReadToEnd();
            Console.WriteLine(err);
            process.WaitForExit();

            Log.Verbose("Process output {Output}", output);

            return output;
        }

        public static async Task<int> RunProcessAsync(string fileName, string args = "", IObserver<string> outputObserver = null, IObserver<string> errorObserver = null)
        {
            using (var process = new Process
            {
                StartInfo =
                {
                    FileName = fileName,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            })
            {
                return await RunProcessAsync(process, outputObserver, errorObserver).ConfigureAwait(false);
            }
        }

        private static Task<int> RunProcessAsync(Process process, IObserver<string> outputObserver, IObserver<string> errorObserver)
        {
            var tcs = new TaskCompletionSource<int>();

            process.Exited += (s, ea) => tcs.SetResult(process.ExitCode);

            if (outputObserver != null)
            {
                process.OutputDataReceived += (s, ea) => outputObserver.OnNext(ea.Data);
            }

            if (errorObserver != null)
            {
                process.ErrorDataReceived += (s, ea) => errorObserver?.OnNext(ea.Data);
            }

            Log.Verbose("Starting process {@Process}", new { process.StartInfo.FileName, process.StartInfo.Arguments });
            bool started = process.Start();
            Log.Verbose("Process started sucessfully");

            if (!started)
            {
                //you may allow for the process to be re-used (started = false) 
                //but I'm not sure about the guarantees of the Exited event in such a case
                throw new InvalidOperationException("Could not start process: " + process);
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return tcs.Task;
        }
    }
}
