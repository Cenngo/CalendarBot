using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalendarBot
{
    internal sealed class CalendarEvent
    {
        [BsonId(true)]
        public ulong Id { get; init; }

        public IUser User { get; init; }
        public IChannel Channel { get; init; }
        public IGuild Guild { get; init; }
        public DateTime DateAndTime { get; init; }
        public string Name { get; init; }
        public string Description {  get; init; }
        public IReadOnlyList<IUser> TargetUsers { get; init; }
        public IReadOnlyList<IRole> TargetRoles { get; init; }
        public bool Recurring => RecursionInterval == RecursionInterval.None;
        public RecursionInterval RecursionInterval { get; init; }
        public Color Color { get; init; }
    }
}
