using Microsoft.Extensions.Logging;

namespace CalendarBot.Options
{
    public record LoggingOptions
    {
        public const string SECTION = "Logging";

        public LogLevel Discord { get; init; }
        public LogLevel Commands { get; init; }
        public int ProcessingInterval { get; init; } 
    }
}
