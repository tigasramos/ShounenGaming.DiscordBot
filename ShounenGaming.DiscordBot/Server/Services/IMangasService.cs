using Refit;
using ShounenGaming.DiscordBot.Server.Models;

namespace ShounenGaming.DiscordBot.Server.Services
{
    public interface IMangasService
    {
        [Get("/mangas/search/{source}")]
        Task<List<MangaMetadata>> SearchMangaMetadata(MangaMetadataSourceEnum source, [Query] string name);

        [Get("/mangas/search/sources")]
        Task<List<ScrappedSimpleManga>> SearchMangaSources([Query] string name);

        // TODO: Endpoint that suggests a Manga by DiscordId (by Tags)
    }
}
