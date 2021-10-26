using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace CalendarBot.Handlers
{
    internal class CalendarHandler
    {
        private readonly ILiteCollection<CalendarEvent> _events;
        private readonly Timer _timer = new(TimeSpan.FromMinutes(5).TotalMilliseconds);

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
            _events.Find(x => x.DateAndTime.Day == DateTime.Now.Day);
            _events.
        }
    }
}
