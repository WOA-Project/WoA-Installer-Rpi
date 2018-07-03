using System;
using System.Globalization;
using System.IO;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Serilog;

namespace Installer.Core.FullFx
{
    public class DismImageService : IWindowsImageService
    {
        private readonly Regex percentRegex = new Regex(@"(\d*.\d*)%");

        public async Task ApplyImage(Volume volume, string imagePath, int imageIndex = 1, IObserver<double> progressObserver = null)
        {
            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException($"Image not found: {imagePath}. Please, verify that the file exists and it's accessible.");
            }

            var procObs = CmdUtils.RunProcessAsync("DISM", $@"/Apply-Image /ImageFile:""{imagePath}"" /Index:{imageIndex} /ApplyDir:{volume.RootDir.Name}");

            IDisposable stdOutputSubscription = null;
            if (progressObserver != null)
            {
                stdOutputSubscription = procObs.StdObs
                    .Select(x => GetPercentage(x.EventArgs.Data))
                    .Where(d => !double.IsNaN(d))
                    .Subscribe(progressObserver.OnNext);
            }

            await procObs.ExitObs;

            stdOutputSubscription?.Dispose();

            CmdUtils.Run("DISM", $@"/Apply-Image /ImageFile:""{imagePath}"" /Index:1 /ApplyDir:{volume.RootDir.Name}");
        }

        private double GetPercentage(string dismOutput)
        {
            if (dismOutput == null)
            {
                return double.NaN;
            }

            var matches = percentRegex.Match(dismOutput);

            if (matches.Success)
            {
                var value = matches.Groups[1].Value;
                try
                {
                    var percentage = double.Parse(value, CultureInfo.InvariantCulture);
                    return percentage;
                }
                catch (FormatException)
                {
                    Log.Warning($"Cannot convert {value} to double");                    
                }
            }

            return double.NaN;
        }

        public Task InjectDrivers(string path, Volume volume)
        {
            CmdUtils.Run("DISM", $@"/Add-Driver /Image:{volume.RootDir.Name} /Driver:""{path}"" /Recurse /ForceUnsigned");
            return Task.CompletedTask;        }
    }
}