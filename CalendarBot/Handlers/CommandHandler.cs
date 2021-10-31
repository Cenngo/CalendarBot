using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace CalendarBot
{
    internal class CommandHandler
    {
        private readonly DiscordSocketClient _discord;
        private readonly InteractionService _commands;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public CommandHandler ( IServiceProvider serviceProvider, IConfiguration configuration )
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;

            _discord = _serviceProvider.GetRequiredService<DiscordSocketClient>();
            _commands = _serviceProvider.GetRequiredService<InteractionService>();
        }

        public async Task Initialize ( )
        {
            await _commands.AddModulesAsync(Assembly.GetExecutingAssembly(), _serviceProvider);
            _discord.Ready += RegisterCommands;
            _discord.InteractionCreated += Discord_InteractionCreated;
            _commands.ComponentCommandExecuted += ComponentCommandExecuted;
            _commands.ContextCommandExecuted += ContextCommandExecuted;
            _commands.SlashCommandExecuted += SlashCommandExecuted;
        }

        private async Task SlashCommandExecuted(SlashCommandInfo arg1, IInteractionCommandContext arg2, IResult arg3)
        {
            if (!arg3.IsSuccess)
                await arg2.Channel.SendMessageAsync(arg3.ErrorReason);
        }

        private async Task ContextCommandExecuted(ContextCommandInfo arg1, IInteractionCommandContext arg2, IResult arg3)
        {
            if (!arg3.IsSuccess)
                await arg2.Channel.SendMessageAsync(arg3.ErrorReason);
        }

        private async Task ComponentCommandExecuted(ComponentCommandInfo arg1, IInteractionCommandContext arg2, IResult arg3)
        {
            if (!arg3.IsSuccess)
                await arg2.Channel.SendMessageAsync(arg3.ErrorReason);
        }

        private async Task Discord_InteractionCreated(SocketInteraction arg)
        {
            var ctx = new SocketInteractionCommandContext(_discord, arg);
            await _commands.ExecuteCommandAsync(ctx, _serviceProvider);
        }

        private async Task RegisterCommands ( )
        {
            if (IsDebug())
                await _commands.RegisterCommandsToGuildAsync(_configuration.GetValue<ulong>("Debug:TestGuild"));
            else
                await _commands.RegisterCommandsGloballyAsync();

            _discord.Ready -= RegisterCommands;
        }

        private static bool IsDebug ( )
        {
#if DEBUG
            return true;
#else
                return false;
#endif
        }
    }
}
