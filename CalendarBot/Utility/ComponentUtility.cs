using Discord;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalendarBot
{
    internal static class ComponentUtility
    {
        public static MessageComponent CreateMonthsDropdown(string customId)
        {
            var now = DateTime.Now;
            SelectMenuBuilder builder = new();
            builder.CustomId = customId;

            for(var i = 1; i <= 12; i++) 
            {
                string monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i);

                builder.AddOption(monthName, i.ToString(), @default: now.Month == i ? true : false);
            }

            return new ComponentBuilder().WithSelectMenu(builder).Build();
        }
    }
}
