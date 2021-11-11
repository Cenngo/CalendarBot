using System.Collections.Generic;

namespace CalendarBot.Entities
{
    public record GuildEventCache
    {
        public ulong GuildId { get; set; }
        public IEnumerable<CalendarEvent> GuildEvents { get; set; }

        public GuildEventCache(ulong guildId, IEnumerable<CalendarEvent> events)
        {
            GuildId = guildId;
            GuildEvents = events;
        }
    }
}
