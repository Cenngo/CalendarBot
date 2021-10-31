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
        public const string DismissId = "dismiss";
        public const string ConfirmId = "confirm";
        public const string CancelId = "cancel";

        public static MessageComponent CreateMonthsDropdown(string customId)
        {
            var now = DateTime.Now;
            SelectMenuBuilder builder = new();
            builder.CustomId = customId;

            for(var i = 1; i <= 12; i++) 
            {
                string monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i);

                builder.AddOption(monthName, i.ToString(), isDefault: now.Month == i);
            }

            return new ComponentBuilder().WithSelectMenu(builder).Build();
        }

        public static ComponentBuilder WithDismissButton(this ComponentBuilder builder, bool disabled = false, int row = 0) =>
            builder.WithButton("Dismiss", DismissId, ButtonStyle.Secondary, Emoji.Parse(":heavy_multiplication_x:"), disabled: disabled, row: row);
    }
}
