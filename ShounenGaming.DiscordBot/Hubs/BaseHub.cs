using Microsoft.AspNetCore.SignalR.Client;
using Serilog;
using ShounenGaming.DiscordBot.Helpers;
using ShounenGaming.DiscordBot.Models;

namespace ShounenGaming.DiscordBot.Hubs
{
    public abstract class BaseHub
    {
        protected readonly HubConnection hub;
        public BaseHub(AppSettings appSettings, LoginHelper loginHelper, string hubName)
        {
            var tokenProvider = () => loginHelper.GetToken();
            var reconnectInterval = new[] { TimeSpan.FromSeconds(2) };

            hub = new HubConnectionBuilder()
                    .WithUrl($"{appSettings.Server.Url}/{hubName}", options =>
                    {
                        options.AccessTokenProvider = tokenProvider;
                    })
                    .WithAutomaticReconnect(reconnectInterval)
                    .Build();

            hub.Closed += async (error) => {
                Log.Information("Closed");
                if (error is not null && error.Message.Contains("Reconnect retries have been exhausted"))
                {
                    await loginHelper.LoginBot();
                }

                await Task.Delay(new Random().Next(0, 5) * 1000);
                await hub.StartAsync();
            };
        }
        public bool IsConnected => hub.State == HubConnectionState.Connected;

        public async Task StartHub()
        {
            if (!IsConnected)
                await hub.StartAsync();
        }
    }

}
