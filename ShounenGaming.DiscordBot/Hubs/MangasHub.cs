using DSharpPlus;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;
using ShounenGaming.DiscordBot.Helpers;
using ShounenGaming.DiscordBot.Models;
using ShounenGaming.DiscordBot.Server.Models;

namespace ShounenGaming.DiscordBot.Hubs
{
    public class MangasHub : BaseHub
    {
        public MangasHub(AppSettings appSettings, LoginHelper loginHelper, DiscordClient bot) : base(appSettings, loginHelper, "mangasHub")
        {
            hub.On("ChaptersAdded", async (List<string> discordIds, string mangaName, List<double> chapters) =>
            {
                Log.Information("ChapterAdded received");

                var chapterPlural = chapters.Count > 1 ? "Chapters" : "Chapter";

                foreach (var discordId in discordIds)
                {
                    if (discordId != "270355690680221706") continue;

                    var member = bot.Guilds.Values.SelectMany(x => x.Members.Values).FirstOrDefault(m => m.Id.ToString() == discordId);
                    if (member is null) continue;

                    await member.SendMessageAsync($":alarm_clock: The manga **{mangaName}** has just published {chapterPlural} **_{string.Join(", ", chapters)}_**.");
                }

            });
        }

        public async Task AddManga(MangaMetadataSourceEnum source, long mangaId, string discordId)
        {
            await hub.InvokeAsync("AddManga", source, mangaId, discordId);
        }
    }
}
