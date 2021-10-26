using Discord;
using Microsoft.Extensions.Logging;
using System;

namespace CalendarBot
{
    public static class LoggingExtensions
    {
        public static LogLevel ToMicrosoft ( this LogSeverity logSeverity ) =>
                (LogLevel)Enum.ToObject(typeof(LogLevel), 5 - logSeverity);
        public static LogSeverity ToDiscord ( this LogLevel logLevel ) =>
                (LogSeverity)Enum.ToObject(typeof(LogSeverity), 5 - logLevel);
    }
}
