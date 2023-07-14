using DSharpPlus;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Refit;
using ShounenGaming.DiscordBot.Server.Services;
using ShounenGaming.DiscordBot.Handlers;
using ShounenGaming.DiscordBot.Models;
using ShounenGaming.DiscordBot.Helpers;
using ShounenGaming.DiscordBot.Hubs;
using System.Net;

try
{
    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

    //Configure Logger
    Log.Logger = new LoggerConfiguration()
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: "[{Timestamp:dd/MM/yyyy - HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File($"logs/log-.txt", rollingInterval: RollingInterval.Day, outputTemplate: "[{Timestamp:dd/MM/yyyy - HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
        .CreateLogger();

    Log.Information("Starting ShounenGaming Discord Bot");

    var services = CreateServiceProvider();

    var serverUrl = services.GetRequiredService<AppSettings>().Server.Url;
    var serverRunning = false;
    while (!serverRunning)
    {
        await Task.Delay(5000);
        serverRunning = await CheckServerStatusHelper.CheckServerIsRunning(serverUrl ?? "");
        Log.Information($"Server Status: {serverRunning}");
    }

    //Discord Bot
    var discord = services.GetRequiredService<DiscordClient>();
    discord.RegisterEventsAndCommands(services);
    Log.Information("Connecting to Discord");
    await discord.ConnectAsync();

    //SignalR Connect to Hubs
    var loginHelper = services.GetRequiredService<LoginHelper>();
    await ServerHubsHelper.ConnectToHubs(ServerHubsHelper.GetHubs(services), loginHelper);

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
    var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

    var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile($"appsettings.{environmentName}.json",
                optional: true,
                reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

    var appSettings = configuration.GetRequiredSection("settings").Get<AppSettings>()!;
    services.AddSingleton<AppSettings>(_ => appSettings);
    services.AddSingleton<DiscordClient>(_ => DiscordBotHelper.CreateDiscordBot(appSettings));
    services.AddSingleton(new AppState());
    services.AddMemoryCache();

    var serverUri = new Uri($"{appSettings.Server.Url}/api");

    //Server Services
    services.AddRefitClient<IAuthService>()
        .ConfigureHttpClient(c => c.BaseAddress = serverUri);

    services.AddRefitClient<IMangasService>()
        .ConfigureHttpClient(c => c.BaseAddress = serverUri)
        .AddHttpMessageHandler<AuthHeaderHandler>();

    //Handlers
    services.AddTransient<AuthHeaderHandler>();
    services.AddTransient<DiscordEventsHandler>();

    //Helpers
    services.AddTransient<LoginHelper>();

    //Hubs
    services.AddSingleton<DiscordEventsHub>();
    services.AddSingleton<MangasHub>();

    return services.BuildServiceProvider();
}
