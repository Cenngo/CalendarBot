using Discord;
using Discord.Interactions;
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
            [SlashCommand("create", "create a calendar event")]
            public async Task Create()
            {

            }

            [SlashCommand("display", "display the events of a month")]
            public async Task Display()
            {
                
            }
        }

        [ComponentInteraction("monthSelect")]
        public async Task HandleMonthSelect(string[] values)
        {
            Context.Guild.Emote
        }
    }
}
