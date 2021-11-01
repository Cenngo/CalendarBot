using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CalendarBot
{
    internal class EventHandler<T> where T : BaseSocketClient
    {
        private readonly IConfiguration _config;

        private readonly T _discord;
        private readonly InteractionService _commands;
        private readonly ILogger<T> _discordLogger;
        private readonly ILogger<InteractionService> _commandsLogger;
        private readonly ConcurrentQueue<(ILogger Logger, LogMessage LogMessage)> _logQueue = new();

        public EventHandler( IServiceProvider serviceProvider, IConfiguration configuration )
        {
            _config = configuration;

            _discord = serviceProvider.GetRequiredService<T>();
            _commands = serviceProvider.GetRequiredService<InteractionService>();
            _discordLogger = serviceProvider.GetRequiredService<ILogger<T>>();
            _commandsLogger = serviceProvider.GetRequiredService<ILogger<InteractionService>>();

            _ = ProcessQueue();
        }

        public void RegisterCommands()
        {
            _discord.Log += Discord_Log;
            _commands.Log += Commands_Log;
        }

        private Task Commands_Log(LogMessage arg)
        {
            _logQueue.Enqueue((_commandsLogger, arg));
            return Task.CompletedTask;
        }

        private Task Discord_Log(LogMessage arg)
        {
            _logQueue.Enqueue((_discordLogger, arg));
            return Task.CompletedTask;
        }

        private async Task ProcessQueue( CancellationToken cancellationToken = default )
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!_logQueue.IsEmpty && _logQueue.TryDequeue(out var loggingPair))
                    loggingPair.Logger.Log(loggingPair.LogMessage.Severity.ToMicrosoft(), loggingPair.LogMessage.ToString());

                await Task.Delay(_config.GetValue<int>("Logging:ProcessingInterval"), cancellationToken);
            }
        }
    }
}
