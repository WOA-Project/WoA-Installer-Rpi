using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using Serilog;

namespace Installer.Core.Services.Wim
{
    public abstract class WindowsImageMetadataReaderBase
    {
        private static XmlSerializer Serializer { get; } = new XmlSerializer(typeof(WimMetadata));

        public XmlWindowsImageMetadata Load(Stream stream)
        {
            Log.Verbose("Getting WIM stream");

            var metadata = (WimMetadata)Serializer.Deserialize(GetXmlMetadataStream(stream));

            Log.Verbose("Wim metadata deserialized correctly {@Metadata}", metadata);

            return new XmlWindowsImageMetadata
            {
                Images = metadata.Images.Select(x => new DiskImageMetadata
                {
                    Architecture = WindowsImageMetadataReader.GetArchitecture(x.Windows.Arch),
                    Build = x.Windows.Version.Build,
                    DisplayName = x.Name,
                    Index = int.Parse(x.Index)
                }).ToList()
            };
        }

        private static Architecture GetArchitecture(string str)
        {
            switch (str)
            {
                case "0":
                    return Architecture.X86;
                case "12":
                    return Architecture.Arm64;
                case "9":
                    return Architecture.X64;
            }

            throw new IndexOutOfRangeException($"The architecture '{str}' is unknown");
        }

        protected abstract Stream GetXmlMetadataStream(Stream wim);
    }
}