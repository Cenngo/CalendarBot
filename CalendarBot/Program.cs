global using Discord;
global using Discord.Interactions;
global using Discord.WebSocket;
using LiteDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace CalendarBot
{
    class Program
    {
        static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables("DC_")
                .AddJsonFile("appsettings.json")
                .AddCommandLine(args)
                .Build();

            RunAsync(configuration).GetAwaiter().GetResult();
        }

        async static Task RunAsync(IConfiguration configuration)
        {
            using var services = ConfigureServices(new ServiceCollection(), configuration);

            var commands = services.GetRequiredService<InteractionService>();

            commands.AddTypeConverter<Guid>(new GuidTypeConverter());

            await new CommandHandler(services, configuration)
                .Initialize();

            new CalendarBot.EventHandler<DiscordSocketClient>(services, configuration)
                .RegisterCommands();

            var calHandler = services.GetRequiredService<CalendarHandler>();
            calHandler.Initialize();

            var discord = services.GetRequiredService<DiscordSocketClient>();

            await discord.LoginAsync(TokenType.Bot, configuration["Discord:Token"]);
            await discord.StartAsync();
            await discord.SetActivityAsync(new Game(configuration["Discord:Activity:Name"], configuration.GetValue<ActivityType>("Discord:Activity:Type")));

            Console.CancelKeyPress += (sender, e) => {
                e.Cancel = true;
                services.GetRequiredService<ILiteDatabase>().Checkpoint();
                discord.StopAsync().GetAwaiter().GetResult();
                discord.LogoutAsync().GetAwaiter().GetResult();
                Environment.Exit(0);
            };
            await Task.Delay(Timeout.Infinite);
        }

        static ServiceProvider ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
        {
            return serviceCollection
                //.Configure<LoggingOptions>(configuration.GetSection(LoggingOptions.SECTION))
                //.Configure<DiscordOptions>(configuration.GetSection(DiscordOptions.SECTION))
                .AddSingleton(configuration)
                .AddLogging(builder => {
                    builder.AddSimpleConsole();
                    builder.SetMinimumLevel(LogLevel.Trace);
                })
                .AddSingleton(new DiscordSocketConfig {
                    LogLevel = configuration.GetValue<LogLevel>("Logging:Discord").ToDiscord(),
                    GatewayIntents = GatewayIntents.GuildMembers | GatewayIntents.AllUnprivileged,
                    AlwaysDownloadUsers = true
                })
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton(new InteractionServiceConfig {
                    DefaultRunMode = RunMode.Async,
                    LogLevel = configuration.GetValue<LogLevel>("Logging:Commands").ToDiscord(),
                    UseCompiledLambda = true,
                    WildCardExpression = "*",
                })
                .AddSingleton<InteractionService>()
                .AddSingleton<ILiteDatabase>(new LiteDatabase(configuration.GetConnectionString("LiteDB")))
                .AddSingleton<ILiteCollection<CalendarEvent>>(services => {
                    var collection = services.GetRequiredService<ILiteDatabase>().GetCollection<CalendarEvent>();
                    collection.EnsureIndex(x => x.DateAndTime);
                    collection.EnsureIndex(x => x.Name);
                    return collection;
                })
                .AddSingleton<CalendarHandler>()
                .AddSingleton<GuidTypeConverter>()
                .AddSingleton(CultureInfo.GetCultureInfo(configuration["CultureInfo"]))
                .BuildServiceProvider();
        }
    }
}
