using Reloaded.Mod.Interfaces;
using System.Drawing;

namespace BGME.Framework;

public static class Log
{
    public static ILogger? Logger { get; set; }

    public static LogLevel LoggerLevel { get; set; } = LogLevel.Information;

    public static void Debug(string message)
    {
        if (LoggerLevel < LogLevel.Information)
        {
            LogMessage(LogLevel.Debug, message);
        }
    }

    public static void Information(string message)
    {
        LogMessage(LogLevel.Information, message);
    }

    public static void Warning(string message)
    {
        LogMessage(LogLevel.Warning, message);
    }
    public static void Error(Exception ex, string message)
    {
        LogMessage(LogLevel.Error, $"{message}\n{ex.StackTrace}");
    }

    public static void Error(string message)
    {
        LogMessage(LogLevel.Error, message);
    }

    public static void Verbose(string message)
    {
        if (LoggerLevel < LogLevel.Debug)
        {
            LogMessage(LogLevel.Verbose, message);
        }
    }

    private static void LogMessage(LogLevel level, string message)
    {
        var color =
            level == LogLevel.Debug ? Color.LightGreen :
            level == LogLevel.Information ? Color.LightBlue :
            level == LogLevel.Error ? Color.Red :
            level == LogLevel.Warning ? Color.LightGoldenrodYellow :
            Color.White;

        Logger?.WriteLine(FormatMessage(level, message), color);
    }

    private static string FormatMessage(LogLevel level, string message)
    {
        var levelStr =
            level == LogLevel.Verbose ? "[VERBOSE]" :
            level == LogLevel.Debug ? "[DEBUG]" :
            level == LogLevel.Information ? "[INFO]" :
            level == LogLevel.Warning ? "[WARN]" :
            level == LogLevel.Error ? "[ERROR]" : string.Empty;

        return $"[BGME Framework] {levelStr} {message}";
    }
}

public enum LogLevel
{
    Verbose,
    Debug,
    Information,
    Warning,
    Error,
}