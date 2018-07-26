using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Installer.Core.Exceptions;
using Installer.Core.FileSystem;
using Installer.Core.Services;
using Installer.Core.Services.Wim;
using Installer.Core.Utils;
using Serilog;

namespace Installer.Core.FullFx
{
    public class DismImageService : IWindowsImageService
    {
        private readonly Regex percentRegex = new Regex(@"(\d*.\d*)%");

        public async Task ApplyImage(Volume volume, string imagePath, int imageIndex = 1, IObserver<double> progressObserver = null)
        {
            var applyDir = volume?.RootDir.Name;
            EnsureValidParameters(applyDir, imagePath, imageIndex);

            ISubject<string> outputSubject = new Subject<string>();
            IDisposable stdOutputSubscription = null;
            if (progressObserver != null)
            {
                stdOutputSubscription = outputSubject
                    .Select(GetPercentage)
                    .Where(d => !double.IsNaN(d))
                    .Subscribe(progressObserver);
            }
            
            var dismName = "DISM";
            var args = $@"/Apply-Image /ImageFile:""{imagePath}"" /Index:{imageIndex} /ApplyDir:{applyDir}";
            Log.Verbose("We are about to run DISM: {ExecName} {Parameters}", dismName, args);
            var resultCode = await ProcessUtils.RunProcessAsync(dismName, args, outputObserver: outputSubject);
            if (resultCode != 0)
            {
                throw new DeploymentException($"There has been a problem during deployment: DISM exited with code {resultCode}.");
            }

            stdOutputSubscription?.Dispose();
        }

        private static void EnsureValidParameters(string applyDir, string imagePath, int imageIndex)
        {
            if (imagePath == null)
            {
                throw new ArgumentNullException(nameof(imagePath));
            }

            if (applyDir == null)
            {
                throw new ArgumentException("The volume to apply the image is invalid");
            }

            EnsureValidImage(imagePath, imageIndex);
        }

        private static void EnsureValidImage(string imagePath, int imageIndex)
        {
            Log.Verbose("Checking image at {Path}, with index {Index}", imagePath, imagePath);

            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException($"Image not found: {imagePath}. Please, verify that the file exists and it's accessible.");
            }

            Log.Verbose("Image file at '{ImagePath}' exists", imagePath);

            using (var stream = File.OpenRead(imagePath))
            {
                var metadata = new WindowsImageMetadataReader().Load(stream);
                var imageMetadata = metadata.Images.Single(x => x.Index == imageIndex);
                if (imageMetadata.Architecture != Architecture.Arm64)
                {
                    throw new InvalidImageException("The selected image isn't for the ARM64 architecture.");
                }
            }            
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
                    var percentage = double.Parse(value, CultureInfo.InvariantCulture) / 100D;
                    return percentage;
                }
                catch (FormatException)
                {
                    Log.Warning($"Cannot convert {value} to double");
                }
            }

            return double.NaN;
        }

        public async Task InjectDrivers(string path, Volume volume)
        {
            var outputSubject = new Subject<string>();
            var subscription = outputSubject.Subscribe(Log.Verbose);
            var resultCode = await ProcessUtils.RunProcessAsync("DISM", $@"/Add-Driver /Image:{volume.RootDir.Name} /Driver:""{path}"" /Recurse /ForceUnsigned", outputSubject, outputSubject);
            subscription.Dispose();
            
            if (resultCode != 0)
            {
                throw new DeploymentException(
                    $"There has been a problem during deployment: DISM exited with code {resultCode}.");
            }
        }
    }
}