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
        public Guid Guid {  get; set; }

        public IUser User { get; init; }
        public IMessageChannel Channel { get; init; }
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
