using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Discord.WebSocket;
using Discord;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using EventManager.Services;
using Microsoft.Extensions.Configuration;
using EventManager.Configurations;
using System.Reflection;
using Discord.Interactions;
using EventManager.Handler;
using Discord.Commands;
using EventManager.Database;
using EventManager.Modules;

await Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(app =>
    {
        app.AddJsonFile("appsettings.json");
    })
    .ConfigureServices(async (hostContext, serviceProvider) =>
    {
        serviceProvider.Configure<DiscordConfiguration>(hostContext.Configuration.GetSection("Discord"));
        serviceProvider.Configure<DatabaseConfiguration>(hostContext.Configuration.GetSection("Database"));
        serviceProvider.AddLogging(configure => configure.AddConsole())
            .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Trace);
        
        var discord = new DiscordSocketClient(new DiscordSocketConfig
        {
            LogGatewayIntentWarnings = false,
            AlwaysDownloadUsers = true,
            LogLevel = LogSeverity.Debug,
        });

        serviceProvider
            .AddSingleton(discord)
            .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
            .AddSingleton(x => new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Debug,
                DefaultRunMode = Discord.Commands.RunMode.Async
            }))
            .AddSingleton(typeof(IMongoRepository<>), typeof(MongoRepository<>))
            .AddSingleton<InteractionHandler>()
            .AddSingleton<PrefixHandler>()
            .AddSingleton<ButtonExecutedHandler>()
            .AddSingleton<ModalSubmittedHandler>()
            .AddSingleton<UserVoiceStateUpdatedHandler>()
            .AddHostedService<UserActivity>()
            .AddHostedService<DiscordBOT>();

        var service = serviceProvider.BuildServiceProvider();
        var logger = service.GetService<ILogger<Program>>();
        discord.Log += async (LogMessage msg) => logger.LogDebug(msg.Message);

    }).Build().RunAsync();