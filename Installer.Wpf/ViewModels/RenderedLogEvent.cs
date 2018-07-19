using Serilog.Events;

namespace Intaller.Wpf.ViewModels
{
    public class RenderedLogEvent
    {
        public string Message { get; set; }
        public LogEventLevel Level { get; set; }
    }
}