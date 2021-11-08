using LiteDB;
using Microsoft.Extensions.Configuration;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace CalendarBot
{
    internal class CalendarHandler
    {
        private readonly ILiteCollection<CalendarEvent> _events;
        private readonly double _pollingInterval;
        private readonly Timer _timer;
        private readonly TimeSpan _lookAheadSpan;
        private readonly DiscordSocketClient _discord;

        public event Func<CalendarEvent, Task> CalendarEventTriggered;

        public CalendarHandler(ILiteCollection<CalendarEvent> events, IConfiguration configuration, DiscordSocketClient discord)
        {
            _events = events;
            _discord = discord;
            _pollingInterval = configuration.GetValue<double>("PollingInterval");
            _timer = new(TimeSpan.FromSeconds(_pollingInterval).TotalMilliseconds);
            _lookAheadSpan = TimeSpan.FromSeconds(_pollingInterval / 2);
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

            var upcoming = _events.Find(x => x.DateAndTime < now + _lookAheadSpan && x.DateAndTime >= now - _lookAheadSpan);

            foreach (var ev in upcoming) {
                CalendarEventTriggered?.Invoke(ev);
                _ = OnCalendarEvent(ev);
            }
        }

        private async Task OnCalendarEvent(CalendarEvent calendarEvent)
        {
            try {
                var embed = EmbedUtility.FromEvent(calendarEvent, _discord);

                var mentionBuilder = new StringBuilder();

                if (calendarEvent.TargetRoles is not null)
                    foreach (var role in calendarEvent.TargetRoles)
                        mentionBuilder.Append($"<@{role}>");

                if (calendarEvent.TargetUsers is not null)
                    foreach (var user in calendarEvent.TargetUsers)
                        mentionBuilder.Append($"<@{user}>");

                var channel = _discord.GetChannel(calendarEvent.MessageChannelId) as IMessageChannel;

                await channel?.SendMessageAsync(mentionBuilder.ToString(), embed: embed);
            }
            finally {
                switch (calendarEvent.RecursionInterval) {
                    case RecursionInterval.None:
                        break;
                    case RecursionInterval.Day:
                        calendarEvent.DateAndTime.AddDays(1);
                        _events.Insert(calendarEvent);
                        break;
                    case RecursionInterval.Week:
                        calendarEvent.DateAndTime.AddDays(7);
                        _events.Insert(calendarEvent);
                        break;
                    case RecursionInterval.Month:
                        calendarEvent.DateAndTime.AddMonths(1);
                        _events.Insert(calendarEvent);
                        break;
                    case RecursionInterval.Year:
                        calendarEvent.DateAndTime.AddYears(1);
                        _events.Insert(calendarEvent);
                        break;
                }

                _events.Delete(calendarEvent.Id);
            }
        }
    }
}
