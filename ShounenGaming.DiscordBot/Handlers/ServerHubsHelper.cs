using Microsoft.Extensions.DependencyInjection;
using Serilog;
using ShounenGaming.DiscordBot.Helpers;
using ShounenGaming.DiscordBot.Hubs;

namespace ShounenGaming.DiscordBot.Handlers
{
    internal static class ServerHubsHelper
    {

        public static IList<BaseHub> GetHubs(IServiceProvider services)
        {
            var discordEventsHub = services.GetRequiredService<DiscordEventsHub>();
            var mangasHub = services.GetRequiredService<MangasHub>();

            return new List<BaseHub> {  discordEventsHub, mangasHub };
        }

        public static async Task ConnectToHubs(IList<BaseHub> hubs, LoginHelper loginHelper)
        {
            for(int i = 0; i < hubs.Count; i++)
            {
                try
                {
                    await hubs[i].StartHub();
                } 
                catch(Exception ex)
                {
                    if (ex.Message.Contains("401"))
                        await loginHelper.LoginBot();

                    Log.Error($"Failed connecting to Hub {i}: {ex.Message}");
                    await Task.Delay(1000);
                    i--;
                }
            }
        }
    }
    
    
    
}
