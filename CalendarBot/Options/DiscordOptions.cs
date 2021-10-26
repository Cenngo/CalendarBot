namespace CalendarBot.Options
{
    public record DiscordOptions
    {
        public const string SECTION = "Discord";

        public string Token { get; init; }
        public ActivityOptions Activity { get; init; }
    }
}
