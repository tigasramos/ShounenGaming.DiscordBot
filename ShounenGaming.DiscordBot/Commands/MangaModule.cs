using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShounenGaming.DiscordBot.Server.Services;
using DSharpPlus.Entities;
using ShounenGaming.DiscordBot.Hubs;

namespace ShounenGaming.DiscordBot.Commands
{
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class MangaModule : BaseCommandModule
    {
        public IMangasService mangasService { private get; set; }
        public MangasHub mangasHub { private get; set; }


        [Command("searchMAL")]
        public async Task SearchMyAnimeListMangas(CommandContext ctx, [RemainingText]string name)
        {
            var mangas = await mangasService.SearchMangaMetadata(Server.Models.MangaMetadataSourceEnum.MYANIMELIST, name);
            StringBuilder sb = new();
            sb.Capacity = 2000;
            sb.AppendLine("```");
            mangas.ForEach(m => { if (sb.Length < 1900) sb.AppendLine($"- {m.Id}. {m.Titles.First()} : {m.Status} - {m.Type}"); });
            sb.Append("```");
            await ctx.RespondAsync(sb.ToString());
        }

        [Command("searchAL")]
        public async Task SearchAniListMangas(CommandContext ctx, [RemainingText] string name)
        {
            var mangas = await mangasService.SearchMangaMetadata(Server.Models.MangaMetadataSourceEnum.ANILIST, name);
            StringBuilder sb = new();
            sb.Capacity = 2000;
            sb.AppendLine("```");
            mangas.ForEach(m => { if (sb.Length < 1900) sb.AppendLine($"- {m.Id}. {m.Titles.First()} : {m.Status} - {m.Type}"); });
            sb.Append("```");
            await ctx.RespondAsync(sb.ToString());
        }

        [Command("al")]
        public async Task AddAniListManga(CommandContext ctx, long id)
        {
            try
            {
                await mangasHub.AddManga(Server.Models.MangaMetadataSourceEnum.ANILIST, id, ctx.Message.Author.Id.ToString());

                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":thumbsup:"));
            }
            catch (Exception ex)
            {
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":thumbsdown:"));
            }
        }

        [Command("mal")]
        public async Task AddMyAnimeListManga(CommandContext ctx, long id)
        {
            try
            {
                await mangasHub.AddManga(Server.Models.MangaMetadataSourceEnum.MYANIMELIST, id, ctx.Message.Author.Id.ToString());

                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":thumbsup:"));
            }
            catch (Exception ex)
            {
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":thumbsdown:"));
            }
        }
    }
}
