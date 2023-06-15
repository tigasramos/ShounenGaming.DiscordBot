using DSharpPlus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using ShounenGaming.DiscordBot;

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
    var appSettings = services.GetRequiredService<AppSettings>(); 
    
    //Start Bot
    var discord = services.GetRequiredService<DiscordClient>();
    await discord.ConnectAsync();


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

    var appSettings = configuration.GetSection("settings").Get<AppSettings>();
    services.AddSingleton<AppSettings>(_ => appSettings);
    services.AddSingleton<DiscordClient>(_ => new DiscordClient(new DiscordConfiguration
    {
        Token = appSettings.Discord.Token,
        TokenType = TokenType.Bot,
        Intents = DiscordIntents.All,
        LogTimestampFormat = "dd/MM/yyyy - HH:mm:ss"
    }));

    return services.BuildServiceProvider();
}