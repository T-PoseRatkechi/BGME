using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Formatting;
using Reloaded.Mod.Interfaces;
using System.Drawing;

namespace BGME.Framework;

internal class BgmeLogger : ILogEventSink
{
    private readonly ILogger log;
    private readonly ITextFormatter formatter = new MessageTemplateTextFormatter("[BGME Framework] [{Level}] {Message:lj}", null);

    public BgmeLogger(ILogger log)
    {
        this.log = log;
    }

    public void Emit(LogEvent logEvent)
    {
        var message = new StringWriter();
        formatter.Format(logEvent, message);
        var color = logEvent.Level == LogEventLevel.Error ? Color.Red
            : logEvent.Level == LogEventLevel.Debug ? Color.LightGreen : Color.White;

        this.log.WriteLine(message.ToString(), color);
    }
}
