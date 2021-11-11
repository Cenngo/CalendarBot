using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalendarBot
{
    public static class EmbedUtility
    {
        public readonly static Color Primary = Color.Blue;
        public readonly static Color Secondary = Color.LightGrey;

        public static Embed FromPrimary(string title, string description, Action<EmbedBuilder> configure = null)
        {
            var builder = new EmbedBuilder {
                Title = title,
                Description = description,
                Color = Primary
            };

            if (configure is not null)
                configure(builder);

            return builder.Build();
        }

        public static Embed FromSecondary(string title, string description, Action<EmbedBuilder> configure = null)
        {
            var builder = new EmbedBuilder {
                Title = title,
                Description = description,
                Color = Secondary
            };

            if (configure is not null)
                configure(builder);

            return builder.Build();
        }

        public static Embed FromEvent(CalendarEvent ev, BaseSocketClient discord, CultureInfo cultureInfo)
        {
            var guild = discord.GetGuild(ev.GuildId);

            return new EmbedBuilder {
                Title = "Scheduled Event: " + ev.Name,
                Description = ev.Description,
                Color = Primary
            }.AddEmptyField()
            .AddField("Recursion Interval", ev.RecursionInterval, false)
            .AddField("Date", ev.DateAndTime.ToString(cultureInfo.DateTimeFormat.ShortDatePattern), true)
            .AddField("Time", ev.DateAndTime.ToString(cultureInfo.DateTimeFormat.ShortTimePattern), true)
            .AddField("Created At", ev.CreatedAt.ToString(cultureInfo.DateTimeFormat.ShortDatePattern), true)
            .AddEmptyField()
            .AddField("Attendee Roles", ev.TargetRoles is not null ? string.Join('\n', ev.TargetRoles?.Select(x => guild.GetRole(x).Mention)) : " - ", true)
            .AddField("Attendee Users", ev.TargetUsers is not null ? string.Join('\n', ev.TargetUsers?.Select(x => $"<@{x}>")) : " - ", true)
            .WithAuthor(discord.GetUser(ev.UserId))
            .Build();
        }

        public static Embed FromSuccess(string title, string description, bool addToTitle = true) =>
            new EmbedBuilder {
                Title = addToTitle ? ":white_check_mark: " : string.Empty + title,
                Description = addToTitle ? string.Empty : ":white_check_mark: " + description,
                Color = Primary
            }.Build();

        public static Embed FromError(string title, string description, bool addToTitle = true) =>
            new EmbedBuilder {
                Title = addToTitle ? ":bangbang: " : string.Empty + title,
                Description = addToTitle ? string.Empty : ":bangbang: " + description,
                Color = Primary
            }.Build();

        public static Embed FromWarning(string title, string description, bool addToTitle = true) =>
            new EmbedBuilder {
                Title = addToTitle ? ":warning: " : string.Empty + title,
                Description = addToTitle ? string.Empty : ":warning: " + description,
                Color = Primary
            }.Build();
    }
}
