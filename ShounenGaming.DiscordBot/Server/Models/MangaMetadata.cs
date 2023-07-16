using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShounenGaming.DiscordBot.Server.Models
{
    public class MangaMetadata
    {
        public long Id { get; set; }
        public MangaMetadataSourceEnum Source { get; set; }

        public List<string> Titles { get; set; }
        public string ImageUrl { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public decimal? Score { get; set; }
        public string Status { get; set; }
        public List<string> Tags { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
    }
}
