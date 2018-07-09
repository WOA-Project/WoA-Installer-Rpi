using System.Collections.Generic;
using System.Xml.Serialization;

namespace Installer.Core.Wim
{
    public abstract class XmlModel
    {
        [XmlRoot(ElementName = "CREATIONTIME")]
        public class Time
        {
            [XmlElement(ElementName = "HIGHPART")] public string HighPart { get; set; }

            [XmlElement(ElementName = "LOWPART")] public string LowPart { get; set; }
        }

        [XmlRoot(ElementName = "SERVICINGDATA")]
        public class ServicingData
        {
            [XmlElement(ElementName = "GDRDUREVISION")]
            public string GdrDuRevision { get; set; }

            [XmlElement(ElementName = "PKEYCONFIGVERSION")]
            public string PKeyConfigVersion { get; set; }
        }

        [XmlRoot(ElementName = "LANGUAGES")]
        public class Languages
        {
            [XmlElement(ElementName = "LANGUAGE")] public string Language { get; set; }

            [XmlElement(ElementName = "DEFAULT")] public string Default { get; set; }
        }

        [XmlRoot(ElementName = "VERSION")]
        public class Version
        {
            [XmlElement(ElementName = "MAJOR")] public string Major { get; set; }

            [XmlElement(ElementName = "MINOR")] public string Minor { get; set; }

            [XmlElement(ElementName = "BUILD")] public string Build { get; set; }

            [XmlElement(ElementName = "SPBUILD")] public string SpBuild { get; set; }

            [XmlElement(ElementName = "SPLEVEL")] public string SpLevel { get; set; }
        }

        [XmlRoot(ElementName = "WINDOWS")]
        public class Windows
        {
            [XmlElement(ElementName = "ARCH")] public string Arch { get; set; }

            [XmlElement(ElementName = "PRODUCTNAME")]
            public string ProductName { get; set; }

            [XmlElement(ElementName = "EDITIONID")]
            public string EditionId { get; set; }

            [XmlElement(ElementName = "INSTALLATIONTYPE")]
            public string InstallationType { get; set; }

            [XmlElement(ElementName = "SERVICINGDATA")]
            public ServicingData ServicingData { get; set; }

            [XmlElement(ElementName = "PRODUCTTYPE")]
            public string ProductType { get; set; }

            [XmlElement(ElementName = "PRODUCTSUITE")]
            public string ProductSuite { get; set; }

            [XmlElement(ElementName = "LANGUAGES")]
            public Languages Languages { get; set; }

            [XmlElement(ElementName = "VERSION")] public Version Version { get; set; }

            [XmlElement(ElementName = "SYSTEMROOT")]
            public string SystemRoot { get; set; }
        }

        [XmlRoot(ElementName = "IMAGE")]
        public class Image
        {
            [XmlElement(ElementName = "DIRCOUNT")] public string DirectoryCount { get; set; }

            [XmlElement(ElementName = "FILECOUNT")]
            public string FileCount { get; set; }

            [XmlElement(ElementName = "TOTALBYTES")]
            public string TotalBytes { get; set; }

            [XmlElement(ElementName = "HARDLINKBYTES")]
            public string HardLinkBytes { get; set; }

            [XmlElement(ElementName = "CREATIONTIME")]
            public Time CreationTime { get; set; }

            [XmlElement(ElementName = "LASTMODIFICATIONTIME")]
            public Time LastModificationTime { get; set; }

            [XmlElement(ElementName = "WIMBOOT")] public string WimBoot { get; set; }

            [XmlElement(ElementName = "WINDOWS")] public Windows Windows { get; set; }

            [XmlElement(ElementName = "NAME")] public string Name { get; set; }

            [XmlElement(ElementName = "DESCRIPTION")]
            public string Description { get; set; }

            [XmlElement(ElementName = "FLAGS")] public string Flags { get; set; }

            [XmlElement(ElementName = "DISPLAYNAME")]
            public string DiplayName { get; set; }

            [XmlElement(ElementName = "DISPLAYDESCRIPTION")]
            public string DisplayDescription { get; set; }

            [XmlAttribute(AttributeName = "INDEX")]
            public string Index { get; set; }
        }

        [XmlRoot(ElementName = "WIM")]
        public class WimInfo
        {
            [XmlElement(ElementName = "TOTALBYTES")]
            public string TotalBytes { get; set; }

            [XmlElement(ElementName = "IMAGE")] public List<Image> Images { get; set; }
        }
    }
}