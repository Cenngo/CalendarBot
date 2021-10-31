using Discord;
using Discord.Interactions;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalendarBot
{
    public sealed class UtilityModule : InteractionModuleBase<SocketInteractionCommandContext>
    {
        public ILiteCollection<CalendarEvent> Events { get; set; }

        [SlashCommand("ping", "recieve a pong")]
        public async Task Ping() => 
            await RespondAsync("pong", ephemeral: true);

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
        [Acknowledge]
        public async Task Create(string name, string description,
            [InclusiveRange(1, 31)] int day,
            Months month,
            [InclusiveRange(0, 9999)] int year,
            [InclusiveRange(0, 23)] int hour,
            [InclusiveRange(0, 59)] int minute,
            [Summary("recursion-interval")]RecursionInterval recursionInterval = RecursionInterval.None)
        {
            var guid = Guid.NewGuid();

            var value = Events.Insert(new CalendarEvent {
                Id = guid,
                Name = name,
                Description = description,
                DateAndTime = new DateTime(year, (int)month, day, hour, minute, 0, DateTimeKind.Local),
                Color = Color.Orange,
                MessageChannelId = Context.Channel.Id,
                GuildId = Context.Guild.Id,
                RecursionInterval = recursionInterval,
                UserId = Context.User.Id
            });

            var roles = Context.Guild.Roles;
            var users = await Context.Guild.GetUsersAsync().FlattenAsync();

            var targetRoles = new List<SelectMenuOptionBuilder>();
            var targetUsers = new List<SelectMenuOptionBuilder>();

            foreach (var role in roles)
                targetRoles.Add(new SelectMenuOptionBuilder(role.Name, role.Id.ToString()));

            foreach (var user in users.Where(x => !x.IsBot))
                targetUsers.Add(new SelectMenuOptionBuilder(user.Username, user.Id.ToString()));

            var targetRoleSelector = new SelectMenuBuilder($"event-target-role:{guid}", placeholder: "Select target roles",
                options: roles.Select(x => new SelectMenuOptionBuilder(x.Name, x.Id.ToString())).ToList(), maxValues: roles.Count);

            var targetUserSelector = new SelectMenuBuilder($"event-target-user:{guid}", placeholder: "Select target users",
                options: users.Select(x => new SelectMenuOptionBuilder(x.Username, x.Id.ToString())).ToList(), maxValues: users.Count());

            var component = new ComponentBuilder()
                .WithSelectMenu(targetRoleSelector, 0)
                .WithSelectMenu(targetUserSelector, 1)
                .WithButton("Dismiss", "dismiss", ButtonStyle.Secondary, emote: Emoji.Parse(":heavy_multiplication_x:"), row: 2)
                .Build();

            await FollowupAsync($"Configure {name} event", component: component, ephemeral: true);
        }

        [SlashCommand("print", "Print the events of a day")]
        [Acknowledge]
        public async Task Display([InclusiveRange(1, 31)]int day, Months month, [InclusiveRange(1, 9999)]int year)
        {
            var targetDate = new DateTime(year, (int)month, day);
            var events = Events.Find(x => x.DateAndTime.Date == targetDate).OrderBy(x => x.DateAndTime);

            var embedBuilder = new EmbedBuilder {
                Title = $"Upcoming Server Events for {targetDate:dd/MMM/yyyy}",
                Color = Color.Orange,
            };

            if (!events.Any()) {
                embedBuilder.Description = "No events";
                await FollowupAsync(embed: embedBuilder.Build(), ephemeral: true);
                return;
            }

            List<SelectMenuOptionBuilder> options = new();

            foreach (var ev in events) {
                embedBuilder.AddField(ev.Name + $" - **{ev.DateAndTime:[HH:mm]}**", ev.Description);
                options.Add(new SelectMenuOptionBuilder(ev.Name, ev.Id.ToString(), ev.Id.ToString()));
            }

            var component = new ComponentBuilder().WithSelectMenu("event-configure", options).Build();


            await FollowupAsync(embed: embedBuilder.Build(), component: component, ephemeral: true);
        }

        [ComponentInteraction("event-configure")]
        [Acknowledge]
        public async Task ConfigureEvent(string values)
        {
            var ev = Events.FindById(Guid.Parse(values));

            var component = new ComponentBuilder()
                .WithButton("Change Date", $"event-change-date:{ev.Id}")
                .WithButton("Change Time", $"event-change-time:{ev.Id}")
                .WithButton("Configure Targets", $"event-change-targets:{ev.Id}")
                .WithButton("Delete", $"event-delete:{ev.Id}", ButtonStyle.Danger)
                .Build();

            var embed = new EmbedBuilder {
                Title = ev.Name,
                Description = ev.Description,
                Color = Color.Orange,
                Timestamp = ev.DateAndTime
            }.WithAuthor(await Context.Client.GetUserAsync(ev.UserId))
            .Build();

            await RespondAsync(embed: embed, component: component, ephemeral: true);
        }

        [SlashCommand("change-date", "Change the date of an event")]
        public async Task ChangeDate([EventAutocompleter]Guid guid, [InclusiveRange(1, 31)]int day, Months month, [InclusiveRange(1, 9999)]int year)
        {
            var ev = Events.FindById(guid);

            var time = ev.DateAndTime.TimeOfDay;
            ev.DateAndTime = new DateTime(year, (int)month, day, time.Hours, time.Minutes, time.Seconds);

            if (Events.Update(ev))
                await RespondAsync("Successfully updated the event", ephemeral: true);
            else
                await RespondAsync("There was a problem", ephemeral: true);
        }

        [SlashCommand("change-time", "Change the time of an event")]
        public async Task ChangeTime([EventAutocompleter]Guid guid, [InclusiveRange(0, 23)]int hour, [InclusiveRange(0, 59)]int minute)
        {
            var ev = Events.FindById(guid);

            var date = ev.DateAndTime.Date;
            ev.DateAndTime = new DateTime(date.Year, date.Month, date.Day, hour, minute, 0);

            if (Events.Update(ev))
                await RespondAsync("Successfully updated the event", ephemeral: true);
            else
                await RespondAsync("There was a problem", ephemeral: true);
        }

        [SlashCommand("rename", "Change the name of an event")]
        public async Task Rename([EventAutocompleter]Guid guid, [Summary("new-name")]string name)
        {
            var ev = Events.FindById(guid);

            ev.Name = name;

            if (Events.Update(ev))
                await RespondAsync("Successfully updated the event", ephemeral: true);
            else
                await RespondAsync("There was a problem", ephemeral: true);
        }

        [SlashCommand("delete", "Delete an event")]
        public async Task Delete(Guid guid)
        {
            if(Events.Delete(guid))
                await RespondAsync("Successfully deleted the event", ephemeral: true);
            else
                await RespondAsync("There was a problem", ephemeral: true);
        }

        [ComponentInteraction("event-delete:*")]
        [Acknowledge]
        public async Task Delete(string guid)
        {
            Events.Delete(Guid.Parse(guid));
        }

        [ComponentInteraction("event-target-role:*")]
        [Acknowledge]
        public async Task ConfigureRoles(string guid, params string[] values)
        {
            var roles = values.Select(x => Convert.ToUInt64(x));

            var ev = Events.FindById(Guid.Parse(guid));

            if (ev.TargetRoles is not null)
                ev.TargetRoles.AddRange(roles);
            else {
                ev.TargetRoles = new List<ulong>();
                ev.TargetRoles.AddRange(roles);
            }

            Events.Update(ev);
        }

        [ComponentInteraction("event-target-user:*")]
        [Acknowledge]
        public async Task ConfigureUsers(string guid, params string[] values)
        {
            var users = values.Select(x => Convert.ToUInt64(x));

            var ev = Events.FindById(Guid.Parse(guid));

            if (ev.TargetUsers is not null)
                ev.TargetUsers.AddRange(users);
            else {
                ev.TargetUsers = new List<ulong>();
                ev.TargetUsers.AddRange(users);
            }

            Events.Update(ev);
        }

        [ComponentInteraction("dismiss")]
        public async Task Dismiss()
        {
            await (Context.Interaction as SocketMessageComponent).UpdateAsync(props => {
                props.Content = ":white_check_mark: Message Dismissed";
                props.Components = null;
                props.Embeds = null;
            });
        }
    }
}