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
        [SlashCommand("ping", "recieve a pong")]
        public async Task Ping() => 
            await RespondAsync("pong");

        [SlashCommand("latency", "Get the estimate round-trip latency to the gateway server")]
        public async Task Latency() => 
            await RespondAsync(Context.Client.Latency.ToString() + " ms");

        [SlashCommand("invite", "Get the invite link")]
        public async Task Invite()
        {
            var button = new ComponentBuilder().WithButton("Invite Me!", style: ButtonStyle.Link,
                url: $"https://discord.com/api/oauth2/authorize?client_id={Context.Client.CurrentUser.Id}&permissions=2048&redirect_uri=https%3A%2F%2Fhydrametry.com&scope=bot%20applications.commands")
                .Build();

            await RespondAsync("\u200B", component: button);
        }

        [Group("calendar", "create/display calendar events")]
        public sealed class Calendar : InteractionModuleBase<SocketInteractionCommandContext>
        {
            public ILiteCollection<CalendarEvent> Events { get; set; }

            [SlashCommand("create", "create a calendar event")]
            [Acknowledge]
            public async Task Create(string name, string description, 
                [InclusiveRange(1, 31)]int day, 
                [InclusiveRange(1, 12)]int month, 
                [InclusiveRange(0, 9999)]int year, 
                [InclusiveRange(0, 23)]int hour, 
                [InclusiveRange(0, 59)]int minute)
            {
                var guid = Guid.NewGuid();

                var value = Events.Insert(new CalendarEvent {
                    Id = guid,
                    Name = name,
                    Description = description,
                    DateAndTime = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Local),
                    Color = Color.Orange,
                    MessageChannelId = Context.Channel.Id,
                    GuildId = Context.Guild.Id,
                    RecursionInterval = RecursionInterval.None,
                    UserId = Context.User.Id
                });

                var roles = Context.Guild.Roles;
                var users = await Context.Guild.GetUsersAsync().FlattenAsync();

                var targetRoles = new List<SelectMenuOptionBuilder>();
                var targetUsers = new List<SelectMenuOptionBuilder>();

                foreach (var role in roles)
                    targetRoles.Add(new SelectMenuOptionBuilder(role.Name, role.Id.ToString()));

                foreach (var user in users)
                    targetUsers.Add(new SelectMenuOptionBuilder(user.Username, user.Id.ToString()));

                var targetRoleSelector = new SelectMenuBuilder($"event-target(role):{guid}", placeholder: "Roles", 
                    options: roles.Select(x => new SelectMenuOptionBuilder(x.Name, x.Id.ToString())).ToList(), maxValues: roles.Count);

                var targetUserSelector = new SelectMenuBuilder($"event-target(user):{guid}", placeholder: "Users", 
                    options: users.Select(x => new SelectMenuOptionBuilder(x.Username, x.Id.ToString())).ToList(), maxValues: roles.Count);

                var component = new ComponentBuilder()
                    .WithSelectMenu(targetRoleSelector, 0)
                    .WithSelectMenu(targetUserSelector, 1)
                    .WithButton("Dismiss", "dismiss", ButtonStyle.Secondary, emote: Emoji.Parse(":heavy_multiplication_x:"), row: 2)
                    .Build();

                await FollowupAsync($"Configure {name} event", component: component, ephemeral: true);
            }

            [SlashCommand("print", "Print the events of a day")]
            [Acknowledge]
            public async Task Display([InclusiveRange(1, 9999)]int year, [InclusiveRange(1, 12)]int month, [InclusiveRange(1, 31)]int  day)
            {
                var events = Events.Find(x => x.DateAndTime.Date == DateTime.Today).OrderBy(x => x.DateAndTime);

                var embedBuilder = new EmbedBuilder {
                    Title = $"Upcoming Server Events for {DateTime.Today:dd/MMM/yyyy}",
                    Color = Color.Orange,
                };

                if(events.Count() == 0) {
                    embedBuilder.Description = "No events";
                    await FollowupAsync(embed: embedBuilder.Build());
                    return;
                }

                List<SelectMenuOptionBuilder> options = new();

                foreach (var ev in events) {
                    embedBuilder.AddField(ev.Name + $" - **{ev.DateAndTime:[HH:mm]}**", ev.Description);
                    options.Add(new SelectMenuOptionBuilder(ev.Name, ev.Id.ToString(), ev.Id.ToString()));
                }

                var component = new ComponentBuilder().WithSelectMenu("event-configure", options).Build();


                await FollowupAsync(embed: embedBuilder.Build(), component: component);
            }

            [ComponentInteraction("event-configure", true)]
            public async Task ConfigureEvent(string values)
            {
                var ev = Events.FindById(Guid.Parse(values));

                var component = new ComponentBuilder()
                    .WithButton("Change Date", $"event-change(date):{ev.Id}")
                    .WithButton("Change Time", $"event-change(time):{ev.Id}")
                    .WithButton("Configure Targets", $"event-change(targets):{ev.Id}")
                    .WithButton("Delete", $"event-delete:{ev.Id}", ButtonStyle.Danger)
                    .Build();

                var embed = new EmbedBuilder {
                    Title = ev.Name,
                    Description = ev.Description,
                    Color = Color.Orange,
                    Timestamp = ev.DateAndTime
                }.WithAuthor(await Context.Client.GetUserAsync(ev.UserId))
                .Build();

                await RespondAsync(embed: embed, component: component);
            }

            [ComponentInteraction("event-change(date):*")]
            public async Task ChangeDate(string guid)
            {
                var ev = Events.FindById(Guid.Parse(guid));
            }

            [ComponentInteraction("event-change(time):*")]
            public async Task ChangeTime(string guid)
            {
                var ev = Events.FindById(Guid.Parse(guid));
            }

            [ComponentInteraction("event-change(targets):*")]
            public async Task ChangeTargets(string guid)
            {
                var ev = Events.FindById(Guid.Parse(guid));
            }

            [ComponentInteraction("event-delete:*")]
            public async Task Delete(string guid)
            {
                Events.Delete(Guid.Parse(guid));
            }

            [ComponentInteraction("event-target(role):*", true)]
            public async Task ConfigureRoles(string guid, string[] values)
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

            [ComponentInteraction("event-target(user):*", true)]
            public async Task ConfigureUsers(string guid, string[] values)
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