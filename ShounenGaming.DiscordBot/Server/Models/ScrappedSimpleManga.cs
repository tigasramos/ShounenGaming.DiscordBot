using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShounenGaming.DiscordBot.Server.Models
{
    public class ScrappedSimpleManga
    {
        public string Name { get; set; }
        public string Link { get; set; }
        public string? ImageURL { get; set; }
        public MangaSourceEnum Source { get; set; }
    }
}
