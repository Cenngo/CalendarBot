using Discord;

namespace CalendarBot.Options
{
    public record ActivityOptions
    {
        public const string SECTION = "Logging";

        public string Name { get; init; }
        public ActivityType Type { get; init; }

    }
}
