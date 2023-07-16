using DSharpPlus;
using Microsoft.AspNetCore.SignalR.Client;
using ShounenGaming.DiscordBot.Helpers;
using ShounenGaming.DiscordBot.Models;
using ShounenGaming.DiscordBot.Server.Models;

namespace ShounenGaming.DiscordBot.Hubs
{
    public class MangasHub : BaseHub
    {
        public MangasHub(AppSettings appSettings, LoginHelper loginHelper, DiscordClient bot) : base(appSettings, loginHelper, "mangasHub")
        {

        }

        public async Task AddManga(MangaMetadataSourceEnum source, long mangaId, string discordId)
        {
            await hub.InvokeAsync("AddManga", source, mangaId, discordId);
        }
    }
}
