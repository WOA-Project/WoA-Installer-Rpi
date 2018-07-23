using Serilog.Events;

namespace Installer.ViewModels.Core
{
    public class RenderedLogEvent
    {
        public string Message { get; set; }
        public LogEventLevel Level { get; set; }
    }
}