using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalendarBot
{
    public sealed class CalendarEvent
    {
        [BsonId]
        public Guid Id {  get; set; }

        public ulong UserId { get; set; }
        public ulong MessageChannelId { get; set; }
        public ulong GuildId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime DateAndTime { get; set; }
        public string Name { get; set; }
        public string Description {  get; set; }
        public List<ulong> TargetUsers { get; set; }
        public List<ulong> TargetRoles { get; set; }
        public bool Recurring => RecursionInterval == RecursionInterval.None;
        public RecursionInterval RecursionInterval { get; set; }
        public Color Color { get; set; }

        [BsonCtor]
        public CalendarEvent() { }

        public CalendarEvent(string name, string description, IUser user, IGuild guild, DateTime dateAndTime, Color color, RecursionInterval recursionInterval = RecursionInterval.None)
        {
            Name = name;
            Description = description;
            CreatedAt = DateTime.Now;
            UserId = user.Id;
            GuildId = guild.Id;
            DateAndTime = dateAndTime;
            Color = color;
            RecursionInterval = recursionInterval;
        }
    }
}
