using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace CalendarBot
{
    internal class CalendarHandler
    {
        public const double INTERVAL_MINS = 5;

        private readonly ILiteCollection<CalendarEvent> _events;
        private readonly Timer _timer = new(TimeSpan.FromMinutes(INTERVAL_MINS).TotalMilliseconds);
        private readonly TimeSpan _lookAheadSpan = TimeSpan.FromMinutes(INTERVAL_MINS / 2);

        public event Func<CalendarEvent, Task> CalendarEventTriggered;

        public CalendarHandler(ILiteCollection<CalendarEvent> events)
        {
            _events = events;
        }

        public void Initialize()
        {
            _timer.Elapsed += Timer_Elapsed;
            _timer.AutoReset = true;
            _timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var now = DateTime.Now;

            var todaysEvents = _events.Find(x => x.DateAndTime.Day == DateTime.Now.Day);
            var upcoming = todaysEvents.Where(x => x.DateAndTime <= now - _lookAheadSpan || x.DateAndTime >= now - _lookAheadSpan);

            foreach (var ev in upcoming) {
                CalendarEventTriggered?.Invoke(ev);
                _ = OnCalendarEvent(ev);
            }
        }

        private async Task OnCalendarEvent(CalendarEvent calendarEvent) 
        {
            var embed = new EmbedBuilder 
            {
                Title = "Scheduled Event: " + calendarEvent.Name,
                Description = calendarEvent.Description,
                Color = calendarEvent.Color,
                Timestamp = calendarEvent.DateAndTime,
            }.WithAuthor(calendarEvent.User)
            .Build();

            var mentionBuilder = new StringBuilder();

            foreach (var role in calendarEvent.TargetRoles)
                mentionBuilder.Append(role.Mention);

            foreach(var user in calendarEvent.TargetUsers)
                mentionBuilder.Append(user.Mention);

            await calendarEvent.Channel.SendMessageAsync(mentionBuilder.ToString(), embed: embed);
        }
    }
}
