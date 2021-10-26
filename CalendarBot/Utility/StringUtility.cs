using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalendarBot
{
    internal static class StringUtility
    {
        public static void CreateCalendarPage(int year, int month)
        {
            DateTime firstDay = new(year, month, 1);

            StringBuilder strBuilder = new();

            for(var i = 1; i <= DateTime.DaysInMonth(year, month); i++) {

            }
        }
    }
}
