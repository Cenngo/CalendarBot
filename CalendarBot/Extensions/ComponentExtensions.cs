using Discord;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalendarBot
{
    internal static class ComponentExtensions
    {
        public const string DismissId = "dismiss";
        public const string ConfirmId = "confirm";
        public const string CancelId = "cancel";

        public static ComponentBuilder WithDismissButton(this ComponentBuilder builder, bool disabled = false, int row = 0) =>
            builder.WithButton("Dismiss", DismissId, ButtonStyle.Secondary, Emoji.Parse(":heavy_multiplication_x:"), disabled: disabled, row: row);
    }
}
