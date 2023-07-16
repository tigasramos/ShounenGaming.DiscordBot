using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShounenGaming.DiscordBot.Models
{
    public class AppSettings
    {
        public ServerSettings Server { get; set; }
        public DiscordSettings Discord { get; set; }
    }

    public class ServerSettings
    {
        public string Url { get; set; }
        public string BotDiscordId { get; set; }
        public string BotPassword { get; set; }
    }

    public class DiscordSettings
    {
        public string Token { get; set; }
        public string Prefix { get; set; }
        public DiscordServerSettings ServerSettings { get; set; }
    }

    public class DiscordServerSettings
    {
        public string ModsChannelId { get; set; }
        public string ModsRoleId { get; set; }
    }
}
