using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using ShounenGaming.DiscordBot;
using ShounenGaming.DiscordBot.Commands;
using System.Reflection;

try
{
    //Configure Logger
    Log.Logger = new LoggerConfiguration()
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: "[{Timestamp:dd/MM/yyyy - HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File($"logs/log-.txt", rollingInterval: RollingInterval.Day, outputTemplate: "[{Timestamp:dd/MM/yyyy - HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
        .CreateLogger();

    Log.Information("Starting ShounenGaming Discord Bot");

    var services = CreateServiceProvider();
    
    //Discord Bot
    await ConfigureDiscordBot(services);

    //TODO: SignalR Server & API

    await Task.Delay(-1);
} 
catch(Exception ex)
{
    Log.Fatal(ex.Message);
}
finally
{
    Log.CloseAndFlush();
}

static IServiceProvider CreateServiceProvider()
{
    var services = new ServiceCollection();

    var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
#if DEBUG
            .AddJsonFile("appsettings.dev.json", optional: false, reloadOnChange: true)
#else
            .AddJsonFile("appsettings.prod.json", optional: false, reloadOnChange: true)
#endif
            .Build();

    var appSettings = configuration.GetRequiredSection("settings").Get<AppSettings>();
    services.AddSingleton<AppSettings>(_ => appSettings);
    services.AddSingleton<DiscordClient>(_ => new DiscordClient(new DiscordConfiguration
    {
        Token = appSettings.Discord.Token,
        TokenType = TokenType.Bot,
        Intents = DiscordIntents.All,
        LogTimestampFormat = "dd/MM/yyyy - HH:mm:ss"
    }));
    services.AddSingleton<DiscordEventsHandler>();

    return services.BuildServiceProvider();
}

static async Task ConfigureDiscordBot(IServiceProvider services)
{
    Log.Information("Configuring the Discord Connection");
    var appSettings = services.GetRequiredService<AppSettings>();

    //Start Bot
    var discord = services.GetRequiredService<DiscordClient>();

    //Interactivity
    discord.UseInteractivity(new InteractivityConfiguration()
    {
        PollBehaviour = PollBehaviour.KeepEmojis,
        Timeout = TimeSpan.FromSeconds(30)
    });

    //Commands
    var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
    {
        Services = services,
        StringPrefixes = new[] { appSettings.Discord.Prefix },
    });
    commands.RegisterCommands(Assembly.GetExecutingAssembly());

    //Events
    services.GetRequiredService<DiscordEventsHandler>().SubscribeDiscordEvents(discord);

    Log.Information("Connecting to Discord");
    await discord.ConnectAsync();
}