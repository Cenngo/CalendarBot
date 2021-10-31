using LiteDB;
using Microsoft.Extensions.Configuration;
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
        private readonly ILiteCollection<CalendarEvent> _events;
        private readonly IConfiguration _config;
        private readonly double _pollingInterval;
        private readonly Timer _timer;
        private readonly TimeSpan _lookAheadSpan;
        private readonly DiscordSocketClient _discord;

        public event Func<CalendarEvent, Task> CalendarEventTriggered;

        public CalendarHandler(ILiteCollection<CalendarEvent> events, IConfiguration configuration, DiscordSocketClient discord)
        {
            _events = events;
            _config = configuration;
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

            var todaysEvents = _events.Find(x => x.DateAndTime.Day == DateTime.Now.Day);
            var upcoming = todaysEvents.Where(x => x.DateAndTime <= now - _lookAheadSpan || x.DateAndTime >= now - _lookAheadSpan);

            foreach (var ev in upcoming) {
                CalendarEventTriggered?.Invoke(ev);
                _ = OnCalendarEvent(ev);
            }
        }

        private async Task OnCalendarEvent(CalendarEvent calendarEvent) 
        {
            try {
                var embed = new EmbedBuilder {
                    Title = "Scheduled Event: " + calendarEvent.Name,
                    Description = calendarEvent.Description,
                    Color = calendarEvent.Color,
                    Timestamp = calendarEvent.DateAndTime,
                }
                .Build();

                var mentionBuilder = new StringBuilder();
                var guild = _discord.GetGuild(calendarEvent.GuildId);
                var roles = calendarEvent.TargetRoles.Select(x => guild.GetRole(x));
                var users = calendarEvent.TargetUsers.Select(x => guild.GetUser(x));


                foreach (var role in roles)
                    mentionBuilder.Append(role.Mention);

                foreach (var user in users)
                    mentionBuilder.Append(user.Mention);

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
            }
        }
    }
}
