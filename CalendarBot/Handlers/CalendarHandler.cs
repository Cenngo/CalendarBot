using LiteDB;
using Microsoft.Extensions.Configuration;
using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
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
        private readonly CultureInfo _culture;

        public event Func<CalendarEvent, Task> CalendarEventTriggered;

        public CalendarHandler(ILiteCollection<CalendarEvent> events, IConfiguration configuration, DiscordSocketClient discord, CultureInfo culture)
        {
            _events = events;
            _discord = discord;
            _pollingInterval = configuration.GetValue<double>("PollingInterval");
            _timer = new(TimeSpan.FromSeconds(_pollingInterval).TotalMilliseconds);
            _lookAheadSpan = TimeSpan.FromSeconds(_pollingInterval / 2);
            _culture = culture;
        }

        public void Initialize()
        {
            _timer.Elapsed += Timer_Elapsed;
            _timer.AutoReset = true;
            _timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var upcoming = _events.FindAll().Where(ev => (ev.DateAndTime.Date == DateTime.Today || ev.RecursAt(DateTime.Now)) && ev.DateAndTime.WithinTimeRange(_lookAheadSpan));

            foreach (var ev in upcoming) {
                CalendarEventTriggered?.Invoke(ev);
                _ = OnCalendarEvent(ev);
            }
        }

        private async Task OnCalendarEvent(CalendarEvent calendarEvent)
        {
            try {
                await CalendarUtility.SendEventNotification(calendarEvent, _discord, _culture);
            }
            finally {
                if (!calendarEvent.Recurring)
                    _events.Delete(calendarEvent.Id);
            }
        }
    }
}
