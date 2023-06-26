using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;
using ShounenGaming.DiscordBot.Server.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShounenGaming.DiscordBot.Handlers;
using ShounenGaming.DiscordBot.Helpers;
using Microsoft.Extensions.DependencyInjection;
using DSharpPlus.Entities;

namespace ShounenGaming.DiscordBot.Commands
{
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class AdminCommandsModule : BaseCommandModule
    {
        public IServiceProvider services { private get; set; }

        [Command("reconnect")]
        public async Task ReconnectWithServer(CommandContext ctx)
        {
            var loginHelper = services.GetRequiredService<LoginHelper>();
            await ServerHubsHelper.ConnectToHubs(ServerHubsHelper.GetHubs(services), loginHelper);
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":thumbsup:"));
        }

        [Command("status")]
        public async Task GetServerHubsStatus(CommandContext ctx)
        {
            var hubs = ServerHubsHelper.GetHubs(services);
            var sb = new StringBuilder();
            foreach (var hub in hubs)
            {
                var state =  hub.IsConnected ? "connected" : "disconnected" ;
                sb.AppendLine($"{hub.GetType().Name} is {state}");
            }
            await ctx.RespondAsync(sb.ToString());
        }
    }
}
