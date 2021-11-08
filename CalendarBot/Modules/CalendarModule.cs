using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CalendarBot
{
    public sealed class CalendarModule : InteractionModuleBase<InteractionContext<DiscordSocketClient>>
    {
        public ILiteCollection<CalendarEvent> Events { get; set; }
        public CultureInfo Culture {  get; set; }

        [SlashCommand("ping", "recieve a pong")]
        public async Task Ping() =>
            await RespondAsync("pong");

        [SlashCommand("latency", "Get the estimate round-trip latency to the gateway server")]
        public async Task Latency() =>
            await RespondAsync(Context.Client.Latency.ToString() + " ms", ephemeral: true);

        [SlashCommand("invite", "Get the invite link")]
        public async Task Invite()
        {
            var button = new ComponentBuilder().WithButton("Invite Me!", style: ButtonStyle.Link,
                url: $"https://discord.com/api/oauth2/authorize?client_id={Context.Client.CurrentUser.Id}&permissions=2048&redirect_uri=https%3A%2F%2Fhydrametry.com&scope=bot%20applications.commands")
                .Build();

            await RespondAsync("\u200B", component: button, ephemeral: true);
        }

        [SlashCommand("create", "create a calendar event")]
        public async Task Create(string name, string description,
            [InclusiveRange(1, 31)] int day,
            Months month,
            [InclusiveRange(0, 9999)] int year,
            [InclusiveRange(0, 23)] int hour,
            [InclusiveRange(0, 59)] int minute,
            RecursionInterval recursionInterval = RecursionInterval.None)
        {
            var guid = Guid.NewGuid();

            var newEvent = new CalendarEvent {
                Id = guid,
                Name = name,
                Description = description,
                CreatedAt = DateTime.Now,
                DateAndTime = new DateTime(year, (int)month, day, hour, minute, 0, DateTimeKind.Local),
                Color = Color.Orange,
                MessageChannelId = Context.Channel.Id,
                GuildId = Context.Guild.Id,
                RecursionInterval = recursionInterval,
                UserId = Context.User.Id
            };

            Events.Insert(newEvent);

            await PrintTargetConfigure(newEvent, "add");
        }

        [Group("print", "display event information")]
        public class Print : InteractionModuleBase<InteractionContext<DiscordSocketClient>>
        {
            public ILiteCollection<CalendarEvent> Events { get; set; }
            public CultureInfo Culture { get; set; }

            [SlashCommand("day", "Print the events of a day")]
            public async Task Day([InclusiveRange(1, 31)] int day, Months month, [InclusiveRange(1, 9999)] int year)
            {
                var targetDate = new DateTime(year, (int)month, day);
                var events = Events.Find(x => x.DateAndTime.Date == targetDate).OrderBy(x => x.DateAndTime);

                var embed = EmbedUtility.FromPrimary($"Upcoming Server Events [{targetDate.ToString(Culture.DateTimeFormat.ShortDatePattern)}]", null, builder => {
                    if (!events.Any()) {
                        builder.Description = "No events found";
                    }
                    else {
                        foreach (var ev in events)
                            builder.AddField(ev.Name + $" - **{ev.DateAndTime.ToString(Culture.DateTimeFormat.ShortTimePattern)}**", ev.Description);
                    }
                });

                var component = events.Any() ? new ComponentBuilder().WithSelectMenu("event-configure",
                    events.Select(x => new SelectMenuOptionBuilder(x.Name, x.Id.ToString(), x.Id.ToString())).ToList()).Build() : null;


                await RespondAsync(embed: embed, component: component, ephemeral: true);
            }

            [SlashCommand("event", "Print the info of an event")]
            public async Task Event([Autocomplete(typeof(EventAutocompleter))] Guid guid) => 
                await PrintConfigureEventDialog(guid);


            [ComponentInteraction("event-configure", true)]
            public async Task ConfigureEvent(string[] values) => 
                await PrintConfigureEventDialog(Guid.Parse(values[0]));

            public async Task PrintConfigureEventDialog(Guid guid)
            {
                var ev = Events.FindById(guid);

                if (ev is null) {
                    await RespondAsync(embed: EmbedUtility.FromError(null, "Event no longer exists.", false));
                    return;
                }

                var embed = EmbedUtility.FromEvent(ev, Context.Client);

                var component = new ComponentBuilder()
                    .WithButton("Change Date", $"event-change-date:{ev.Id}", ButtonStyle.Primary, (Emoji)":calendar:")
                    .WithButton("Change Time", $"event-change-time:{ev.Id}", ButtonStyle.Primary, (Emoji)":clock1:")
                    .WithButton("Add Targets", $"event-add-targets:{ev.Id}", ButtonStyle.Primary, (Emoji)":loudspeaker:")
                    .WithButton("Remove Targets", $"event-remove-targets:{ev.Id}", ButtonStyle.Primary, (Emoji)":loudspeaker:")
                    .WithButton("Delete", $"event-delete:{ev.Id}", ButtonStyle.Danger, (Emoji)":wastebasket:")
                    .Build();

                await RespondAsync(embed: embed, component: component, ephemeral: true);
            }
        }

        [SlashCommand("change-date", "Change the date of an event")]
        public async Task ChangeDate([Autocomplete(typeof(EventAutocompleter))] Guid guid, [InclusiveRange(1, 31)] int day, Months month, [InclusiveRange(1, 9999)] int year)
        {
            var ev = Events.FindById(guid);

            var time = ev.DateAndTime.TimeOfDay;
            ev.DateAndTime = new DateTime(year, (int)month, day, time.Hours, time.Minutes, time.Seconds);

            if (Events.Update(ev))
                await RespondAsync(embed: EmbedUtility.FromSuccess(null, "Successfully updated the event", false), ephemeral: true);
            else
                await RespondAsync(embed: EmbedUtility.FromError(null, "There was a problem", false), ephemeral: true);
        }

        [SlashCommand("change-time", "Change the time of an event")]
        public async Task ChangeTime([Autocomplete(typeof(EventAutocompleter))] Guid guid, [InclusiveRange(0, 23)] int hour, [InclusiveRange(0, 59)] int minute)
        {
            var ev = Events.FindById(guid);

            var date = ev.DateAndTime.Date;
            ev.DateAndTime = new DateTime(date.Year, date.Month, date.Day, hour, minute, 0);

            if (Events.Update(ev))
                await RespondAsync(embed: EmbedUtility.FromSuccess(null, "Successfully updated the event", false), ephemeral: true);
            else
                await RespondAsync(embed: EmbedUtility.FromError(null, "There was a problem", false), ephemeral: true);
        }

        [SlashCommand("calendar-page", "Display a calendar page")]
        [Acknowledge(true)]
        public async Task Test(Months month, int year)
        {
            using var bmp = CalendarUtility.GenerateCalendarPage(1200, 800, (int)month, year);
            using var ms = new MemoryStream();

            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Seek(0, SeekOrigin.Begin);

            var message = await Context.Interaction.FollowupWithFileAsync(ms, "cal.png",
                embed: EmbedUtility.FromPrimary($":calendar_spiral: {month}, {year}", null, builder => builder.ImageUrl = "attachment://cal.png"), ephemeral: true);
        }

        [SlashCommand("rename", "Change the name of an event")]
        public async Task Rename([Autocomplete(typeof(EventAutocompleter))] Guid guid, [Summary("new-name")] string name)
        {
            var ev = Events.FindById(guid);

            ev.Name = name;

            if (Events.Update(ev))
                await RespondAsync(embed: EmbedUtility.FromSuccess(null, "Successfully updated the event", false), ephemeral: true);
            else
                await RespondAsync(embed: EmbedUtility.FromError(null, "There was a problem", false), ephemeral: true);
        }

        [SlashCommand("delete", "Delete an event")]
        public async Task Delete([Autocomplete(typeof(EventAutocompleter))] Guid guid)
        {
            if (Events.Delete(guid))
                await RespondAsync(embed: EmbedUtility.FromSuccess(null, "Event deleted.", false), ephemeral: true);
            else
                await RespondAsync(embed: EmbedUtility.FromError(null, "Event couln't be deleted.", false), ephemeral: true);
        }

        [ComponentInteraction("event-delete:*")]
        public async Task Delete(string guid)
        {
            if (Events.Delete(Guid.Parse(guid)))
                await (Context.Interaction as SocketMessageComponent).UpdateAsync(props => {
                    props.Content = string.Empty;
                    props.Embeds = null;
                    props.Components = null;
                    props.Embed = EmbedUtility.FromSuccess(null, "Event deleted.", false);
                });
            else
                await (Context.Interaction as SocketMessageComponent).UpdateAsync(props => {
                    props.Content = string.Empty;
                    props.Embeds = null;
                    props.Components = null;
                    props.Embed = EmbedUtility.FromError(null, "Event couln't be deleted.", false);
                });
        }

        [ComponentInteraction("event-target-role:*")]
        [Acknowledge]
        public async Task ConfigureRoles(string guid, params string[] values)
        {
            var roles = values.Select(x => Convert.ToUInt64(x));

            var ev = Events.FindById(Guid.Parse(guid));

            if (ev is null) {
                await FollowupAsync(embed: EmbedUtility.FromError(null, "Event no longer exists.", false));
                return;
            }

            if (ev.TargetRoles is not null)
                ev.TargetRoles.AddRange(roles);
            else {
                ev.TargetRoles = new List<ulong>();
                ev.TargetRoles.AddRange(roles);
            }

            Events.Update(ev);
        }

        [ComponentInteraction("event-target-user:*,*")]
        [Acknowledge]
        public async Task ConfigureUsers(string guid, string op, params string[] values)
        {
            var users = values.Select(x => Convert.ToUInt64(x));

            var ev = Events.FindById(Guid.Parse(guid));

            if (ev is null) {
                await FollowupAsync(embed: EmbedUtility.FromError(null, "Event no longer exists.", false));
                return;
            }

            if (ev.TargetUsers is not null)
                switch (op) {
                    case "add":
                        ev.TargetUsers.AddRange(users);
                        break;
                    case "remove":
                        ev.TargetUsers.RemoveAll(x => users.Contains(x));
                        break;
                }
            else {
                if(op == "add") {
                    ev.TargetUsers = new List<ulong>();
                    ev.TargetUsers.AddRange(users);
                }
            }

            Events.Update(ev);
        }

        [ComponentInteraction("dismiss")]
        public async Task Dismiss()
        {
            await (Context.Interaction as SocketMessageComponent).UpdateAsync(props => {
                props.Content = string.Empty;
                props.Components = null;
                props.Embeds = null;
                props.Embed = EmbedUtility.FromSuccess(null, "Message Dismissed", false);
            });
        }

        [ComponentInteraction("event-*-targets:*")]
        public async Task ChangeTargets(string op, string guid)
        {
            var ev = Events.FindById(Guid.Parse(guid));

            await PrintTargetConfigure(ev, op);
        }

        [ComponentInteraction("event-change-date:*")]
        public async Task ChangeDateDialog(string guid)
        {
            var ev = Events.FindById(Guid.Parse(guid));

            if (ev is null) {
                await RespondAsync(embed: EmbedUtility.FromError(null, "Event no longer exists.", false));
                return;
            }

            var component = new ComponentBuilder()
                .WithButton("1 Day", $"event-time-add-24:{guid}", ButtonStyle.Primary, (Emoji)":heavy_plus_sign:")
                .WithButton("1 Week", $"event-time-add-{TimeSpan.FromDays(7).TotalHours}:{guid}", ButtonStyle.Primary, (Emoji)":heavy_plus_sign:")
                .WithButton("1 Month", $"event-time-add-{TimeSpan.FromDays(31).TotalHours}:{guid}", ButtonStyle.Primary, (Emoji)":heavy_plus_sign:")
                .WithButton("1 Day", $"event-time-substract-24:{guid}", ButtonStyle.Primary, (Emoji)":heavy_minus_sign:", row: 1)
                .WithButton("1 Week", $"event-time-substract-{TimeSpan.FromDays(7).TotalHours}:{guid}", ButtonStyle.Primary, (Emoji)":heavy_minus_sign:", row: 1)
                .WithButton("1 Month", $"event-time-substract-{TimeSpan.FromDays(31).TotalHours}:{guid}", ButtonStyle.Primary, (Emoji)":heavy_minus_sign:", row: 1)
                .Build();

            await RespondAsync(embed: EmbedUtility.FromPrimary($"Configure {ev.Name} event", null), component: component, ephemeral: true);
        }

        [ComponentInteraction("event-change-time:*")]
        public async Task ChangeTimeDialog(string guid)
        {
            var ev = Events.FindById(Guid.Parse(guid));

            if (ev is null) {
                await RespondAsync(embed: EmbedUtility.FromError(null, "Event no longer exists.", false));
                return;
            }

            var component = new ComponentBuilder()
                .WithButton("1 Hour", $"event-time-add-1:{guid}", ButtonStyle.Primary, (Emoji)":heavy_plus_sign:")
                .WithButton("4 Hours", $"event-time-add-4:{guid}", ButtonStyle.Primary, (Emoji)":heavy_plus_sign:")
                .WithButton("12 Hours", $"event-time-add-12:{guid}", ButtonStyle.Primary, (Emoji)":heavy_plus_sign:")
                .WithButton("1 Hour", $"event-time-substract-1:{guid}", ButtonStyle.Primary, (Emoji)":heavy_minus_sign:", row: 1)
                .WithButton("4 Hours", $"event-time-substract-4:{guid}", ButtonStyle.Primary, (Emoji)":heavy_minus_sign:", row: 1)
                .WithButton("12 Hours", $"event-time-substract-12:{guid}", ButtonStyle.Primary, (Emoji)":heavy_minus_sign:", row: 1)
                .Build();

            await RespondAsync(embed: EmbedUtility.FromPrimary($"Configure {ev.Name} event", null), component: component, ephemeral: true);
        }

        [ComponentInteraction("event-time-*-*:*")]
        [Acknowledge]
        public Task ChangeDateTime(string op, string hours, string guid)
        {
            var ev = Events.FindById(Guid.Parse(guid));

            var hoursInt = Convert.ToInt32(hours);

            switch (op) {
                case "add":
                    ev.DateAndTime.AddHours(hoursInt);
                    break;
                case "substract":
                    ev.DateAndTime.Subtract(TimeSpan.FromHours(hoursInt));
                    break;
            }

            Events.Update(ev);
            return Task.CompletedTask;
        }

        private async Task PrintTargetConfigure(CalendarEvent ev, string suffix)
        {
            var roles = Context.Guild.Roles;
            var users = Context.Guild.Users;

            var targetRoleSelector = new SelectMenuBuilder($"event-target-role:{ev.Id},{suffix}", placeholder: "Select target roles",
                options: roles.Select(x => new SelectMenuOptionBuilder(x.Name, x.Id.ToString(), isDefault: ev.TargetRoles?.Contains(x.Id))).ToList(), 
                maxValues: roles.Count);

            var targetUserSelector = new SelectMenuBuilder($"event-target-user:{ev.Id},{suffix}", placeholder: "Select target users",
                options: users.Select(x => new SelectMenuOptionBuilder(x.Username, x.Id.ToString(), isDefault: ev.TargetUsers?.Contains(x.Id))).ToList(), 
                maxValues: users.Count);

            var component = new ComponentBuilder()
                .WithSelectMenu(targetRoleSelector, 0)
                .WithSelectMenu(targetUserSelector, 1)
                .WithButton("Dismiss", $"dismiss", ButtonStyle.Secondary, emote: (Emoji)":heavy_multiplication_x:", row: 2)
                .Build();

            await RespondAsync(embed: EmbedUtility.FromEvent(ev, Context.Client), component: component, ephemeral: true);
        }
    }
}