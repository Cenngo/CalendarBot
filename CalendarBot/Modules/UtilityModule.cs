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

        [Group("calendar", "create/display upcoming events")]
        public sealed class Calendar : InteractionModuleBase<SocketInteractionCommandContext>
        {
            public ILiteCollection<CalendarEvent> Events { get; set; }

            [SlashCommand("create", "create a calendar event")]
            public async Task Create(string name, string description, int day, int month, int year, [Summary("target-user")] IUser targetUser)
            {
                var value = Events.Insert(new CalendarEvent {
                    Name = name,
                    Description = description,
                    DateAndTime = new DateTime(year, month, day),
                    Color = Color.Orange,
                    Channel = Context.Channel,
                    Guild = Context.Guild,
                    RecursionInterval = RecursionInterval.None,
                    User = Context.User,
                    TargetUsers = new List<IUser> { targetUser }
                });
            }

            [SlashCommand("display", "display the events of a month")]
            public async Task Display()
            {
                var first = Events.FindOne(x => true);
            }
        }

        [ComponentInteraction("monthSelect")]
        public async Task HandleMonthSelect(string[] values)
        {
            
        }
    }
}
