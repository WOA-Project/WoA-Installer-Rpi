using System.Xml.Serialization;

namespace Installer.Core.Services.Wim
{
    [XmlRoot(ElementName = "CREATIONTIME")]
        public class Time
        {
            [XmlElement(ElementName = "HIGHPART")] public string HighPart { get; set; }

            [XmlElement(ElementName = "LOWPART")] public string LowPart { get; set; }
        }
}