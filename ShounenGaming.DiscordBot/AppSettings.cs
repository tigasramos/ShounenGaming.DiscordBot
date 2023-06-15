using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShounenGaming.DiscordBot
{
    internal class AppSettings
    {
        public ServerSettings Server { get; set; }
        public DiscordSettings Discord { get; set; }
    }

    internal class ServerSettings
    {
        public string Url { get; set; }
        public string Port { get; set; }
        public string BotDiscordId { get; set; }
        public string BotPassword { get; set; }
    }

    internal class DiscordSettings
    {
        public string Token { get; set; }
        public string Prefix { get; set; }
    }
}
