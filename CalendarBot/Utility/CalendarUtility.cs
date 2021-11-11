using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = System.Drawing.Color;

namespace CalendarBot
{
    internal static class CalendarUtility
    {
        private readonly static string[] Header = { "S", "M", "T", "W", "T", "F", "S" };

        public static Bitmap GenerateCalendarPage(int width, int height, int month, int year, int[] highlights = null)
        {
            var font = new Font("Arial", width / 20, FontStyle.Bold, GraphicsUnit.Pixel);
            var coloredBrush = new SolidBrush(Color.Orange);
            var whiteBrush = new SolidBrush(Color.White);
            var highlighPen = new Pen(Color.GreenYellow, width / 100);

            var bmp = new Bitmap(width, height);
            var graphics = Graphics.FromImage(bmp);

            var point = new Point(width / 100, height / 100);
            var firstDay = (int)(new DateTime(year, month, 1).DayOfWeek);

            for (var i = 0; i < 7; i++) {
                graphics.DrawString(Header[i], font, coloredBrush, point);
                point.X += width / 7;
            }

            for (var i = 0; i < Math.Ceiling((decimal)(DateTime.DaysInMonth(year, month) + firstDay) / 7); i++) {
                point.X = width / 100;
                point.Y += font.Height * 2;

                for (var j = 0; j < 7; j++) {
                    var day = i * 7 + j - firstDay + 1;
                    graphics.DrawString(day <= 0 || day > DateTime.DaysInMonth(year, month) ? " " : day.ToString(), font, whiteBrush, point);

                    if ((highlights?.Contains(day)).GetValueOrDefault())
                        graphics.DrawEllipse(highlighPen, point.X - width / 35, point.Y - width / 35, font.Height * 2, font.Height * 2);

                    point.X += width / 7;
                }
            }

            return bmp;
        }

        public static async Task SendEventNotification(CalendarEvent ev, BaseSocketClient discord, CultureInfo culture)
        {
            var guild = discord.GetGuild(ev.GuildId);

            var embed = EmbedUtility.FromEvent(ev, discord, culture);

            var mentionBuilder = new StringBuilder();

            if (ev.TargetRoles is not null)
                foreach (var role in ev.TargetRoles)
                    mentionBuilder.Append(guild.GetRole(role).Mention);

            if (ev.TargetUsers is not null)
                foreach (var user in ev.TargetUsers)
                    mentionBuilder.Append($"<@{user}>");

            var channel = discord.GetChannel(ev.MessageChannelId) as IMessageChannel;

            await channel?.SendMessageAsync(mentionBuilder.ToString(), embed: embed);
        }

        public static DateTime AddRecursionInterval(this DateTime dateTime, RecursionInterval recursionInterval) =>
            recursionInterval switch {
                RecursionInterval.Day => dateTime.AddDays(1),
                RecursionInterval.Week => dateTime.AddDays(7),
                RecursionInterval.Month => dateTime.AddMonths(1),
                RecursionInterval.Year => dateTime.AddYears(1),
            };

        public static bool WithinTimeRange(this DateTime dateTime, TimeSpan span)
        {
            var now = DateTime.Now.TimeOfDay;

            return dateTime.TimeOfDay < now + span && dateTime.TimeOfDay >= now - span;
        }

        public static bool RecursAt(this CalendarEvent ev, DateTime dateTime)
        {
            switch (ev.RecursionInterval) {
                case RecursionInterval.Day:
                    return true;
                case RecursionInterval.Week:
                    return ev.DateAndTime.DayOfWeek == dateTime.DayOfWeek;
                case RecursionInterval.Month:
                    return ev.DateAndTime.Day == dateTime.Day;
                case RecursionInterval.Year:
                    return ev.DateAndTime.Day == dateTime.Day && ev.DateAndTime.Month == dateTime.Month;
                default:
                    return false;
            }
        }
    }
}
